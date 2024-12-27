using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    // Implementation of the scenario orchestration.
    // This file contains all the logic and APIs to run a scenario stages and
    // synchronize the nodes and their the data flow.
    // For the scenario data model, see Scenario.cs.
    internal partial class Scenario
    {
        private struct StageRunResult
        {
            public bool Success;
            public List<Task> MonitoringTasks;
        }

        internal static event Action<Scenario> ScenarioStarted;

        private Dictionary<Node, HashSet<NodeInput>> m_Dependencies;

        public async Task RunOrResumeAsync(CancellationToken cancellationToken)
        {
            RefreshStatus();

            var state = Status.State;
            if (state != ScenarioState.Idle && state != ScenarioState.Running)
                throw new InvalidOperationException($"Cannot run or resume a scenario that is not in the idle or running state ({state}).");

            if (!m_HasStarted)
            {
                m_HasStarted = true;
                ScenarioStarted?.Invoke(this);
            }

            CacheDataForExecution();

            var stages = new Queue<ScenarioStage>(new ScenarioStage[]
            {
                ScenarioStage.Prepare,
                ScenarioStage.Deploy,
                ScenarioStage.Run,
            });

            var monitoringTasks = new List<Task>();

            while (stages.Count > 0)
            {
                var stageResult = await RunOrResumeStageAsync(stages.Dequeue(), cancellationToken);

                if (!stageResult.Success)
                    break;

                monitoringTasks.AddRange(stageResult.MonitoringTasks);
            }

            await Task.WhenAll(monitoringTasks);

            // This will make sure that the status will be updated after the last stage is finished
            // even in the case where the scenario has no nodes.
            OnNodeStatusChanged();

            DebugUtils.Trace("Scenario finished running.");
        }

        private async Task<StageRunResult> RunOrResumeStageAsync(ScenarioStage stage, CancellationToken cancellationToken)
        {
            DebugUtils.Trace($"Running stage {stage}.");
            var stageNodes = m_Stages[(int)stage].Nodes;
            List<Node> nodesToRun;
            int completed;

            var monitoringTasks = new List<Task>();

            while ((nodesToRun = GetNextNodesToRun(stageNodes, out completed)).Count > 0)
            {
                // If a domain reload or compilation is pending before the node execution, we wait for it to be completed as it can affect it.
                await WaitForPendingReloads();

                // If we're not running a node that is allowed to request a domain reload, we prevent domain reloads from this scope.
                using DomainReloadScopeLock domainReloadLock = CanRequestDomainReload(nodesToRun) ? null : new DomainReloadScopeLock();

                var tasks = new List<Task>();
                foreach (var node in nodesToRun)
                {
                    switch (node.State)
                    {
                        case NodeState.Idle:
                            FlushInputs(node);
                            tasks.Add(node.RunAsync(cancellationToken).ContinueWith(t => monitoringTasks.Add(t.Result.MonitoringTask)));
                            break;
                        case NodeState.Running:
                        case NodeState.Active:
                            tasks.Add(node.ResumeAsync(cancellationToken).ContinueWith(t => monitoringTasks.Add(t.Result.MonitoringTask)));
                            break;
                    }
                }

                await Task.WhenAll(tasks);
            }

            DebugUtils.Trace($"Stage {stage} finished running. Total completed nodes: {completed}.");

            return new StageRunResult
            {
                Success = completed == stageNodes.Count,
                MonitoringTasks = monitoringTasks
            };
        }

        private static async Task WaitForPendingReloads()
        {
            // Give it a chance to flush the domain reload requests.
            await Task.Yield();

            while (InternalUtilities.IsDomainReloadRequested() || EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Delay(100);
        }

        /// <summary>
        /// Fill the dependencies map with the current state of the scenario.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        private void CacheDataForExecution()
        {
            if (m_Dependencies == null)
                m_Dependencies = new();
            else
                m_Dependencies.Clear();

            var nodesInInvalidState = 0;
            foreach (var stage in m_Stages)
                foreach (var node in stage.Nodes)
                {
                    switch (node.State)
                    {
                        case NodeState.Idle:
                        case NodeState.Running:
                        case NodeState.Completed:
                            break;
                        case NodeState.Error:
                        case NodeState.Aborted:
                            nodesInInvalidState++;
                            break;
                    }
                }

            if (nodesInInvalidState > 0)
                throw new System.InvalidOperationException("Cannot run or resume a scenario with nodes in error or aborted state.");

            foreach (var connection in m_ConnectedInputs ?? Enumerable.Empty<NodeInput>())
            {
                if (!m_Dependencies.TryGetValue(connection.GetNode(), out var dependencies))
                {
                    dependencies = new HashSet<NodeInput>();
                    m_Dependencies.Add(connection.GetNode(), dependencies);
                }

                dependencies.Add(connection);
            }
        }

        /// <summary>
        /// Get the nodes whose dependencies are already completed.
        /// </summary>
        /// <param name="candidates">The candidate nodes</param>
        /// <param name="completed">The number of candidate nodes that are completed</param>
        /// <returns></returns>
        private List<Node> GetNextNodesToRun(IEnumerable<Node> candidates, out int completed)
        {
            var availableNodes = new List<Node>();
            var availableIsolationNode = (Node)null;
            completed = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.State == NodeState.Completed || candidate.State == NodeState.Active)
                    completed++;

                if (NodeFinishedExecution(candidate))
                    continue;

                if (candidate.State != NodeState.Idle && !candidate.Interrupted)
                    continue;

                if (HasAllDependenciesCompleted(candidate))
                {
                    if (HasToRunInIsolation(candidate))
                        availableIsolationNode = candidate;
                    else
                        availableNodes.Add(candidate);
                }
            }

            // If there is a node that needs to run in isolation, we run that one first before any other node.
            if (availableIsolationNode != null)
            {
                availableNodes.Clear();
                availableNodes.Add(availableIsolationNode);
            }

            return availableNodes;
        }

        /// <summary>
        /// Returns true if the node ran and completed its execution,
        /// either successfully or with an error.
        /// </summary>
        private static bool NodeFinishedExecution(Node node)
        {
            if (node.State == NodeState.Completed ||
                node.State == NodeState.Error ||
                node.State == NodeState.Aborted)
                return true;

            return false;
        }

        private void FlushInputs(Node node)
        {
            if (!m_Dependencies.TryGetValue(node, out var connections))
                return;

            foreach (var connection in connections)
                connection.Flush();
        }

        private static bool HasAllConnectionsCompleted(IEnumerable<NodeInput> connections)
        {
            foreach (var connection in connections)
            {
                var dependency = connection.GetSource().GetNode();
                if (dependency.State != NodeState.Completed && dependency.State != NodeState.Active)
                    return false;
            }

            return true;
        }

        private bool HasAllDependenciesCompleted(Node node)
        {
            if (!m_Dependencies.TryGetValue(node, out var dependencies))
                return true;

            return HasAllConnectionsCompleted(dependencies);
        }

        private static bool HasToRunInIsolation(Node node)
            => CanRequestDomainReload(node);

        private static bool CanRequestDomainReload(Node node)
            => node.GetType().IsDefined(typeof(CanRequestDomainReloadAttribute), true);

        private static bool CanRequestDomainReload(List<Node> nodes)
            => nodes.Count == 1 && CanRequestDomainReload(nodes[0]);
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    // Implementation of the scenario data model.
    // This file contains all the logic and APIs to manage the scenario data.
    // Here it is ensured that the scenario graph data is consistent and valid.
    // For the orchestration of the scenario, see ScenarioOrchestration.cs.
    [Serializable]
    internal partial class Scenario : ISerializationCallbackReceiver
    {
        [Serializable]
        private struct NodesCollection
        {
            [SerializeReference] public List<Node> Nodes;
        }

        [SerializeField] private string m_Name;
        [SerializeField] private NodesCollection[] m_Stages;
        [SerializeReference] private List<NodeInput> m_ConnectedInputs;
        [SerializeField] private bool m_HasStarted;

        [NonSerialized] private ScenarioStatus m_Status;

        public string Name => m_Name;
        public ScenarioStatus Status => m_Status;

        public event Action<ScenarioStatus> StatusChanged;

        public Scenario(string name)
        {
            m_Name = name;

            var stageNames = Enum.GetNames(typeof(ScenarioStage));
            m_Stages = new NodesCollection[stageNames.Length];
            for (var i = 0; i < stageNames.Length; i++)
            {
                Assert.IsTrue(Enum.Parse<ScenarioStage>(stageNames[i]) == (ScenarioStage)i, $"Enum value mismatch for {stageNames[i]}");
                m_Stages[i] = new NodesCollection { Nodes = new List<Node>() };
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            RefreshStatus();
            Assert.AreEqual(m_HasStarted, m_Status.State != ScenarioState.Idle, $"Mismatch between m_HasStarted and m_Status.State: {m_HasStarted} - {m_Status.State}");

            foreach (var stage in m_Stages)
                foreach (var node in stage.Nodes)
                    SetupNodeEvents(node);

            if (m_Stages.Length == 0)
                m_Status = ScenarioStatus.Invalid;
        }

        private void OnNodeProgressChanged(float progress) => OnNodeStatusChanged();
        private void OnNodeStateChanged(NodeState state) => OnNodeStatusChanged();
        private void OnNodeStatusChanged()
        {
            var previousStatus = Status;
            RefreshStatus();

            if (!previousStatus.Equals(Status))
                StatusChanged?.Invoke(Status);
        }

        private void SetupNodeEvents(Node node)
        {
            node.ProgressChanged -= OnNodeProgressChanged;
            node.ProgressChanged += OnNodeProgressChanged;

            node.StateChanged -= OnNodeStateChanged;
            node.StateChanged += OnNodeStateChanged;
        }

        // TODO: Add profiler markers
        private void RefreshStatus()
        {
            if (!m_HasStarted)
            {
                m_Status = new ScenarioStatus();
                return;
            }

            var totalNodes = 0;
            var progressSum = 0.0f;
            var idleNodes = 0;
            var runningNodes = 0;
            var completedNodes = 0;
            var activeNodes = 0;
            var failedNodes = 0;
            var abortedNodes = 0;
            var currentStageState = NodeState.Invalid;
            var currentStage = ScenarioStage.None;
            var errorMessages = new List<string>();
            var nodeStatuses = new List<NodeStatus>();
            var stageStates = new NodeState[m_Stages.Length];

            var stagesCount = m_Stages?.Length ?? 0;

            if (stagesCount == 0)
            {
                m_Status = ScenarioStatus.Invalid;
                return;
            }

            for (var i = stagesCount - 1; i >= 0; i--)
            {
                var stage = m_Stages[i];

                if (stage.Nodes == null)
                {
                    m_Status = ScenarioStatus.Invalid;
                    return;
                }

                ComputeStageState(
                    stage,
                    out var stageState,
                    progressSum: out var stageProgressSum,
                    idleNodes: out var stageIdleNodes,
                    runningNodes: out var stageRunningNodes,
                    completedNodes: out var stageCompletedNodes,
                    activeNodes: out var stageActiveNodes,
                    failedNodes: out var stageFailedNodes,
                    abortedNodes: out var stageAbortedNodes,
                    errorMessages: ref errorMessages,
                    nodeStatuses: ref nodeStatuses
                );

                progressSum += stageProgressSum;
                idleNodes += stageIdleNodes;
                runningNodes += stageRunningNodes;
                completedNodes += stageCompletedNodes;
                activeNodes += stageActiveNodes;
                failedNodes += stageFailedNodes;
                abortedNodes += stageAbortedNodes;

                totalNodes += stage.Nodes.Count;

                stageStates[i] = stageState;

                if ((stageState != NodeState.Completed && stageState != NodeState.Active) || i == stagesCount - 1)
                {
                    currentStage = (ScenarioStage)i;
                    currentStageState = stageState;
                }
            }

            var progress = progressSum / totalNodes;
            ScenarioState scenarioState;

            if (idleNodes == totalNodes && totalNodes > 0)
                scenarioState = ScenarioState.Idle;
            else if (failedNodes > 0)
                scenarioState = ScenarioState.Failed;
            else if (abortedNodes > 0)
                scenarioState = ScenarioState.Aborted;
            else if (completedNodes == totalNodes)
                scenarioState = ScenarioState.Completed;
            else
                scenarioState = ScenarioState.Running;

            m_Status = new ScenarioStatus
            {
                State = scenarioState,
                CurrentStage = currentStage,
                StageState = currentStageState,
                Progress = progress,
                ErrorMessages = errorMessages,
                NodeStateReports = nodeStatuses,
                StageStates = stageStates
            };
        }

        private static void ComputeStageState(
            NodesCollection nodes,
            out NodeState state,
            out float progressSum,
            out int idleNodes,
            out int runningNodes,
            out int completedNodes,
            out int activeNodes,
            out int failedNodes,
            out int abortedNodes,
            ref List<string> errorMessages,
            ref List<NodeStatus> nodeStatuses)
        {
            state = NodeState.Invalid;

            progressSum = 0.0f;
            idleNodes = 0;
            runningNodes = 0;
            completedNodes = 0;
            activeNodes = 0;
            failedNodes = 0;
            abortedNodes = 0;

            foreach (var node in nodes.Nodes)
            {
                progressSum += node.Progress;

                switch (node.State)
                {
                    case NodeState.Idle:
                        idleNodes++;
                        break;
                    case NodeState.Running:
                        runningNodes++;
                        break;
                    case NodeState.Completed:
                        completedNodes++;
                        break;
                    case NodeState.Active:
                        activeNodes++;
                        break;
                    case NodeState.Error:
                        failedNodes++;
                        errorMessages.Add(node.ErrorMessage);
                        break;
                    case NodeState.Aborted:
                        abortedNodes++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                nodeStatuses.Add(new NodeStatus(node.Name, node.State, node.TimeData, node.Progress));
            }

            if (failedNodes > 0)
                state = NodeState.Error;
            else if (abortedNodes > 0)
                state = NodeState.Aborted;
            else if (runningNodes > 0)
                state = NodeState.Running;
            else if (activeNodes > 0)
                state = NodeState.Active;
            else if (idleNodes > 0)
                state = NodeState.Idle;
            else if (completedNodes == nodes.Nodes.Count)
                state = NodeState.Completed;
        }

        internal ReadOnlyCollection<Node> GetNodes(ScenarioStage stage)
        {
            if (m_Stages == null || m_Stages.Length == 0)
                return new List<Node>().AsReadOnly();
            return m_Stages[(int)stage].Nodes.AsReadOnly();
        }

        private void ValidateHasNotStarted()
        {
            if (m_HasStarted)
                throw new InvalidOperationException("Trying to modify a scenario that has already started.");
        }

        public void AddNode(Node node, ScenarioStage stage)
        {
            ValidateHasNotStarted();

            if (stage == ScenarioStage.None)
                throw new ArgumentException("Stage cannot be None", nameof(stage));

            if (m_Stages.SelectMany(s => s.Nodes).Any(n => n.Name == node.Name))
                throw new ArgumentException($"Node with the same name already exists [{node.Name}].");

            if (node.State != NodeState.Idle)
                throw new InvalidOperationException("Trying to add a node that is not in idle state.");

            m_Stages[(int)stage].Nodes.Add(node);

            SetupNodeEvents(node);
        }

        public void ConnectConstant<T>(NodeInput<T> input, T value)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            ValidateHasNotStarted();

            input.SetValue(value);
        }

        public void Connect<T>(NodeOutput<T> from, NodeInput<T> to)
        {
            if (from is null)
                throw new ArgumentNullException(nameof(from));

            if (to is null)
                throw new ArgumentNullException(nameof(to));

            ValidateHasNotStarted();

            if (GetInputConnectionSlot(to) != -1)
                throw new InvalidOperationException("Input is already connected");

            to.Connect(from);

            if (m_ConnectedInputs == null)
                m_ConnectedInputs = new List<NodeInput>(1);

            m_ConnectedInputs.Add(to);
        }

        private int GetInputConnectionSlot<T>(NodeInput<T> input)
        {
            if (m_ConnectedInputs == null)
                return -1;

            for (int i = 0; i < m_ConnectedInputs.Count; i++)
                if (input == m_ConnectedInputs[i])
                    return i;

            return -1;
        }
    }
}

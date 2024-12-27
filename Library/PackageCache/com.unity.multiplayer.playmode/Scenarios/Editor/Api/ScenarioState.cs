using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Api
{
    /// <summary>
    /// Describes the running state of an scenario.
    /// </summary>
    internal enum ScenarioState
    {
        Idle,
        Running,
        Completed,
        Aborted,
        Failed
    }

    // The NodeStatus is a struct that is used to report the state of a node to the UI. As such it is a subset of the Node class that only contains what we want to show the the end user.
    // [?] Would we want to capture the errors here instead of at the scenario level ?
    internal struct NodeStatus
    {
        public string NodeName;
        public NodeState State;
        public NodeTimeData TimeData;
        public float Progress;

        public NodeStatus(string nodeName, NodeState state, NodeTimeData timeData, float progress)
        {
            NodeName = nodeName;
            State = state;
            TimeData = timeData;
            Progress = progress;
        }
    }

    /// <summary>
    /// Aggregates all the values that describe the current state of a scenario.
    /// </summary>
    [Serializable]
    internal struct ScenarioStatus
    {
        public ScenarioState State;
        public ScenarioStage CurrentStage;
        public NodeState StageState;
        public float Progress;
        public List<string> ErrorMessages;
        public List<NodeStatus> NodeStateReports;
        public NodeState[] StageStates;

        public static readonly ScenarioStatus Default = new(ScenarioStage.None, NodeState.Idle);
        public static readonly ScenarioStatus Invalid = new(ScenarioStage.None, NodeState.Invalid);

        public ScenarioStatus(ScenarioStage stage = ScenarioStage.None, NodeState state = NodeState.Idle)
        {
            State = ScenarioState.Idle;
            CurrentStage = stage;
            StageState = state;
            Progress = 0;
            ErrorMessages = new List<string>();
            NodeStateReports = new List<NodeStatus>();
            StageStates = new NodeState[0];
        }

        override public string ToString()
        {
            return $"ScenarioState: {State}, CurrentStage: {CurrentStage}, StageState: {StageState}, Progress: {Progress}, ErrorMessages: {ErrorMessages?.Count}, NodesStatus: {NodeStateReports?.Count}";
        }
    }
}

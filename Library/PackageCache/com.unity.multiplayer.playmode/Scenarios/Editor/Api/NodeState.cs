namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Api
{
    /// <summary>
    /// Represents the state of a node in a graph
    ///
    /// </summary>
    enum NodeState
    {
        /// <summary>
        /// Node has failed and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Error,
        /// <summary>
        /// Initial state; Will transition to Running or Error when the Node
        /// </summary>
        Idle,
        /// <summary>
        /// Node is running; building, uploading something, starting a process, etc. Will transition to Completed or Error when done
        /// </summary>
        Running,
        /// <summary>
        /// Run node is active and waiting for the process to finish. Will transition to Completed or Error when done
        /// </summary>
        Active,
        /// <summary>
        /// Node has completed its tasks successfully and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Completed,
        /// <summary>
        /// Node has been aborted and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Aborted,
        /// <summary>
        /// Node is invalid and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Invalid = int.MaxValue
    }
}

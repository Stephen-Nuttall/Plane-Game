namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    internal interface IConnectableNode
    {
        public NodeInput<ConnectionData> ConnectionDataIn { get; }
        public NodeOutput<ConnectionData> ConnectionDataOut { get; }
    }
}

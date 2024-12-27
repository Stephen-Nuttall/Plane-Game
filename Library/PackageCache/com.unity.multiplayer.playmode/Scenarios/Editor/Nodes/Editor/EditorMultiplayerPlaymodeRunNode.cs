using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using Unity.Multiplayer.PlayMode.Common.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor
{
    [CanRequestDomainReload]
    class EditorMultiplayerPlaymodeRunNode : Node, IInstanceRunNode
    {
        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionDataIn; // Nodes needs to be public fields since they are serialized
        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut; // Nodes needs to be public fields since they are serialized
        [SerializeReference] public NodeOutput<int> ProcessId;
        [SerializeReference] public NodeInput<string> PlayerTags;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionDataIn;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        [SerializeField] private int m_ProcessId;

        public UnityPlayer GetPlayer() => MultiplayerPlaymode.Players[GetInput(PlayerInstanceIndex)];
        public bool IsRunning() => MultiplayerPlaymode.Players[GetInput(PlayerInstanceIndex)].PlayerState == PlayerState.Launched;

        public EditorMultiplayerPlaymodeRunNode(string name) : base(name)
        {
            PlayerInstanceIndex = new(this);
            ConnectionDataIn = new(this);
            PlayerTags = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);

            ConnectionDataOut = new(this);
            ProcessId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = GetPlayer();
            var connectionData = GetInput(ConnectionDataIn);
            var streamLogs = GetInput(StreamLogs);
            CurrentScenarioConnectionData.connection = connectionData;
            SetOutput(ConnectionDataOut, connectionData ?? new ConnectionData());

            Debug.Assert(player.PlayerState == PlayerState.Launched);

            m_ProcessId = MultiplayerPlaymodeEditorUtility.GetProcessID(player);
            SetOutput(ProcessId, m_ProcessId);

            SetupListeningLogs(streamLogs);

            SendSyncStateMessage(player, new CloneState()
            {
                StreamLogsToMainEditor = streamLogs
            });

            if (player.Type == PlayerType.Main)
            {
                EditorApplication.EnterPlaymode();
            }

            // Entering play mode could take a few frames, so we wait until the state changes (clone receive a message to go into playmode from MPPM)
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) { await Task.Delay(100); }
        }

        protected override async Task ExecuteResumeAsync(CancellationToken cancellationToken)
        {
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) { await Task.Delay(100); }

            Assert.IsTrue(EditorApplication.isPlaying, $"Editor should be already in play mode when resuming the editor play node. isPlaying: {EditorApplication.isPlaying}, isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            SetupListeningLogs(GetInput(StreamLogs));

            var playerInstanceIndex = GetInput(PlayerInstanceIndex);
            var player = MultiplayerPlaymode.Players[playerInstanceIndex];
            while (!cancellationToken.IsCancellationRequested) { await Task.Delay(100); }

            SendSyncStateMessage(player, new CloneState()
            {
                StreamLogsToMainEditor = false
            });
            SetupListeningLogs(false);

            if (player.Type == PlayerType.Main)
            {
                EditorApplication.ExitPlaymode();
            }
            // Wait until the players (including main player) are out of playmode
            while (EditorApplication.isPlaying) { await Task.Delay(100); }


            var hasDeactivated = player.Deactivate(out _);
            DebugUtils.Trace(hasDeactivated
                ? $"Successfully deactivated '{player.Name}'"
                : $"Failed to deactivate '{player.Name}'");

            var tags = GetInput(PlayerTags).Split('|');
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (player.RemoveTag(tag, out var tagError)) continue;

                Debug.Log($"Could not remove tag '{tag}'. Reason[{tagError}]");
            }
        }

        private void OnLogMessageReceived(VirtualProjectIdentifier identifier, string message, string stackTrace, LogType type)
        {
            var player = GetPlayer();
            if (player.TypeDependentPlayerInfo.VirtualProjectIdentifier != identifier)
                return;

            IInstanceRunNode.PrintReceivedLog(player.Name, GetInput(LogsColor), message, type);
        }

        private void SetupListeningLogs(bool listen)
        {
            if (GetPlayer().Type == PlayerType.Main)
                return;

            VirtualProjectWorkflow.WorkflowMainEditorContext.MainPlayerSystems.ApplicationEvents.LogMessageReceived -= OnLogMessageReceived;
            if (listen)
                VirtualProjectWorkflow.WorkflowMainEditorContext.MainPlayerSystems.ApplicationEvents.LogMessageReceived += OnLogMessageReceived;
        }

        private static void SendSyncStateMessage(UnityPlayer player, CloneState state)
        {
            if (player.Type == PlayerType.Main)
                return;

            EditorContexts.MainEditorContext.MessagingService.Send(new SyncStateMessage(state), player.TypeDependentPlayerInfo.VirtualProjectIdentifier);
        }
    }
}

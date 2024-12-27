using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using UnityEditor.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor
{
    class EditorMultiplayerPlaymodeDeployNode : Node
    {
        bool m_HasConnected;
#if UNITY_USE_MULTIPLAYER_ROLES

        [SerializeReference] public NodeInput<MultiplayerRoleFlags> MultiplayerRole;
#endif
        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;
        [SerializeReference] public NodeInput<SceneAsset> InitialScene;
        [SerializeReference] public NodeInput<string> PlayerTags;
        [SerializeReference] public NodeOutput<PlayerIdentifier> PlayerIdentifier; // Nodes needs to be public fields since they are serialized
        [SerializeReference] public NodeOutput<TypeDependentPlayerInfo> TypeDependentPlayerInfo; // Nodes needs to be public fields since they are serialized
        [SerializeField] private SceneSetup[] m_CurrentSceneSetup;
        [SerializeField] private string m_SentMessage;

        public EditorMultiplayerPlaymodeDeployNode(string name) : base(name)
        {
            PlayerInstanceIndex = new(this);
            InitialScene = new(this);
#if UNITY_USE_MULTIPLAYER_ROLES
            MultiplayerRole = new(this);
#endif
            PlayerTags = new(this);

            PlayerIdentifier = new(this);
            TypeDependentPlayerInfo = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var playerInstanceIndex = GetInput(PlayerInstanceIndex);
            var player = MultiplayerPlaymode.Players[playerInstanceIndex];

            try
            {
                DebugUtils.Trace($"Deploy started for '{player.Name}'");
                var hasActivated = player.Activate(out _);
                DebugUtils.Trace(hasActivated
                    ? $"Successfully activated '{player.Name}'"
                    : $"Failed to activate '{player.Name}'");

                SetupAndLoadInitialScene();

                if (hasActivated)
                {
                    // Clear Tags on Player to be refreshed with new ones.
                    player.ClearTags(out var clearError);
                    if (clearError != TagError.None)
                    {
                        Debug.LogWarning($"Could not refresh tags for player '{player.Name}': {clearError}");
                    }

                    // Set Tags
                    var tags = GetInput(PlayerTags).Split('|');
                    foreach (var tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;
                        if (player.Tags.Contains(tag)) continue;
                        if (player.AddTag(tag, out var tagError)) continue;

                        Debug.Log($"Could not add tag '{tag}'. Reason[{tagError}]");
                    }

#if UNITY_USE_MULTIPLAYER_ROLES
                    // Set role
                    player.Role = GetInput(MultiplayerRole);
#endif

                    // Activating at first could take a while
                    // 1. Could be symbolic linking the MPPM folder
                    // 2. Could be launching the process
                    while (player.PlayerState != PlayerState.Launched)
                    {
                        await Task.Delay(3000, cancellationToken);

                        if (player.PlayerState != PlayerState.Launched)
                        {
                            DebugUtils.Trace($"'{player.Name}' is not in the ready state");
                        }
                        else
                        {
                            if (!m_HasConnected)
                            {
                                m_HasConnected = true;
                                DebugUtils.Trace($"'{player.Name}' is ready!");
                            }
                        }
                    }
                }

                DebugUtils.Trace($"Deploy finished for '{player.Name}'");

                SetOutput(PlayerIdentifier, player.PlayerIdentifier);
                SetOutput(TypeDependentPlayerInfo, player.TypeDependentPlayerInfo);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // Perform Cleanup on node task cancellation
                CleanupInitialScene();
                player.ClearTags(out _);

                DebugUtils.Trace($"Play mode cancelled, deactivating '{player.Name}'.");
                player.Deactivate(out _);
                return;
            }
        }

        protected override Task ExecuteResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);

                if ((DateTime.UtcNow - startTime).TotalSeconds > 20)
                {
                    if (!string.IsNullOrWhiteSpace(m_SentMessage) && EditorApplication.isPlaying)
                    {
                        startTime = DateTime.UtcNow;
                        DebugUtils.Trace($"Sending to clone runtime scene {m_SentMessage}");
                        var mainEditorContext = Playmode.VirtualProjects.Editor.EditorContexts.MainEditorContext;
                        var m = new RuntimeSceneSwitchMessage(m_SentMessage);
                        mainEditorContext.MessagingService.Broadcast(m, () => { m_SentMessage = null; }, Debug.LogWarning);
                    }
                }
            }

            // Perform Cleanup on node task Monitoring completion
            CleanupInitialScene();
        }

        private void SetupAndLoadInitialScene()
        {
            // Only the main player (Editor) performs Initial Scene Setup.
            if (!IsMainEditor())
            {
                return;
            }

            // If the scene is already active (is EditMode-focused scene), there's nothing to do.
            var initialScene = GetInput(InitialScene);
            var currentScene = SceneManager.GetActiveScene();
            if (initialScene == null || AssetDatabase.GetAssetPath(initialScene) == currentScene.path)
            {
                return;
            }

            // Save current scene states for the Main Editor to restore back to.
            m_CurrentSceneSetup = EditorSceneManager.GetSceneManagerSetup();

            // Set the initial scene as the active one.
            string initialScenePath = AssetDatabase.GetAssetPath(initialScene);
            EditorSceneManager.OpenScene(initialScenePath);

            // Schedule a RuntimeSceneSwitchMessage scene for clones
            m_SentMessage = initialScene.name;
        }

        private void CleanupInitialScene()
        {
            // Only the main player (Editor) performs Initial Scene Cleanup.
            if (!IsMainEditor())
            {
                return;
            }

            // If there's nothing to clean up, return.
            if (m_CurrentSceneSetup == null || m_CurrentSceneSetup.Length == 0)
            {
                return;
            }

            // If during cleanup the Editor is in playmode, re-attempt a CleanupInitialScene
            // call with a OnNotifyEnteredEditMode listener.
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged -= OnNotifyEnteredEditMode;
                EditorApplication.playModeStateChanged += OnNotifyEnteredEditMode;
                return;
            }

            EditorApplication.playModeStateChanged -= OnNotifyEnteredEditMode;

            // Restore active Editor scene, if any.
            EditorSceneManager.RestoreSceneManagerSetup(m_CurrentSceneSetup);
            m_CurrentSceneSetup = null;
        }

        private bool IsMainEditor()
        {
            var playerInstanceIndex = GetInput(PlayerInstanceIndex);
            var player = MultiplayerPlaymode.Players[playerInstanceIndex];
            return player.Type == PlayerType.Main;
        }

        private void OnNotifyEnteredEditMode(PlayModeStateChange state)
        {
            // Fire off CleanupInitialScene upon return to Edit mode.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                CleanupInitialScene();
            }
        }
    }
}

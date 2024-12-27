#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor
{
    static class CurrentScenarioConnectionData
    {
        public static ConnectionData connection;
    }

    [CanRequestDomainReload]
    class EditorPlayNode : Node, IInstanceRunNode
    {
#if UNITY_USE_MULTIPLAYER_ROLES
        [SerializeReference] public NodeInput<MultiplayerRoleFlags> MultiplayerRole;
#endif
        [SerializeReference] public NodeInput<SceneAsset> InitialScene;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;
        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        public bool IsRunning() => EditorApplication.isPlaying;

        public EditorPlayNode(string name) : base(name)
        {
#if UNITY_USE_MULTIPLAYER_ROLES
            MultiplayerRole = new(this);
#endif
            InitialScene = new(this);
            ConnectionData = new(this);

            ConnectionDataOut = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var initialScene = GetInput(InitialScene);
            var connectionData = GetInput(ConnectionData);
#if UNITY_USE_MULTIPLAYER_ROLES
            var multiplayerRole = GetInput(MultiplayerRole);
#endif

            var currentScene = SceneManager.GetActiveScene();
            if (initialScene != null && AssetDatabase.GetAssetPath(initialScene) != currentScene.path)
            {
                EditorApplication.playModeStateChanged -= LoadInitialScene;
                EditorApplication.playModeStateChanged += LoadInitialScene;
            }

            CurrentScenarioConnectionData.connection = connectionData;
            SetOutput(ConnectionDataOut, connectionData);

#if UNITY_USE_MULTIPLAYER_ROLES
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask = multiplayerRole;
#endif
            EditorApplication.EnterPlaymode();

            // Entering play mode could take a few frames, so we wait until the state changes.
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode)
                await Task.Delay(100);
        }

        protected override async Task ExecuteResumeAsync(CancellationToken cancellationToken)
        {
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode)
                await Task.Delay(100);

            Assert.IsTrue(EditorApplication.isPlaying, $"Editor should be already in play mode when resuming the editor play node. isPlaying: {EditorApplication.isPlaying}, isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");
        }

        private EditorBuildSettingsScene[] m_OriginalScenes;
        private void LoadInitialScene(PlayModeStateChange state)
        {
            var initialScene = GetInput(InitialScene);

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Make sure the scene is part of the build settings.
                m_OriginalScenes = EditorBuildSettings.scenes;
                var scenePath = AssetDatabase.GetAssetPath(initialScene);
                var isSceneInSettings = false;
                foreach (var scene in m_OriginalScenes)
                {
                    if (scene.path == scenePath)
                    {
                        isSceneInSettings = true;
                        break;
                    }
                }

                if (!isSceneInSettings)
                {
                    var newScenes = m_OriginalScenes.ToList();
                    newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    EditorBuildSettings.scenes = newScenes.ToArray();
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= LoadInitialScene;
                SceneManager.LoadScene(initialScene.name, LoadSceneMode.Single);

                EditorBuildSettings.scenes = m_OriginalScenes;
                m_OriginalScenes = null;
            }
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            EditorApplication.ExitPlaymode();
        }
    }
}

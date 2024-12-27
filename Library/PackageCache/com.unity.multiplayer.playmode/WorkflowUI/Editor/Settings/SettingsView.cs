using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Playmode.WorkflowUI.Editor
{
    class SettingsView : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UXMLWorkflowRoot}/Settings/SettingsView.uxml";

        public Toggle IsMppmActiveToggle => this.Q<Toggle>(nameof(IsMppmActiveToggle));
        public Toggle ShowLaunchScreenToggle => this.Q<Toggle>(nameof(ShowLaunchScreenToggle));
        public Toggle MutePlayersToggle => this.Q<Toggle>(nameof(MutePlayersToggle));
        public IntegerField AssetDatabaseRefreshTimeoutSlider => this.Q<IntegerField>(nameof(AssetDatabaseRefreshTimeoutSlider));

        public SettingsView()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);
        }
    }
}

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Playmode.WorkflowUI.Editor
{
    class MainView : VisualElement
    {
        public const string k_HasCompileErrorsClassName = "hasCompileErrors";
        public const string k_HasMPPMDisabled = "hasMPPMDisabled";
        public const string k_HasPlayerLaunchingClassName = "hasPlayerLaunching";
        public const string k_VirtualListViewName = "VirtualListView";
        static readonly string UXML = $"{UXMLPaths.UXMLWorkflowRoot}/Views/MainView.uxml";
        public readonly PlayersListView MainListView;
        public readonly PlayersListView VirtualListView;

        public MainView()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            MainListView = this.Q<PlayersListView>(nameof(MainListView));
            VirtualListView = this.Q<PlayersListView>(nameof(VirtualListView));
        }
    }
}

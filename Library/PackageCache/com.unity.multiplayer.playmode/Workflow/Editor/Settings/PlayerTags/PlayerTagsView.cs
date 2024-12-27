using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class PlayerTagsView : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UXMLWorkflowRoot}/Settings/PlayerTags/PlayerTagsView.uxml";
        internal ListView PlayerTagsList => this.Q<ListView>(nameof(PlayerTagsList));
        Button AddTagButton => this.Q<Button>(nameof(AddTagButton));
        Button RemoveTagButton => this.Q<Button>(nameof(RemoveTagButton));

        public event Action AddTagEvent;
        public event Action RemoveTagEvent;

        public PlayerTagsView()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);
            AddTagButton.clicked += () => AddTagEvent?.Invoke();
            RemoveTagButton.clicked += () => RemoveTagEvent?.Invoke();
        }

        public static void BindData(PlayerTagsView playerTagsView, UnityPlayerTags playerTagsViewModel)
        {
            playerTagsView.PlayerTagsList.itemsSource = playerTagsViewModel.Tags;
            playerTagsView.PlayerTagsList.bindItem += (element, i) => { ((Label)element).text = playerTagsViewModel.Tags[i]; };
            playerTagsView.PlayerTagsList.makeItem = () => new Label();
        }
    }
}

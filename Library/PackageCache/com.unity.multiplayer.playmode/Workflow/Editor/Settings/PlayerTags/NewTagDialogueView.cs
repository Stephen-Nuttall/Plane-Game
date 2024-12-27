using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class NewTagDialogueView : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UXMLWorkflowRoot}/Settings/PlayerTags/NewTagDialogueView.uxml";

        Button SaveButton => this.Q<Button>(nameof(SaveButton));
        Button CancelButton => this.Q<Button>(nameof(CancelButton));
        public TextField TagField => this.Q<TextField>(nameof(TagField));

        public Action OnSaveSelectedEvent;
        public Action OnCancelSelectedEvent;

        public NewTagDialogueView()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);
            RegisterCallback((EventCallback<AttachToPanelEvent>)OnAttach);
            RegisterCallback((EventCallback<DetachFromPanelEvent>)OnDetach);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            CancelButton.clicked -= OnCancelButtonClicked;
            SaveButton.clicked -= OnSaveButtonClicked;
            TagField.UnregisterCallback<KeyDownEvent>(OnTagFieldKeyDown);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            TagField.RegisterCallback<KeyDownEvent>(OnTagFieldKeyDown);
            SaveButton.clicked += OnSaveButtonClicked;
            CancelButton.clicked += OnCancelButtonClicked;
            TagField.Focus();
        }

        void OnTagFieldKeyDown(KeyDownEvent keyboardEvent)
        {
            if (keyboardEvent.keyCode != KeyCode.Return && keyboardEvent.keyCode != KeyCode.KeypadEnter) return;

            OnSaveSelectedEvent.Invoke();
        }

        void OnCancelButtonClicked() => OnCancelSelectedEvent?.Invoke();
        void OnSaveButtonClicked() => OnSaveSelectedEvent?.Invoke();
    }
}

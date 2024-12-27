using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class WindowLayoutPopoutWindow : PopupWindowContent
    {
        public readonly WindowLayoutCheckboxes CheckBoxes = new WindowLayoutCheckboxes();
        public event Action OpenEvent;
        public event Action CloseEvent;

        public override void OnOpen()
        {
            editorWindow.rootVisualElement.Add(CheckBoxes);
            OpenEvent?.Invoke();
        }

        public override void OnClose()
        {
            editorWindow.rootVisualElement.Clear();
            CloseEvent?.Invoke();
        }

        public override void OnGUI(Rect rect)
        {
        }

        public override Vector2 GetWindowSize()
        {
            // There doesn't seem to be a way to grab the height of elements dynamically because of how early this
            // is called (basically all the elements are 0 since they are calculating their sizes as well)
            // Use the known size and just dynamically create the final sizes based off of the mode and number of elements.
            // Use +1 for apply button
            var height = (CheckBoxes.Toggles.Length + 1) * WindowLayoutCheckboxes.ToggleDimensions.y;
            var width = WindowLayoutCheckboxes.ToggleDimensions.x;
            return new Vector2(width, height);
        }
    }
}

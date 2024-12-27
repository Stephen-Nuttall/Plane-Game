using System;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor.Bridge
{
    internal class ToolbarExtensions
    {
        public static event Action<VisualElement> CreatingPlayModeButtons
        {
            add => UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated += value;
            remove => UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated -= value;
        }
    }
}

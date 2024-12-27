using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    class PlaymodeDropdownButton : EditorToolbarButton
    {
        const string k_Stylesheet = "Configurations/Editor/GUI/PlaymodeDropdownButton.uss";
        const string k_PlaymodeDropdownName = "playmode-dropdown";

        public PlaymodeDropdownButton()
        {
            name = k_PlaymodeDropdownName;
            var arrow = new VisualElement();
            icon = EditorGUIUtility.FindTexture("UnityLogo");
            arrow.AddToClassList("unity-icon-arrow");
            Add(arrow);
            RegisterCallback<ClickEvent>(evt => ShowPlaymodeConfigPopup());
            UIUtils.ApplyStyleSheetAsync(k_Stylesheet, this).Forget();
            UpdateUI(PlayModeManager.instance.ActivePlayModeConfig);
        }

        public void UpdateUI(PlayModeConfig config)
        {
            text = config.name;
            ValidateCurrentConfiguration(config);
        }

        void ValidateCurrentConfiguration(PlayModeConfig config)
        {
            icon = config.Icon;
            tooltip = string.IsNullOrEmpty(config.Description) ? "" : config.Description;

            if (!config.IsConfigurationValid(out var error))
            {
                icon = EditorGUIUtility.FindTexture("console.warnicon");
                tooltip = error;
            }
        }

        void ShowPlaymodeConfigPopup()
        {
            UnityEditor.PopupWindow.Show(new Rect(worldBound.x + worldBound.width - PlaymodePopupContent.windowSize.x, worldBound.y, worldBound.width, worldBound.height), new PlaymodePopupContent());
        }
    }
}

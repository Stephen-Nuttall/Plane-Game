using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    internal class PlayModeScenariosWindow : EditorWindow
    {
        internal const string k_NewConfigurationButtonName = "NewConfigurationButton";
        internal const string k_NewConfigurationButtonTooltip = "Create a new Play Mode Scenario";

        DetailView m_DetailView;
        HelpBox m_DisableEditingHelpbox;
        PlayModeListView m_PlayModeListView;

        [MenuItem("Window/Multiplayer/Play Mode Scenarios")]
        public static void ShowWindow()
        {
            GetWindow<PlayModeScenariosWindow>("Play Mode Scenarios");
        }

        public void OnEnable()
        {
            m_DisableEditingHelpbox = new HelpBox("Editing is not allowed in Playmode.", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_DisableEditingHelpbox);

            var toolbar = new Toolbar();
            m_PlayModeListView = new PlayModeListView();

            toolbar.Add(CreateNewScenarioMenu());
            rootVisualElement.Add(toolbar);

            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            m_DetailView = new DetailView();

            m_PlayModeListView.OnConfigSelected += SelectConfig;

            m_DetailView.SetConfig(m_PlayModeListView.ConfigurationToEdit);

            splitView.Add(m_PlayModeListView);
            splitView.Add(m_DetailView);

            SetHelpboxStatus();
            PlayModeManager.instance.StateChanged += (state) =>
            {
                SetHelpboxStatus();
            };
        }

        private VisualElement CreateNewScenarioMenu()
        {
            var configTypes = TypeCache.GetTypesWithAttribute<CreatePlayModeConfigurationMenuAttribute>();

            if (configTypes.Count == 0)
                throw new InvalidOperationException("No PlayModeConfig types found.");

            if (configTypes.Count == 1)
            {
                var configType = configTypes[0];
                if (!ValidatePlayModeConfigType(configType))
                    throw new InvalidOperationException("Invalid PlayModeConfig type.");

                var attribute = configType.GetCustomAttributes(typeof(CreatePlayModeConfigurationMenuAttribute), false).FirstOrDefault() as CreatePlayModeConfigurationMenuAttribute;
                var button = new ToolbarButton(() => NewScenarioAction(configType, attribute.NewItemName))
                {
                    name = k_NewConfigurationButtonName,
                    tooltip = k_NewConfigurationButtonTooltip,
                    iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("Toolbar Plus").image)
                };
                return button;
            }

            var toolbarMenu = new ToolbarMenu() { text = "+" };
            toolbarMenu.style.fontSize = 16;

            foreach (var configType in configTypes)
            {
                if (!ValidatePlayModeConfigType(configType))
                    continue;

                var attribute = configType.GetCustomAttributes(typeof(CreatePlayModeConfigurationMenuAttribute), false).FirstOrDefault() as CreatePlayModeConfigurationMenuAttribute;
                toolbarMenu.menu.AppendAction(attribute.Label, _ => { NewScenarioAction(configType, attribute.NewItemName); });
            }

            return toolbarMenu;
        }

        private void NewScenarioAction(Type configType, string newItemName)
            => m_PlayModeListView.ShowAddTextField(configType, newItemName);

        private static bool ValidatePlayModeConfigType(Type type)
        {
            if (!typeof(PlayModeConfig).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Type {type} is not a PlayModeConfig. Only types that inherit from PlayModeConfig are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return false;
            }

            if (type.IsAbstract)
            {
                Debug.LogWarning($"Type {type} is abstract. Only concrete types are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return false;
            }

            return true;
        }

        void SetHelpboxStatus()
        {
            m_DisableEditingHelpbox.style.display = PlayModeManager.instance.CurrentState == PlayModeState.Running ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            foreach (var visualElement in rootVisualElement.Children())
            {
                visualElement.enabledSelf = PlayModeManager.instance.CurrentState == PlayModeState.NotRunning;
            }

            m_DisableEditingHelpbox.enabledSelf = true;
        }

        private void SelectConfig(PlayModeConfig config)
        {
            m_DetailView.SetConfig(config);
        }
    }
}

using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    /// <summary>
    /// Shows one configuration that the user can change
    /// </summary>
    class DetailView : ScrollView
    {
        const string k_Stylesheet = "Configurations/Editor/GUI/PlayModeScenariosWindow/DetailView.uss";

        PlayModeConfig m_Config;
        VisualElement m_ConfigEditor;
        readonly VisualElement m_NoConfigSelectedView;

        public DetailView()
        {
            name = "playmodeconfig-detail-view";
            m_NoConfigSelectedView = NoConfigSelectedView();
            Add(m_NoConfigSelectedView);
            UIUtils.ApplyStyleSheet(k_Stylesheet, this);
        }

        internal void SetConfig(PlayModeConfig config)
        {
            m_Config = config;
            if (m_Config != null)
            {
                m_NoConfigSelectedView.style.display = DisplayStyle.None;
                UpdateView();
                return;
            }

            if (m_ConfigEditor != null)
                m_ConfigEditor.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            m_NoConfigSelectedView.style.display = DisplayStyle.Flex;
        }

        void UpdateView()
        {
            var so = new SerializedObject(m_Config);

            if (m_ConfigEditor == null)
            {
                m_ConfigEditor = new InspectorElement(so);
                Add(m_ConfigEditor);
            }
            m_ConfigEditor.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            m_ConfigEditor.Unbind();
            m_ConfigEditor.Bind(so);
        }

        VisualElement NoConfigSelectedView()
        {
            var container = new VisualElement();
            container.name = "no-config-selected-view";
            container.Add(new Label("No configuration selected"));
            return container;
        }
    }
}

using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Views
{
    class PlaymodeStatusWindow : EditorWindow
    {
        [MenuItem("Window/Multiplayer/Play Mode Status")]
        internal static void OpenWindow()
        {
            GetWindow<PlaymodeStatusWindow>();
        }

        private PlaymodeStatusElement m_ScenarioElement;

        private void OnEnable()
        {
            Debug.Log("ScenarioStatusWindow OnEnable");
            titleContent = new GUIContent("Play mode Status");

            Refresh(null);
            Scenario.ScenarioStarted += Refresh;
        }

        private void OnFocus()
        {
            Refresh(null);
        }

        private void Refresh(Scenario scenario)
        {
            rootVisualElement.Clear();


            m_ScenarioElement = new PlaymodeStatusElement();
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}

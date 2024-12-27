using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Debugging.Editor
{
    internal class ScenarioStatusWindow : EditorWindow
    {
        private const string k_StylesheetPath = "Packages/com.unity.multiplayer.playmode/Scenarios/Editor/Debugging/ScenarioStatusWindow.uss";

        [MenuItem("Window/Analysis/Play Mode Scenarios Status")]
        private static void OpenWindow()
        {
            GetWindow<ScenarioStatusWindow>();
        }

        private static Scenario m_LastScenario;

        private Scenario m_Scenario;
        private ScenarioStatusElement m_ScenarioElement;

        private void OnEnable()
        {
            titleContent = new GUIContent("Play Mode Scenarios Status");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylesheetPath);
            rootVisualElement.styleSheets.Add(styleSheet);

            m_Scenario = m_LastScenario != null ? m_LastScenario : ScenarioRunner.instance.ActiveScenario;
            Scenario.ScenarioStarted += OnScenarioStarted;

            Refresh();
        }

        private void OnDisable()
        {
            Scenario.ScenarioStarted -= OnScenarioStarted;
        }

        private void OnScenarioStarted(Scenario scenario)
        {
            m_Scenario = scenario;
            m_LastScenario = scenario;
            Refresh();
        }

        private void Refresh()
        {
            rootVisualElement.Clear();

            if (m_Scenario == null)
                return;

            m_ScenarioElement = new ScenarioStatusElement(m_Scenario);
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}

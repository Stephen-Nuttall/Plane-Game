using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Scenarios.Debugging.Editor
{
    internal class ScenarioStatusElement : VisualElement
    {
        private Scenario m_Scenario;
        private Dictionary<ScenarioStage, StageStatusElement> m_StageElements = new Dictionary<ScenarioStage, StageStatusElement>();

        private Label m_StateLabel;
        private Label m_MessageLabel;

        public ScenarioStatusElement(Scenario config)
        {
            m_Scenario = config;
            BuildUI();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += Refresh;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= Refresh;
        }

        private void BuildUI()
        {
            AddToClassList("scenario-status");

            if (m_Scenario == null || m_Scenario.Status.StageState == NodeState.Invalid)
                return;

            var nameLabel = new Label($"Scenario: {m_Scenario.Name}") { name = "scenario-name" };
            m_StateLabel = new Label("[]") { name = "scenario-state" };
            m_MessageLabel = new Label("Message:") { name = "scenario-message" };
            var stagesScrollView = new ScrollView(ScrollViewMode.Horizontal) { name = "stages-scroll-view" };
            var stagesContainer = new VisualElement { name = "stages-container" };

            Add(nameLabel);
            Add(m_StateLabel);
            Add(m_MessageLabel);
            Add(stagesScrollView);
            stagesScrollView.Add(stagesContainer);

            var stageKeys = Enum.GetValues(typeof(ScenarioStage));
            foreach (ScenarioStage stage in stageKeys)
            {
                var stageElement = new StageStatusElement(stage, m_Scenario.GetNodes(stage));
                m_StageElements[stage] = stageElement;
                stagesContainer.Add(stageElement);
            }
        }

        private void Refresh()
        {
            if (m_Scenario == null || m_Scenario.Status.StageState == NodeState.Invalid)
                return;

            var state = m_Scenario.Status;
            var message = state.ErrorMessages != null ? string.Join("\n", state.ErrorMessages) : string.Empty;

            m_StateLabel.text = $"[{state.State} - {state.Progress * 100}%]";
            m_MessageLabel.text = $"Message: {message}";

            var stageKeys = Enum.GetValues(typeof(ScenarioStage));
            foreach (ScenarioStage stage in stageKeys)
            {
                var stageElement = m_StageElements[stage];
                var stageState = CalculateStageState(state, stage);
                stageElement.SetState(stageState, state.Progress);
            }
        }

        private static NodeState CalculateStageState(ScenarioStatus scenarioState, ScenarioStage stage)
        {
            if (scenarioState.StageStates == null || scenarioState.StageStates.Length == 0)
                return NodeState.Invalid;

            return scenarioState.StageStates[(int)stage];
        }
    }
}

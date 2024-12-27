using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Scenarios.Debugging.Editor
{
    internal class StageStatusElement : VisualElement
    {
        private ScenarioStage m_Stage;
        private IEnumerable<Node> m_Nodes;

        private Label m_StateLabel;

        public StageStatusElement(ScenarioStage stage, IEnumerable<Node> nodes)
        {
            m_Stage = stage;
            m_Nodes = nodes;
            BuildUI();
        }

        private void BuildUI()
        {
            AddToClassList("stage-status");

            var stageNameLabel = new Label($"{m_Stage}") { name = "stage-name" };
            m_StateLabel = new Label("[]") { name = "stage-state" };
            var nodesContainer = new VisualElement { name = "nodes-container" };

            Add(stageNameLabel);
            Add(m_StateLabel);
            Add(nodesContainer);

            if (m_Nodes == null)
                return;

            foreach (var node in m_Nodes)
            {
                var nodeVisualElement = new NodeStatusElement(node);
                nodesContainer.Add(nodeVisualElement);
            }
        }

        public void SetState(NodeState state, float progress)
        {
            m_StateLabel.text = $"[{state}]";
        }
    }
}

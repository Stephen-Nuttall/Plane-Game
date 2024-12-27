#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Configurations.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Views
{
    /// <summary>
    /// The content of the status window that is shown, when a user clicks
    /// <see cref="MultiplayerPlayModeStatusButton"/>
    /// </summary>
    class PlaymodeStatusPopupContent : PopupWindowContent
    {
        const string k_Stylesheet = "Scenarios/Editor/PlayModeConfig/UI/MultiplayerStatusView/PlaymodeStatusPopupContent.uss";
        const string k_HeadlineName = "headline";
        private const string k_StatusIcon = "status-icon";
        const string k_InstanceListContainer = "status-list";

        const string k_Title = "Instances Status";

        public static readonly Vector2 windowSize = new Vector2(300, 175);
        static Dictionary<InstanceView, Node> m_ViewToNode = new();

        public override Vector2 GetWindowSize()
        {
            return windowSize;
        }

        public override VisualElement CreateGUI()
        {
            var container = new ScrollView() { name = k_InstanceListContainer };
            container.style.minHeight = container.style.maxHeight = windowSize.y;
            container.style.minWidth = container.style.maxWidth = windowSize.x;

            var headline = new Label(k_Title) { name = k_HeadlineName };

            var statusIcon = new Button() { name = k_StatusIcon };

            var headlineContainer = new VisualElement();

            headlineContainer.AddToClassList("headline-container");

            statusIcon.AddToClassList("status-icon");

            statusIcon.tooltip = "Open Play mode Status Window";

            statusIcon.RegisterCallback<ClickEvent>(evt => PlaymodeStatusWindow.OpenWindow());

            headlineContainer.Add(headline);
            headlineContainer.Add(statusIcon);
            container.Add(headlineContainer);
            container.schedule.Execute(() => UpdateInstanceStatus()).Every(1000);

            UIUtils.ApplyStyleSheetAsync(k_Stylesheet, container).Forget();

            m_ViewToNode.Clear();
            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;

            if (currentConfig == null)
                return new VisualElement() { name = "no content" };

            var instances = currentConfig.GetAllInstances();
            foreach (var instance in instances)
            {
                var instanceView = new InstanceView(instance);
                m_ViewToNode.Add(instanceView, GetRunNodeForNodeName(instance.CorrespondingNodeId));
                container.Add(instanceView);
            }

            return container;
        }

        Node GetRunNodeForNodeName(string nodeName)
        {
            if (ScenarioRunner.instance.ActiveScenario == null)
                return null;

            var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ScenarioStage.Run);
            foreach (var node in runNodes)
            {
                if (node.Name == nodeName)
                    return node;
            }

            return null;
        }

        static void UpdateInstanceStatus()
        {
            foreach (var (view, node) in m_ViewToNode)
            {
                view.SetStatus(node?.State ?? NodeState.Idle);
            }
        }

        class InstanceView : VisualElement
        {
            readonly VisualElement m_StatusIndicator;
            const string k_InstanceViewClass = "instance-view";
            const string k_InstanceIconName = "instance-icon";
            const string k_InstanceNameName = "instance-name";
            const string k_InstanceContainerName = "instance-name-container";
            const string k_StatusContainerName = "status-container";
            const string k_StatusIndicatorName = "status-indicator";

            const string k_ActiveClass = "active";
            const string k_ErrorClass = "error";
            const string k_IdleClass = "idle";

            internal InstanceView(InstanceDescription instance)
            {
                AddToClassList(k_InstanceViewClass);
                AddToClassList(k_ActiveClass);

                var instanceIcon = new VisualElement() { name = k_InstanceIconName };
                instanceIcon.AddToClassList("icon");
                var nameLabel = new Label(instance.Name) { name = k_InstanceNameName };

                var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
                instanceContainer.Add(instanceIcon);
                instanceContainer.Add(nameLabel);

                var statusContainer = new VisualElement() { name = k_StatusContainerName };
                m_StatusIndicator = new VisualElement() { name = k_StatusIndicatorName };
                m_StatusIndicator.AddToClassList("icon");

                statusContainer.Add(m_StatusIndicator);

#if UNITY_USE_MULTIPLAYER_ROLES
                var roleLabel = new Label();
                statusContainer.Add(roleLabel);
#endif

                // Todo: this info should come from the configuration and not be branched here.
                if (instance is EditorInstanceDescription editorInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("d_UnityLogo");
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = editorInstanceDescription.RoleMask.ToString();
#endif
                }

                if (instance is LocalInstanceDescription localInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = InternalUtilities.GetBuildProfileTypeIcon(localInstanceDescription.BuildProfile);
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (localInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(localInstanceDescription.BuildProfile).ToString();
#endif
                }

                if (instance is RemoteInstanceDescription remoteInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = InternalUtilities.GetBuildProfileTypeIcon(remoteInstanceDescription.BuildProfile);
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (remoteInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(remoteInstanceDescription.BuildProfile).ToString();
#endif
                    var linkToDashboard = new VisualElement();
                    linkToDashboard.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        var orgId = CloudProjectSettings.organizationKey;
                        var projectId = CloudProjectSettings.projectId;

                        Application.OpenURL($"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/multiplay/overview");
                    });


                    linkToDashboard.AddToClassList("dashboard-link");
                    linkToDashboard.AddToClassList("icon");
                    statusContainer.Add(linkToDashboard);
                }

                Add(instanceContainer);
                Add(statusContainer);
            }

            internal void SetStatus(NodeState status)
            {
                RemoveFromClassList(k_ActiveClass);
                RemoveFromClassList(k_ErrorClass);
                RemoveFromClassList(k_IdleClass);
                switch (status)
                {
                    case NodeState.Active:
                        AddToClassList(k_ActiveClass);
                        m_StatusIndicator.tooltip = "active";
                        break;
                    case NodeState.Error:
                        AddToClassList(k_ErrorClass);
                        m_StatusIndicator.tooltip = "error";
                        break;
                    default:
                        AddToClassList(k_IdleClass);
                        m_StatusIndicator.tooltip = "idle";
                        break;
                }
            }
        }

    }
}

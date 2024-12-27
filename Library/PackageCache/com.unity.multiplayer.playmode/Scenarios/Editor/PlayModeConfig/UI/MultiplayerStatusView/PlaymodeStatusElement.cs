#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Configurations.Editor.Gui;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor.Experimental.GraphView;
using Node = Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation.Node;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Views
{
    class PlaymodeStatusElement : VisualElement
    {
        const string k_Stylesheet =
            "Scenarios/Editor/PlayModeConfig/UI/MultiplayerStatusView/PlaymodeStatusWindow.uss";

        const string k_HeadlineName = "headline";
        const string k_InstanceListContainer = "status-list";
        const string k_EditorContainerName = "editor-instance-list";
        static List<InstanceView> m_InstanceViewList = new();

        public PlaymodeStatusElement()
        {
            CreateGUI();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ScenarioRunner.StatusChanged += UpdateInstanceStatus;
            Scenario currentScenario = ScenarioRunner.instance.ActiveScenario;
            if (currentScenario != null)
            {
                ScenarioStatus currentStatus = currentScenario.Status;
                UpdateInstanceStatus(currentStatus);
            }
            else
            {
                UpdateInstanceStatus(ScenarioStatus.Invalid);
            }
        }


        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ScenarioRunner.StatusChanged -= UpdateInstanceStatus;
        }

        public void CreateGUI()
        {
            var container = new ScrollView() { name = k_InstanceListContainer };
            var editorContainer = new VisualElement() { name = k_EditorContainerName };
            var editorTitle = new Label("Editor") { name = k_HeadlineName };
            editorContainer.Add(editorTitle);
            var localContainer = new VisualElement();
            var localTitle = new Label("Local Instances") { name = k_HeadlineName };
            localContainer.Add(localTitle);
            var remoteContainer = new VisualElement();
            var remoteTitle = new Label("Remote Instances") { name = k_HeadlineName };
            remoteContainer.Add(remoteTitle);


            var configButton = new Button(); // Add a button to open the config window
            configButton.AddToClassList("config-button");
            configButton.text = "Edit Configuration";
            configButton.RegisterCallback<ClickEvent>(evt => PlayModeScenariosWindow.ShowWindow());

            container.Add(configButton);

            UIUtils.ApplyStyleSheetAsync(k_Stylesheet, container).Forget();
            m_InstanceViewList.Clear();

            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;

            if (currentConfig == null)
                return;

            var instances = currentConfig.GetAllInstances();
            foreach (var instance in instances)
            {
                var instanceView = new InstanceView(instance);
                m_InstanceViewList.Add(instanceView);

                if (instance is EditorInstanceDescription)
                {
                    editorContainer.Add(instanceView);
                }
                else if (instance is LocalInstanceDescription)
                {
                    localContainer.Add(instanceView);
                }
                else if (instance is RemoteInstanceDescription)
                {
                    remoteContainer.Add(instanceView);
                }
            }

            if (editorContainer.childCount <= 1)
            {
                editorContainer.style.display = DisplayStyle.None;
            }

            if (remoteContainer.childCount <= 1)
            {
                remoteTitle.style.display = DisplayStyle.None;
            }

            if (localContainer.childCount <= 1)
            {
                localTitle.style.display = DisplayStyle.None;
            }

            container.Add(editorContainer);
            container.Add(localContainer);
            container.Add(remoteContainer);


            Add(container);
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

        static void UpdateInstanceStatus(ScenarioStatus scenarioStatus)
        {
            foreach (var view in m_InstanceViewList)
            {
                view.SetStatus(scenarioStatus);
            }
        }

        internal class InstanceView : VisualElement
        {
            private InstanceDescription m_Instance;
            readonly VisualElement m_StatusIndicator;
            internal Label LogInfoText;
            internal Label LogWarningText;
            internal Label LogErrorText;
            internal TextField IpAddress;
            internal TextField Port;
            internal Button IpCopyButton;
            internal Button PortCopyButton;
            internal UnityPlayer Player;
            private Label m_ConnectedLabel;
            private Label m_StatusLabel;
            internal const string k_InstanceViewClass = "instance-view";
            internal const string k_InstanceIconName = "instance-icon";
            internal const string k_InstanceNameName = "instance-name";
            internal const string k_InstanceContentName = "instance-content";
            internal const string k_InstanceContainerName = "instance-container";
            internal const string k_StatusContainerName = "status-container";
            internal const string k_StatusIndicatorName = "status-indicator";
            internal const string k_LogInfoIcon = "LogInfoIcon";
            internal const string k_LogWarningIcon = "LogWarningIcon";
            internal const string k_LogErrorIcon = "LogErrorIcon";

            internal const string k_ActiveClass = "active";
            internal const string k_ErrorClass = "error";
            internal const string k_IdleClass = "idle";
            internal const string k_LoadingClass = "loading";





            internal InstanceView(InstanceDescription instance)
            {
                m_Instance = instance;
                AddToClassList(k_InstanceViewClass);
                AddToClassList(k_IdleClass);

                var instanceIcon = new VisualElement() { name = k_InstanceIconName };
                instanceIcon.AddToClassList("icon");
                var nameLabel = new Label(instance.Name) { name = k_InstanceNameName };

                var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
                var instanceNameContainer = new VisualElement();
                instanceNameContainer.AddToClassList("instance-name-container");
                instanceNameContainer.style.flexDirection = FlexDirection.Row;
                instanceNameContainer.Add(instanceIcon);
                instanceNameContainer.Add(nameLabel);
                var instanceContent = new VisualElement() { name = k_InstanceContentName };
                instanceContent.AddToClassList("instance-content");
                instanceContent.style.flexDirection = FlexDirection.Row;
                var buttonContainer = new VisualElement();
                buttonContainer.style.flexDirection = FlexDirection.Row;

                var statusContainer = new VisualElement() { name = k_StatusContainerName };
                m_StatusLabel = new Label();
                m_StatusIndicator = new VisualElement() { name = k_StatusIndicatorName };
                m_ConnectedLabel = new Label();
                var logInfoIcon = new VisualElement() { name = k_LogInfoIcon };
                logInfoIcon.AddToClassList("icon");
                LogInfoText = new Label();
                LogWarningText = new Label();
                LogErrorText = new Label();
                var logWarningIcon = new VisualElement() { name = k_LogWarningIcon };
                logWarningIcon.AddToClassList("icon");
                var logErrorIcon = new VisualElement() { name = k_LogErrorIcon };
                logErrorIcon.AddToClassList("icon");
                m_StatusIndicator.AddToClassList("icon");

                var statusContentContainer = new VisualElement();
                var statusLabelContainer = new VisualElement();
                statusLabelContainer.AddToClassList("status-label-container");
                statusContentContainer.AddToClassList("status-content-container");
                statusContentContainer.style.flexDirection = FlexDirection.Row;
                statusLabelContainer.style.flexDirection = FlexDirection.Row;
                statusContainer.style.flexDirection = FlexDirection.Column;
                statusLabelContainer.Add(m_StatusIndicator);
                statusLabelContainer.Add(m_StatusLabel);
                statusLabelContainer.Add(m_ConnectedLabel);
                statusContentContainer.Add(logInfoIcon);
                statusContentContainer.Add(LogInfoText);
                statusContentContainer.Add(logWarningIcon);
                statusContentContainer.Add(LogWarningText);
                statusContentContainer.Add(logErrorIcon);
                statusContentContainer.Add(LogErrorText);

#if UNITY_USE_MULTIPLAYER_ROLES
                var roleLabel = new Label();
                instanceContent.Add(roleLabel);
#endif

                // Todo: this info should come from the configuration and not be branched here.
                if (instance is EditorInstanceDescription editorInstanceDescription)
                {
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("d_UnityLogo");
                    Player = MultiplayerPlaymode.Players[editorInstanceDescription.PlayerInstanceIndex];


                    if (editorInstanceDescription.PlayerTag != "")
                    {
                        var pill = new Pill();
                        pill.text = editorInstanceDescription.PlayerTag;
                        pill.AddToClassList("player-tag-pill");
                        instanceContent.Add(pill);
                    }

                    if (editorInstanceDescription.Name.Contains("Main"))
                    {
                        logInfoIcon.style.display = DisplayStyle.None;
                        logWarningIcon.style.display = DisplayStyle.None;
                        logErrorIcon.style.display = DisplayStyle.None;
                        LogInfoText.style.display = DisplayStyle.None;
                        LogWarningText.style.display = DisplayStyle.None;
                        LogErrorText.style.display = DisplayStyle.None;
                    }

                    if (editorInstanceDescription.Name.Contains("Player"))
                    {
                        var pinButton = new Button();
                        pinButton.AddToClassList("pin-button");
                        pinButton.text = "Focus Player";
                        pinButton.RegisterCallback<ClickEvent>(evt =>
                            MultiplayerPlaymodeEditorUtility.FocusPlayerView(
                                (PlayerIndex)editorInstanceDescription.PlayerInstanceIndex + 1));
                        buttonContainer.Add(pinButton);
                    }
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = editorInstanceDescription.RoleMask.ToString();

#endif
                }

                if (instance is LocalInstanceDescription localInstanceDescription)
                {
                    instanceIcon.style.backgroundImage =
                        InternalUtilities.GetBuildProfileTypeIcon(localInstanceDescription.BuildProfile);
                    logInfoIcon.style.display = DisplayStyle.None;
                    logWarningIcon.style.display = DisplayStyle.None;
                    logErrorIcon.style.display = DisplayStyle.None;
                    LogInfoText.style.display = DisplayStyle.None;
                    LogWarningText.style.display = DisplayStyle.None;
                    LogErrorText.style.display = DisplayStyle.None;
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (localInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager
                            .GetMultiplayerRoleForBuildProfile(localInstanceDescription.BuildProfile).ToString();
#endif
                }

                if (instance is RemoteInstanceDescription remoteInstanceDescription)
                {
                    IpAddress = new TextField();
                    Port = new TextField();
                    IpAddress.isReadOnly = true;
                    Port.isReadOnly = true;
                    IpAddress.style.display = DisplayStyle.None;
                    Port.style.display = DisplayStyle.None;
                    IpCopyButton = new Button();
                    PortCopyButton = new Button();
                    IpCopyButton.tooltip = "Copy IP Address To ClipBoard";
                    PortCopyButton.tooltip = "Copy Port To ClipBoard";
                    IpCopyButton.RegisterCallback<ClickEvent>(evt => CopyTextToClipboard(IpAddress.value));
                    PortCopyButton.RegisterCallback<ClickEvent>(evt => CopyTextToClipboard(Port.value));
                    IpCopyButton.iconImage = EditorGUIUtility.FindTexture("Clipboard");
                    PortCopyButton.iconImage = EditorGUIUtility.FindTexture("Clipboard");
                    IpCopyButton.style.display = DisplayStyle.None;
                    PortCopyButton.style.display = DisplayStyle.None;
                    instanceIcon.style.backgroundImage =
                        InternalUtilities.GetBuildProfileTypeIcon(remoteInstanceDescription.BuildProfile);
                    logInfoIcon.style.display = DisplayStyle.None;
                    logWarningIcon.style.display = DisplayStyle.None;
                    logErrorIcon.style.display = DisplayStyle.None;
                    LogInfoText.style.display = DisplayStyle.None;
                    LogWarningText.style.display = DisplayStyle.None;
                    LogErrorText.style.display = DisplayStyle.None;
#if UNITY_USE_MULTIPLAYER_ROLES
                    roleLabel.text = "no role";
                    if (remoteInstanceDescription.BuildProfile != null)
                        roleLabel.text = EditorMultiplayerRolesManager
                            .GetMultiplayerRoleForBuildProfile(remoteInstanceDescription.BuildProfile).ToString();
#endif
                    var linkToDashboard = new VisualElement();
                    linkToDashboard.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        var orgId = CloudProjectSettings.organizationKey;
                        var projectId = CloudProjectSettings.projectId;

                        Application.OpenURL(
                            $"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/multiplay/overview");
                    });


                    linkToDashboard.AddToClassList("dashboard-link");
                    linkToDashboard.AddToClassList("icon");
                    instanceContent.Add(linkToDashboard);
                    statusContentContainer.Add(IpAddress);
                    statusContentContainer.Add(IpCopyButton);
                    statusContentContainer.Add(Port);
                    statusContentContainer.Add(PortCopyButton);
                }

                statusContainer.Add(statusLabelContainer);
                statusContainer.Add(statusContentContainer);
                instanceContainer.Add(instanceNameContainer);
                instanceContainer.Add(instanceContent);
                instanceContainer.Add(buttonContainer);
                Add(instanceContainer);
                Add(statusContainer);
            }

            internal void CopyTextToClipboard(string text)
            {
                // Copy the text field's value to clipboard
                GUIUtility.systemCopyBuffer = text;
            }

            private void CleanUpStatus()
            {
                RemoveFromClassList(k_ActiveClass);
                RemoveFromClassList(k_ErrorClass);
                RemoveFromClassList(k_IdleClass);

                m_StatusIndicator.tooltip = string.Empty;
                m_ConnectedLabel.text = string.Empty;
                m_StatusLabel.text = string.Empty;

                m_StatusLabel.style.display = DisplayStyle.None;

                if (m_Instance is RemoteInstanceDescription)
                {
                    Port.style.display = DisplayStyle.None;
                    IpAddress.style.display = DisplayStyle.None;
                    PortCopyButton.style.display = DisplayStyle.None;
                    IpCopyButton.style.display = DisplayStyle.None;
                }
            }

            private ScenarioStage ComputeInstanceStage(ScenarioStatus scenarioStatus)
            {
                return scenarioStatus.CurrentStage;
            }

            private List<NodeStatus> GetNodesStatusForInstance(ScenarioStatus scenarioStatus)
            {
                var nodesIds = m_Instance.GetCorrespondingNodes();
                var nodesStatus = new List<NodeStatus>();

                if (scenarioStatus.NodeStateReports != null)
                {
                    foreach (var nodeStatus in scenarioStatus.NodeStateReports)
                    {
                        if (nodesIds.Contains(nodeStatus.NodeName))
                            nodesStatus.Add(nodeStatus);
                    }
                }

                return nodesStatus;
            }

            private NodeState ComputeInstanceState(List<NodeStatus> nodesStatus, ScenarioStatus scenarioStatus)
            {
                var totalNodes = nodesStatus.Count;
                var errorNodes = 0;
                var idleNodes = 0;
                var runningNodes = 0;
                var activeNodes = 0;
                var completeNodes = 0;
                var abortedNodes = 0;

                foreach (var nodeStatus in nodesStatus)
                {
                    switch (nodeStatus.State)
                    {
                        case NodeState.Error:
                            errorNodes++;
                            break;
                        case NodeState.Idle:
                            idleNodes++;
                            break;
                        case NodeState.Running:
                            runningNodes++;
                            break;
                        case NodeState.Active:
                            activeNodes++;
                            break;
                        case NodeState.Completed:
                            completeNodes++;
                            break;
                        case NodeState.Aborted:
                            abortedNodes++;
                            break;
                        default:
                            UnityEngine.Debug.LogError($"Invalid node state {nodeStatus.State}");
                            return NodeState.Invalid;
                    }
                }

                if (errorNodes > 0)
                    return NodeState.Error;

                if (idleNodes == totalNodes)
                    return NodeState.Idle;

                if (abortedNodes > 0)
                    return NodeState.Aborted;

                if (completeNodes == totalNodes)
                    return NodeState.Completed;

                if (activeNodes > 0 && activeNodes + completeNodes == totalNodes)
                    return NodeState.Active;

                return NodeState.Running;
            }

            private void AssignStatusClass(NodeState state, ScenarioStage stage)
            {
                if (stage == ScenarioStage.Run)
                {
                    switch (state)
                    {
                        case NodeState.Completed:
                            AddToClassList(k_IdleClass);
                            return;
                        case NodeState.Active:
                            AddToClassList(k_ActiveClass);
                            return;
                    }
                }

                switch (state)
                {
                    case NodeState.Idle:
                        AddToClassList(k_IdleClass);
                        break;
                    case NodeState.Completed:
                    case NodeState.Running:
                    case NodeState.Active:
                        AddToClassList(k_LoadingClass);
                        break;
                    case NodeState.Error:
                    case NodeState.Aborted:
                    case NodeState.Invalid:
                    default:
                        AddToClassList(k_ErrorClass);
                        break;
                }
            }

            private void AssignStatusTooltip(NodeState state)
            {
                switch (state)
                {
                    case NodeState.Active:
                        m_StatusIndicator.tooltip = "active";
                        break;
                    case NodeState.Error:
                        m_StatusIndicator.tooltip = "error";
                        break;
                    case NodeState.Completed:
                        m_StatusIndicator.tooltip = "completed";
                        break;
                    case NodeState.Running:
                        m_StatusIndicator.tooltip = "running";
                        break;
                    case NodeState.Idle:
                        m_StatusIndicator.tooltip = "idle";
                        break;
                    case NodeState.Invalid:
                        m_StatusIndicator.tooltip = "invalid";
                        break;
                    case NodeState.Aborted:
                        m_StatusIndicator.tooltip = "aborted";
                        break;
                    default:
                        m_StatusIndicator.tooltip = "idle";
                        break;
                }
            }

            private void AssignLogs(NodeState state)
            {
                if (m_Instance is not EditorInstanceDescription) return; // only Editor Instance have Logs
                switch (state)
                {
                    case NodeState.Active:
                        if (Player != null)
                        {
                            var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(Player.PlayerIdentifier).LogCounts;
                            LogInfoText.text = logs.Logs.ToString();
                            LogWarningText.text = logs.Warnings.ToString();
                            LogErrorText.text = logs.Errors.ToString();
                        }

                        break;
                    default:
                        LogInfoText.text = 0.ToString();
                        LogWarningText.text = 0.ToString();
                        LogErrorText.text = 0.ToString();
                        break;
                }
            }

            Node GetConnectableNode(string nodeName)
            {
                if (ScenarioRunner.instance.ActiveScenario == null)
                    return null;

                var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ScenarioStage.Run);
                foreach (var node in runNodes)
                {
                    if (node.Name == nodeName && node is IConnectableNode)
                        return node;
                }

                return null;
            }

            private void AssignIpAddress(NodeState state)
            {
                if (m_Instance is not RemoteInstanceDescription) return; // only Remote Instance have Ip info
                var nodes = m_Instance.GetCorrespondingNodes();

                var connectableRemoteNode = nodes.Select(GetConnectableNode).FirstOrDefault(item => item != null);
                switch (state)
                {
                    case NodeState.Active:
                        if (IpAddress != null)
                        {
                            if (connectableRemoteNode is IConnectableNode connectableNode)
                            {
                                var ipAddress = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()
                                    ?.IpAddress;
                                var port = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()?.Port;

                                if (ipAddress != null)
                                    IpAddress.value = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()
                                        .IpAddress;
                                Port.value = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()?.Port
                                    .ToString();
                                IpAddress.style.display = DisplayStyle.Flex;
                                IpCopyButton.style.display = DisplayStyle.Flex;
                                Port.style.display = DisplayStyle.Flex;
                                PortCopyButton.style.display = DisplayStyle.Flex;
                            }
                        }

                        break;
                }
            }

            private void AssignStatusLabel(List<NodeStatus> status, ScenarioStage scenarioStage)
            {
                List<NodeStatus> inScopeNodes = GetCurrentNodes(status, m_Instance, scenarioStage);
                m_StatusLabel.text = GetNodesStageDisplayStr(inScopeNodes, scenarioStage);
                m_StatusLabel.style.display = DisplayStyle.Flex;
            }

            private string GetNodesStageDisplayStr(List<NodeStatus> nodesStatusList, ScenarioStage scenarioStage)
            {
                double progress = 1.0;
                if (nodesStatusList.Count > 0)
                {
                    progress = CalculateNodeProgress(
                        nodesStatusList); // extract to function, if length > 1, take average of the node progress
                }

                string stageStr = scenarioStage switch
                {
                    ScenarioStage.Prepare => $"Preparing {Math.Round(progress * 100, 0)}%",
                    ScenarioStage.Deploy => $"Deploying {Math.Round(progress * 100, 0)}%",
                    ScenarioStage.Run => "Running",
                    _ => "Idle"
                };
                if (scenarioStage == ScenarioStage.Run &&
                    nodesStatusList.Any(nodeStatus => nodeStatus.State is NodeState.Running or NodeState.Idle))
                {
                    stageStr = $"Launching {Math.Round(progress * 100, 0)}%";
                }

                return stageStr;
            }

            private double CalculateNodeProgress(List<NodeStatus> nodeStatusList)
            {
                if (nodeStatusList.Count == 0)
                {
                    return 1.0;
                }

                return nodeStatusList.Average(status => status.Progress);
            }

            private List<NodeStatus> GetCurrentNodes(List<NodeStatus> nodeStatusList, InstanceDescription instance,
                ScenarioStage scenarioStage)
            {
                return nodeStatusList.Where(nodeStatus =>
                {
                    switch (instance)
                    {
                        case EditorInstanceDescription:
                            return scenarioStage switch
                            {
                                ScenarioStage.Prepare => false,
                                ScenarioStage.Deploy => nodeStatus.NodeName.Contains("deploy",
                                    StringComparison.OrdinalIgnoreCase),
                                ScenarioStage.Run =>
                                    nodeStatus.NodeName.Contains("run", StringComparison.OrdinalIgnoreCase),
                                _ => false
                            };
                        case LocalInstanceDescription:
                            return scenarioStage switch
                            {
                                ScenarioStage.Prepare => nodeStatus.NodeName.Contains("- build",
                                    StringComparison.OrdinalIgnoreCase),
                                ScenarioStage.Deploy => false,
                                ScenarioStage.Run =>
                                    nodeStatus.NodeName.Contains("- run", StringComparison.OrdinalIgnoreCase),
                                _ => false
                            };
                        case RemoteInstanceDescription:
                            return scenarioStage switch
                            {
                                ScenarioStage.Prepare => nodeStatus.NodeName.Contains(
                                    ScenarioFactory.RemoteNodeConstants.k_BuildNodePostFix,
                                    StringComparison.OrdinalIgnoreCase),
                                ScenarioStage.Deploy => nodeStatus.NodeName.Contains(
                                                            ScenarioFactory.RemoteNodeConstants
                                                                .k_DeployBuildNodePostfix,
                                                            StringComparison.OrdinalIgnoreCase)
                                                        || nodeStatus.NodeName.Contains(
                                                            ScenarioFactory.RemoteNodeConstants
                                                                .k_DeployConfigBuildNodePostfix,
                                                            StringComparison.OrdinalIgnoreCase)
                                                        || nodeStatus.NodeName.Contains(
                                                            ScenarioFactory.RemoteNodeConstants
                                                                .k_DeployFleetNodePostfix,
                                                            StringComparison.OrdinalIgnoreCase),
                                ScenarioStage.Run => nodeStatus.NodeName.Contains(
                                                         ScenarioFactory.RemoteNodeConstants.k_RunNodePostfix,
                                                         StringComparison.OrdinalIgnoreCase)
                                                     || nodeStatus.NodeName.Contains(
                                                         ScenarioFactory.RemoteNodeConstants.k_AllocateNodePostfix,
                                                         StringComparison.OrdinalIgnoreCase),
                                _ => false
                            };
                        default:
                            return false;
                    }
                }).ToList();
            }

            internal void SetStatus(ScenarioStatus scenarioStatus)
            {
                CleanUpStatus();
                var nodesStatus = GetNodesStatusForInstance(scenarioStatus);
                var instanceState = ComputeInstanceState(nodesStatus, scenarioStatus);
                AssignStatusClass(instanceState, scenarioStatus.CurrentStage);
                AssignStatusTooltip(instanceState);
                AssignStatusLabel(nodesStatus, scenarioStatus.CurrentStage);
                AssignLogs(instanceState);
                AssignIpAddress(instanceState);
            }

            internal string GetCurrentScenarioStageString(ScenarioStage stage)
            {
                var stageString = "";
                switch (stage)
                {
                    case ScenarioStage.Prepare:
                        stageString = "Preparing";
                        break;
                    case ScenarioStage.Deploy:
                        stageString = "Deploying";
                        break;
                    case ScenarioStage.Run:
                        stageString = "Launching";
                        break;
                }
                return $"{stageString}";
            }
        }
    }
}

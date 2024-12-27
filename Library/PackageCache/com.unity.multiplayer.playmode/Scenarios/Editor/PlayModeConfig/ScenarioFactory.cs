#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor;
using UnityEngine.Assertions;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using UnityEditor.Build.Profile;
using System.Text.RegularExpressions;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local;
using UnityEditor;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
#if MULTIPLAY_API_AVAILABLE
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay;
#endif

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Creates a scenario graph from a list of instance descriptions.
    /// </summary>
    internal static class ScenarioFactory
    {
#if UNITY_USE_MULTIPLAYER_ROLES
        public static MultiplayerRoleFlags GetRoleForInstance(InstanceDescription instance)
        {
            Assert.IsNotNull(instance, $"Null instance used");
            switch (instance)
            {
                case EditorInstanceDescription editorInstance: return editorInstance.RoleMask;
                case IBuildableInstanceDescription buildableInstance: return buildableInstance.BuildProfile == null ? MultiplayerRoleFlags.Client : EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(buildableInstance.BuildProfile);
            }
            return MultiplayerRoleFlags.Client;
        }
#endif
        internal static class RemoteNodeConstants
        {
            internal const string k_BuildNodePostFix = "- Build";
            internal const string k_DeployBuildNodePostfix = "- Deploy Build";
            internal const string k_DeployConfigBuildNodePostfix = "- Deploy Build Configuration";
            internal const string k_DeployFleetNodePostfix = "- Deploy Fleet";
            internal const string k_RunNodePostfix = "- Run";
            internal const string k_AllocateNodePostfix = "- Allocate";
        }

        private static void CategorizeInstances(
            List<InstanceDescription> instances,
            out List<InstanceDescription> servers,
            out List<InstanceDescription> clients)
        {
            servers = new List<InstanceDescription>();
            clients = new List<InstanceDescription>();

            foreach (var instance in instances)
            {
#if UNITY_USE_MULTIPLAYER_ROLES
                if (GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                    servers.Add(instance);
                else
#endif
                {
                    clients.Add(instance);
                }
            }
        }

        public static Scenario CreateScenario(string name, List<InstanceDescription> instances)
        {
            var scenario = new Scenario(name);

            CategorizeInstances(instances, out var servers, out var clients);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(servers.Count() <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            var serverInstance = servers.FirstOrDefault();
            var serverRunNode = serverInstance != null ? InsertInstance(scenario, serverInstance) : null;

            // This will Ensure the server will pass its connection Data (IP + Port, potentially transport type in the future (UDP / WSS) , secure/ Not Secure) to all the clients.
            foreach (var clientInstance in clients)
            {
                var clientRunNode = InsertInstance(scenario, clientInstance);

                if (serverRunNode != null && serverRunNode != default)
                    scenario.Connect(serverRunNode.ConnectionDataOut, clientRunNode.ConnectionDataIn);
                else
                    scenario.ConnectConstant(clientRunNode.ConnectionDataIn, new ConnectionData());
            }

            return scenario;
        }

        private static IConnectableNode InsertInstance(Scenario scenario, InstanceDescription instance)
        {
            if (instance is EditorInstanceDescription editorInstance)
            {
                return InsertEditorInstance(scenario, editorInstance);
            }
            if (instance is LocalInstanceDescription localInstance)
            {
                return InsertLocalInstance(scenario, localInstance);
            }
            if (instance is RemoteInstanceDescription remoteInstance)
            {
#if MULTIPLAY_API_AVAILABLE
                return InsertRemoteInstance( scenario,  remoteInstance );
#else
                throw new System.Exception("The Multiplay API is not available. It is not possible to create a InsertRemoteInstance instance without it.");
#endif
            }
            throw new System.NotImplementedException();
        }

        static IConnectableNode InsertEditorInstance(Scenario scenario, EditorInstanceDescription editorInstanceDescription)
        {
            var runNode = new EditorMultiplayerPlaymodeRunNode($"{editorInstanceDescription.Name}|{editorInstanceDescription.PlayerInstanceIndex}_run");
            var deployNode = new EditorMultiplayerPlaymodeDeployNode($"{editorInstanceDescription.Name}|{editorInstanceDescription.PlayerInstanceIndex}_deploy");

            scenario.AddNode(deployNode, ScenarioStage.Deploy);
            scenario.ConnectConstant(deployNode.PlayerInstanceIndex, editorInstanceDescription.PlayerInstanceIndex);
            scenario.ConnectConstant(deployNode.PlayerTags, editorInstanceDescription.PlayerTag);
#if UNITY_USE_MULTIPLAYER_ROLES
            scenario.ConnectConstant(deployNode.MultiplayerRole, editorInstanceDescription.RoleMask);
#endif
            scenario.ConnectConstant(deployNode.InitialScene, editorInstanceDescription.InitialScene);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            editorInstanceDescription.CorrespondingNodeId = runNode.Name;

            editorInstanceDescription.SetCorrespondingNodes(runNode, deployNode);

            scenario.AddNode(runNode, ScenarioStage.Run);
            scenario.ConnectConstant(runNode.PlayerInstanceIndex, editorInstanceDescription.PlayerInstanceIndex);
            scenario.ConnectConstant(runNode.PlayerTags, editorInstanceDescription.PlayerTag);

            if (editorInstanceDescription is VirtualEditorInstanceDescription virtualEditorInstanceDescription)
            {
                scenario.ConnectConstant(runNode.StreamLogs, virtualEditorInstanceDescription.AdvancedConfiguration.StreamLogsToMainEditor);
                scenario.ConnectConstant(runNode.LogsColor, virtualEditorInstanceDescription.AdvancedConfiguration.LogsColor);
            }

            return runNode;
        }

        private static IConnectableNode InsertLocalInstance(Scenario scenario, LocalInstanceDescription instance)
        {
            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            var buildNode = new EditorBuildNode($"{instance.Name} - Build");
            scenario.AddNode(buildNode, ScenarioStage.Prepare);

            scenario.ConnectConstant(buildNode.BuildPath, GenerateBuildPath(instance.BuildProfile));
            scenario.ConnectConstant(buildNode.Profile, instance.BuildProfile);

            var runNode = new LocalRunNode($"{instance.Name} - Run");
            scenario.AddNode(runNode, ScenarioStage.Run);

            // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
            // arguments cause a crash. As a temporary workaround we detect that case and remove any
            // of those arguments that, in any case, take no effect on that platform.
            var arguments = instance.AdvancedConfiguration.Arguments;
            if (InternalUtilities.IsServerProfile(instance.BuildProfile))
                arguments = CleanupScreenArguments(arguments);

            scenario.ConnectConstant(runNode.Arguments, arguments);
            scenario.ConnectConstant(runNode.StreamLogs, instance.AdvancedConfiguration.StreamLogsToMainEditor);
            scenario.ConnectConstant(runNode.LogsColor, instance.AdvancedConfiguration.LogsColor);
            scenario.Connect(buildNode.ExecutablePath, runNode.ExecutablePath);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            instance.CorrespondingNodeId = runNode.Name;

            instance.SetCorrespondingNodes(buildNode, runNode);

            return runNode;
        }

        private static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }



        private static IConnectableNode InsertRemoteInstance(Scenario scenario, RemoteInstanceDescription instance)
        {

#if !MULTIPLAY_API_AVAILABLE
            throw new System.Exception("The Multiplay API is not available. It is not possible to create the corresponding Deploy and Run nodes without it.");
#else

#if UNITY_USE_MULTIPLAYER_ROLES
            var role = GetRoleForInstance(instance);

            // We assume the remote instance is a server.
            Assert.AreEqual(role, MultiplayerRoleFlags.Server);
#endif
            var buildPath = GenerateBuildPath(instance.BuildProfile);


            var buildNode = new EditorBuildNode($"{instance.Name} {RemoteNodeConstants.k_BuildNodePostFix}");
            scenario.AddNode(buildNode, ScenarioStage.Prepare);
            scenario.ConnectConstant(buildNode.BuildPath, buildPath);
            scenario.ConnectConstant(buildNode.Profile, instance.BuildProfile);


            var advancedConfiguration = instance.AdvancedConfiguration;
            var multiplayName = RemoteInstanceDescription.ComputeMultiplayName(advancedConfiguration.Identifier);
            var buildName = multiplayName;
            var buildConfigurationName = multiplayName;
            var fleetName = multiplayName;

            var deployBuildNode = new DeployBuildNode($"{instance.Name} {RemoteNodeConstants.k_DeployBuildNodePostfix}");
            scenario.AddNode(deployBuildNode, ScenarioStage.Deploy);
            scenario.ConnectConstant(deployBuildNode.BuildName, buildName);
            scenario.Connect(buildNode.OutputPath, deployBuildNode.BuildPath);
            scenario.Connect(buildNode.ExecutablePath, deployBuildNode.ExecutablePath);
            scenario.Connect(buildNode.BuildHash, deployBuildNode.BuildHash);

            var deployBuildConfigNode = new DeployBuildConfigurationNode($"{instance.Name} {RemoteNodeConstants.k_DeployConfigBuildNodePostfix}");
            scenario.AddNode(deployBuildConfigNode, ScenarioStage.Deploy);
            scenario.ConnectConstant(deployBuildConfigNode.BuildConfigurationName, buildConfigurationName);
            scenario.ConnectConstant(deployBuildConfigNode.BuildName, buildName);
            scenario.ConnectConstant(deployBuildConfigNode.Settings, instance.GetBuildConfigurationSettings());
            scenario.Connect(deployBuildNode.BuildId, deployBuildConfigNode.BuildId);
            scenario.Connect(buildNode.RelativeExecutablePath, deployBuildConfigNode.BinaryPath);

            var deployFleetNode = new DeployFleetNode($"{instance.Name} {RemoteNodeConstants.k_DeployFleetNodePostfix}");
            scenario.AddNode(deployFleetNode, ScenarioStage.Deploy);
            scenario.ConnectConstant(deployFleetNode.FleetName, fleetName);
            scenario.ConnectConstant(deployFleetNode.Region, advancedConfiguration.FleetRegion);
            scenario.ConnectConstant(deployFleetNode.BuildConfigurationName, buildConfigurationName);
            scenario.Connect(deployBuildConfigNode.BuildConfigurationId, deployFleetNode.BuildConfigurationId);


            var allocateNode = new AllocateNode($"{instance.Name} {RemoteNodeConstants.k_AllocateNodePostfix}");
            scenario.AddNode(allocateNode, ScenarioStage.Run);
            scenario.ConnectConstant(allocateNode.FleetName, fleetName);
            scenario.ConnectConstant(allocateNode.BuildConfigurationName, buildConfigurationName);

            var runNode = new RunServerNode($"{instance.Name} {RemoteNodeConstants.k_RunNodePostfix}");
            scenario.AddNode(runNode, ScenarioStage.Run);
            scenario.ConnectConstant(runNode.StreamLogs, instance.AdvancedConfiguration.StreamLogsToMainEditor);
            scenario.ConnectConstant(runNode.LogsColor, instance.AdvancedConfiguration.LogsColor);
            scenario.Connect(allocateNode.ServerId, runNode.ServerId);
            scenario.Connect(allocateNode.ConnectionDataOut, runNode.ConnectionData);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            instance.CorrespondingNodeId = runNode.Name;

            instance.SetCorrespondingNodes(buildNode,deployBuildNode, deployBuildConfigNode, deployFleetNode, allocateNode,runNode);

            return runNode;
#endif
        }

        private static string GenerateBuildPath(BuildProfile profile)
        {
            // It is important that all builds are in its own folder because when we upload the build to the Multiplay service,
            // we upload the whole folder. If we have multiple builds in the same folder, we will upload all of them.
            var escapedProfileName = EscapeProfileName(profile.name);
            return $"Builds/PlayModeScenarios/{escapedProfileName}/{escapedProfileName}";
        }

        private static string EscapeProfileName(string path) => Regex.Replace(path, @"[^\w\d]", "_");
    }
}

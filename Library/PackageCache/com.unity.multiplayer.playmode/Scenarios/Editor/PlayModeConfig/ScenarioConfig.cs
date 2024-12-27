using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Multiplayer.PlayMode.Configurations.Editor.Gui;
using UnityEditor.SceneManagement;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [CreatePlayModeConfigurationMenu("Scenario Configuration", "NewPlayModeScenario")]
    class ScenarioConfig : PlayModeConfig, ISerializationCallbackReceiver
    {
        public static readonly ReadOnlyCollection<string> k_RequiredPackagesForRemoteInstances = new List<string>()
        {
            "com.unity.services.multiplayer@1.0.0",
        }.AsReadOnly();

        [SerializeField] private bool m_EnableEditors = true;
        [SerializeField] private MainEditorInstanceDescription m_MainEditorInstance = new();

        [Tooltip("Initial Editor Instances when entering playmode. Editor Instances will only have limited authoring capabilities.")]
        [SerializeField] private List<VirtualEditorInstanceDescription> m_EditorInstances = new();

        [Tooltip("Local Instances are builds that will run on the same machine as the editor.")]
        [SerializeField] private List<LocalInstanceDescription> m_LocalInstances = new();

        [Tooltip("Remote Instances are builds that will get deployed to UGS and will run there.")]
        [SerializeField] private List<RemoteInstanceDescription> m_RemoteInstances = new();

        [SerializeField] private bool m_OverridePort;
        [SerializeField] private ushort m_Port;

        private Scenario m_Scenario;

        public Scenario Scenario => m_Scenario;
        public MainEditorInstanceDescription EditorInstance => m_MainEditorInstance;
        public ReadOnlyCollection<VirtualEditorInstanceDescription> VirtualEditorInstances => m_EditorInstances.AsReadOnly();
        public ReadOnlyCollection<LocalInstanceDescription> LocalInstances => m_LocalInstances.AsReadOnly();
        public ReadOnlyCollection<RemoteInstanceDescription> RemoteInstances => m_RemoteInstances.AsReadOnly();

        public override bool SupportsPauseAndStep => true;

        // The following section is for upgrading from 1.0.0-pre.2 to 1.0.0-pre.3.
        // Because m_MainEditorInstance was serialized as reference we need to manually copy the old values to the new instance.
        [SerializeReference, FormerlySerializedAs("m_MainEditorInstance")] private MainEditorInstanceDescription m_MainEditorInstanceObsolete;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (m_MainEditorInstanceObsolete != null)
            {
                var serialized = JsonUtility.ToJson(m_MainEditorInstanceObsolete);
                JsonUtility.FromJsonOverwrite(serialized, m_MainEditorInstance);
                m_MainEditorInstanceObsolete = null;
            }
        }
        // End upgrade section.

        public List<InstanceDescription> GetAllInstances()
        {
            var instances = new List<InstanceDescription>();

            if (m_EnableEditors)
            {
                Assert.IsNotNull(m_MainEditorInstance);
                m_MainEditorInstance.Name = MultiplayerPlaymode.PlayerOne.Name;
                instances.Add(m_MainEditorInstance);
                for (var i = 0; i < m_EditorInstances.Count; i++)
                {
                    var playerIndex = i + 1;// Main editor is PlayerInstanceIndex 0
                    m_EditorInstances[i].PlayerInstanceIndex = playerIndex;
                    m_EditorInstances[i].Name = MultiplayerPlaymode.Players[playerIndex].Name;
                    instances.Add(m_EditorInstances[i]);
                }
            }

            instances.AddRange(m_LocalInstances);
            instances.AddRange(m_RemoteInstances);
            return instances;
        }

        public override Task ExecuteStartAsync(CancellationToken cancellationToken)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return Task.FromCanceled(new CancellationToken(true));

            m_Scenario = ScenarioFactory.CreateScenario(name, GetAllInstances());
            ScenarioRunner.StartScenario(m_Scenario);

            return Task.CompletedTask;
        }

        public override void ExecuteStop()
        {
            ScenarioRunner.StopScenario();

            var state = ScenarioRunner.GetScenarioStatus();
            if (state.State == Api.ScenarioState.Failed)
            {
                if (state.ErrorMessages != null)
                {
                    var errors = string.Join("\n\t -> ", state.ErrorMessages);
                    Debug.LogError($"Scenario failed with error:\n\t -> {errors}");
                }
                else
                    Debug.LogError($"Scenario failed with unknown error.");
            }
        }

        public override VisualElement CreateTopbarUI() => new MultiplayerPlayModeStatusButton(this);
        public override Texture2D Icon => Icons.GetImage(Icons.ImageName.PlayModeScenario);

        public override string Description
        {
            get
            {
                var summary = "\n1 Editor instance\n";

                var localInstanceCount = m_LocalInstances.Count;
                if (localInstanceCount > 0)
                    summary += $"{localInstanceCount} Local instance{(localInstanceCount > 1 ? "s" : "")}\n";
                var remoteInstanceCount = m_RemoteInstances.Count;
                if (remoteInstanceCount > 0)
                    summary += $"{remoteInstanceCount} Remote instance{(remoteInstanceCount > 1 ? "s" : "")}\n";

                return (base.Description + summary).Trim('\n');
            }
        }

        public override bool IsConfigurationValid(out string reasonForInvalidConfiguration)
        {
            reasonForInvalidConfiguration = "";

            // Check if instance names are unique
            List<string> takenNames = new List<string>();
            var allInstances = GetAllInstances().Distinct();
            foreach (var instance in allInstances)
            {
                if (takenNames.Contains(instance.Name))
                {
                    reasonForInvalidConfiguration = "Instance names must be unique.";
                    break;
                }
                takenNames.Add(instance.Name);
            }

            // Check if local build targets are supported for building
            var localBuildTargetsAreSupported = m_LocalInstances.Count == 0 | m_LocalInstances.All(instance => instance != null && instance.BuildProfile != null && InternalUtilities.IsBuildProfileSupported(instance.BuildProfile));
            if (!localBuildTargetsAreSupported)
                reasonForInvalidConfiguration += "\nLocal instance(s) have incorrect build target";

            // Check if local build targets are supported to run.
            // This is necessary because if for example Linux Build Support is available but we are running on Windows, we can build but we cannot start the instance.
            var localBuildTargetsCanRunOnPlatform = m_LocalInstances.Count == 0 | m_LocalInstances.All(instance => instance != null && instance.BuildProfile != null && InternalUtilities.BuildProfileCanRunOnCurrentPlatform(instance.BuildProfile));
            if (!localBuildTargetsCanRunOnPlatform)
                reasonForInvalidConfiguration += "\nLocal instance(s) buildtarget cannot run on current platform.";

            // Check if we have the correct packages installed for running a remote server.
            if (!PackagesForRemoteDeployInstalled(out var missingPacks) && m_RemoteInstances.Count > 0)
                reasonForInvalidConfiguration += "\nPackages are missing:\n" + string.Join("\n", missingPacks);

            // Check if remote build targets are supported to be build.
            var remoteBuildTargetsCorrect = m_RemoteInstances.Count == 0 | m_RemoteInstances.All(instance => instance != null && instance.BuildProfile != null && InternalUtilities.IsBuildProfileSupported(instance.BuildProfile));
            if (!remoteBuildTargetsCorrect)
                reasonForInvalidConfiguration += "\nRemote instance(s) have incorrect build target.";

            // Check if we have more than one server role.
            var configHasMoreServerInstances = ConfigurationHasMaxOneServer();
            if (!configHasMoreServerInstances)
                reasonForInvalidConfiguration += "\nOnly one Server Role is allowed per Configuration.";

            reasonForInvalidConfiguration = reasonForInvalidConfiguration.Trim('\n');
            return localBuildTargetsAreSupported && remoteBuildTargetsCorrect && localBuildTargetsCanRunOnPlatform && configHasMoreServerInstances;
        }

        bool ConfigurationHasMaxOneServer()
        {
#if UNITY_USE_MULTIPLAYER_ROLES
            var allInstances = GetAllInstances();
            var serverCount = allInstances.Count(instance => ScenarioFactory.GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server));
            return serverCount < 2;
#else
            return true;
#endif
        }


        public static bool PackagesForRemoteDeployInstalled(out List<string> missingPacks)
        {
            missingPacks = new List<string>();

            foreach (var packIds in k_RequiredPackagesForRemoteInstances)
            {
                var nameParts = packIds.Split('@');
                var packageName = nameParts[0];
                var requiredVersion = nameParts[1];
                var packInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
                var packageIsInstalled = false;
                if (packInfo != null)
                {
                    var installedVersion = packInfo.version;
                    packageIsInstalled = IsPackageVersionCompatible(installedVersion, requiredVersion);
                }

                var packInstalled = packInfo != null && packageIsInstalled;
                if (!packInstalled)
                    missingPacks.Add(packIds);
            }
            return missingPacks.Count == 0;
        }

        private static bool IsPackageVersionCompatible(string installedVersion, string requiredVersion)
        {
            SplitPackageVersion(installedVersion, out var installedMajor, out var installedMinor, out var installedPatch, out var installedPre, out var isInstalledPre);
            SplitPackageVersion(requiredVersion, out var requiredMajor, out var requiredMinor, out var requiredPatch, out var requiredPre, out var isRequiredPre);

            if (installedMajor < requiredMajor)
                return false;

            if (installedMajor > requiredMajor)
                return true;

            if (installedMinor < requiredMinor)
                return false;

            if (installedMinor > requiredMinor)
                return true;

            if (installedPatch < requiredPatch)
                return false;

            if (installedPatch > requiredPatch)
                return true;

            if (isInstalledPre && !isRequiredPre)
                return false;

            if (isInstalledPre && isRequiredPre && installedPre < requiredPre)
                return false;

            return true;
        }

        private static void SplitPackageVersion(string version, out int major, out int minor, out int patch, out int pre, out bool isPre)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"(\d+)\.(\d+)\.(\d+)(?:-pre\.(\d+))?");
            var match = regex.Match(version);

            if (!match.Success)
                throw new ArgumentException($"Invalid version string: {version}");

            major = int.Parse(match.Groups[1].Value);
            minor = int.Parse(match.Groups[2].Value);
            patch = int.Parse(match.Groups[3].Value);

            isPre = match.Groups[4].Success;
            pre = isPre ? int.Parse(match.Groups[4].Value) : -1;
        }
    }
}

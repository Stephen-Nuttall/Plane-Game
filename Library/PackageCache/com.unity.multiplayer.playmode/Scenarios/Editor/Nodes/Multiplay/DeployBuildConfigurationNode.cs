// TODO: This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    internal class DeployBuildConfigurationNode : MultiplayNode
    {
        private const string k_ErrorMessage_DeployBuildConfigurationFailed = "Failed to deploy the build configuration";

        [SerializeReference] public NodeInput<string> BuildConfigurationName;
        [SerializeReference] public NodeInput<string> BuildName;
        [SerializeReference] public NodeInput<long> BuildId;
        [SerializeReference] public NodeInput<string> BinaryPath;
        [SerializeReference] public NodeInput<BuildConfigurationSettings> Settings;

        [SerializeReference] public NodeOutput<long> BuildConfigurationId;

        public DeployBuildConfigurationNode(string name, IScopedServiceProvider serviceProviderOverride = null)
            : base(name, serviceProviderOverride)
        {
            BuildConfigurationName = new(this);
            BuildName = new(this);
            BuildId = new(this);
            BinaryPath = new(this);
            Settings = new(this);

            BuildConfigurationId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var buildConfigItem = GenerateBuildConfigurationItem();

            buildConfigItem.PropertyChanged += OnBuildConfigStatusChanged;
            using var scope = await CreateAndValidateServiceProvider();

            var deployer = scope.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();

            var (buildConfigIds, failedBuildConfigs) = await deployer.DeployBuildConfigs(
                new List<BuildConfigurationItem> { buildConfigItem },
                new Dictionary<BuildName, BuildId>() {
                {
                    new BuildName { Name = buildConfigItem.Definition.Build.Name },
                    new BuildId { Id = GetInput(BuildId) }
                }},
                cancellationToken);

            ValidateDeployment(failedBuildConfigs);

            SetOutput(BuildConfigurationId, buildConfigIds[buildConfigItem.OriginalName].Id);
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
            ValidateNameParameter(GetInput(BuildConfigurationName), nameof(BuildConfigurationName));
            ValidateInputIsSet(BuildName, nameof(BuildName));
            ValidateInputIsSet(BuildId, nameof(BuildId));
            ValidateInputIsSet(BinaryPath, nameof(BinaryPath));
            ValidateInputIsSet(Settings, nameof(Settings));

            var settings = GetInput(Settings);
            ValidateSettingsIsSet(settings.CommandLineArguments, nameof(settings.CommandLineArguments));
            ValidateSettingsIsSet(settings.CoresCount, nameof(settings.CoresCount));
            ValidateSettingsIsSet(settings.MemoryMiB, nameof(settings.MemoryMiB));
            ValidateSettingsIsSet(settings.SpeedMhz, nameof(settings.SpeedMhz));
        }

        private static void ValidateSettingsIsSet<T>(T value, string parameterName)
        {
            if (value == null || value.Equals(default(T)))
                throw new InvalidOperationException($"{parameterName} is not set");
        }

        private void OnBuildConfigStatusChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var buildConfigItem = (BuildConfigurationItem)sender;
            SetProgress(buildConfigItem.Progress / 100f);

            if (e.PropertyName == "Status")
                DebugUtils.Trace($"Build configuration status changed: {buildConfigItem.Status.Message}");
        }

        private void ValidateDeployment(List<BuildConfigurationItem> failedBuildConfigs)
        {
            if (failedBuildConfigs.Count > 0)
            {
                var errors = failedBuildConfigs.Select(
                    bi => bi.Status.Message
                ).ToList();
                var errList = string.Join("; ", errors);
                throw new Exception($"{k_ErrorMessage_DeployBuildConfigurationFailed}\n{errList}");
            }
        }

        private BuildConfigurationItem GenerateBuildConfigurationItem()
        {
            return new()
            {
                OriginalName = new BuildConfigurationName
                {
                    Name = GetInput(BuildConfigurationName)
                },
                Definition = new MultiplayConfig.BuildConfigurationDefinition
                {
                    Build = new BuildName
                    {
                        Name = GetInput(BuildName)
                    },
                    BinaryPath = GetInput(BinaryPath),
                    CommandLine = GetInput(Settings).CommandLineArguments,
                    Cores = GetInput(Settings).CoresCount,
                    MemoryMiB = GetInput(Settings).MemoryMiB,
                    SpeedMhz = GetInput(Settings).SpeedMhz,
                    QueryType = MultiplayConfig.Query.None
                }
            };
        }
    }
}
#endif

// TODO: This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    internal class DeployFleetNode : MultiplayNode
    {
        private const string k_ErrorMessage_FleetDeploymentFailed = "Failed to deploy the fleet.";

        [SerializeReference] public NodeInput<string> FleetName;
        [SerializeReference] public NodeInput<string> Region;
        [SerializeReference] public NodeInput<string> BuildConfigurationName;
        [SerializeReference] public NodeInput<long> BuildConfigurationId;

        public DeployFleetNode(string name, IScopedServiceProvider serviceProviderOverride = null)
            : base(name, serviceProviderOverride)
        {
            FleetName = new(this);
            Region = new(this);
            BuildConfigurationName = new(this);
            BuildConfigurationId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var fleetItem = GenerateFleetItem();
            fleetItem.PropertyChanged += OnFleetStatusChanged;

            using var provider = await CreateAndValidateServiceProvider();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();

            var fleets = new List<FleetItem> { fleetItem };
            var buildConfigIds = new Dictionary<BuildConfigurationName, BuildConfigurationId>
            {
                {
                    new BuildConfigurationName { Name = GetInput(BuildConfigurationName) },
                    new BuildConfigurationId { Id = GetInput(BuildConfigurationId) }
                }
            };
            await deployer.DeployFleets(fleets, buildConfigIds, cancellationToken);
            ValidateFleetDeployment(fleetItem);
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(FleetName, nameof(FleetName));
            ValidateNameParameter(GetInput(FleetName), nameof(FleetName));
            ValidateInputIsSet(Region, nameof(Region));
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
            ValidateInputIsSet(BuildConfigurationId, nameof(BuildConfigurationId));
        }

        private void ValidateFleetDeployment(FleetItem fleetItem)
        {
            if (fleetItem.Status.MessageSeverity == SeverityLevel.Error)
            {
                throw new Exception($"{k_ErrorMessage_FleetDeploymentFailed}\n{fleetItem.Status.MessageDetail}");
            }
        }

        private FleetItem GenerateFleetItem()
        {
            return new()
            {
                OriginalName = new() { Name = GetInput(FleetName) },
                Definition = new()
                {
                    BuildConfigurations = new List<BuildConfigurationName>()
                    {
                        new() { Name = GetInput(BuildConfigurationName) }
                    },
                    Regions = new Dictionary<string, MultiplayConfig.ScalingDefinition>()
                    {
                        {
                            GetInput(Region), new MultiplayConfig.ScalingDefinition
                            {
                                MaxServers = 1,
                                MinAvailable = 0,
                            }
                        }
                    }
                }
            };
        }

        private void OnFleetStatusChanged(object sender, PropertyChangedEventArgs args)
        {
            var fleetItem = (FleetItem)sender;
            SetProgress(fleetItem.Progress / 100f);

            if (args.PropertyName == "Status")
                DebugUtils.Trace($"Fleet deployment status changed: {fleetItem.Status.Message}");
        }
    }
}
#endif

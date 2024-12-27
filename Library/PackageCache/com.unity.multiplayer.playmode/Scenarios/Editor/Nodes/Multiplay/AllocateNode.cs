// TODO: This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    class AllocateNode : MultiplayNode
    {
        [SerializeReference] public NodeInput<string> FleetName;
        [SerializeReference] public NodeInput<string> BuildConfigurationName;

        [SerializeReference] public NodeOutput<long> ServerId;
        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        public AllocateNode(string name, IScopedServiceProvider serviceProviderOverride = null)
            : base(name, serviceProviderOverride)
        {
            FleetName = new(this);
            BuildConfigurationName = new(this);
            ServerId = new(this);
            ConnectionDataOut = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            using var provider = await CreateAndValidateServiceProvider();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();

            var info = await deployer.CreateAndSyncTestAllocationAsync(
                new FleetName { Name = GetInput(FleetName) },
                new BuildConfigurationName { Name = GetInput(BuildConfigurationName) },
                cancellationToken);

            ValidateAllocationResult(info);

            SetOutput(ServerId, info.ServerId);
            SetOutput(ConnectionDataOut, new ConnectionData
            {
                IpAddress = info.Ipv4Address,
                Port = (ushort)info.GamePort,
            });
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(FleetName, nameof(FleetName));
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
        }

        private void ValidateAllocationResult(AllocationInformation info)
        {
            if (info == null)
                throw new Exception("Failed to allocate server.");
        }
    }
}
#endif

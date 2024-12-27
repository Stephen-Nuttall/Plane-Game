// TODO: This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Editor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    internal class DeployBuildNode : MultiplayNode
    {
        private const string k_ErrorMessage_BuildPathDoesNotExist = "Build path does not exist";
        private const string k_ErrorMessage_ExecutablePathDoesNotExist = "Executable path does not exist";
        private const string k_ErrorMessage_BuildUploadFailed = "Failed to upload the build. Have you configured the Multiplay Service for this project?";
        private const string k_ErrorMessage_BuildSyncFailed = "Failed to sync the build";

        [SerializeReference] public NodeInput<string> BuildName;
        [SerializeReference] public NodeInput<string> BuildPath;
        [SerializeReference] public NodeInput<string> ExecutablePath;
        [SerializeReference] public NodeInput<Hash128> BuildHash;

        [SerializeReference] public NodeOutput<long> BuildId;

        public DeployBuildNode(string name, IScopedServiceProvider serviceProviderOverride = null)
            : base(name, serviceProviderOverride)
        {
            BuildName = new(this);
            BuildPath = new(this);
            ExecutablePath = new(this);
            BuildHash = new(this);

            BuildId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            using var provider = await CreateAndValidateServiceProvider();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();

            if (await ProcessReuseBuild(deployer, cancellationToken))
                return;

            var buildItem = GenerateBuildItem();
            buildItem.PropertyChanged += OnBuildStatusChanged;

            var uploadResult = await deployer.UploadAndSyncBuilds(new List<BuildItem> { buildItem }, cancellationToken);

            ValidateBuildUpload(uploadResult);

            var buildId = uploadResult.SuccessfulSyncs[buildItem.OriginalName].Id;

            SetOutput(BuildId, buildId);

            var version = await GetVersionOfBuild(deployer, buildId, cancellationToken);
            DeployData.instance.AssignHashToBuildId(buildId, version, GetInput(BuildHash));
        }

        private async Task<bool> ProcessReuseBuild(IMultiplayDeployer deployer, CancellationToken cancellationToken)
        {
            var buildHash = GetInput(BuildHash);
            if (DeployData.instance.FindBuildIdAndVersionForHash(buildHash, out var buildId, out var localVersion)
                && localVersion != -1)
            {
                var remoteVersion = await GetVersionOfBuild(deployer, buildId, cancellationToken);

                if (remoteVersion != localVersion)
                    return false;

                SetOutput(BuildId, buildId);
                return true;
            }

            return false;
        }

        private static async Task<long> GetVersionOfBuild(IMultiplayDeployer deployer, long buildId, CancellationToken cancellationToken)
        {
            var builds = await deployer.GetBuilds(cancellationToken);
            foreach (var build in builds)
            {
                if (build.BuildId.Id == buildId)
                {
                    // TODO: We don't have access to the version of the build in the API yet.
                    // For now, the updated time would do the same.
                    return build.Updated.ToBinary();
                }
            }

            return -1;
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(BuildName, nameof(BuildName));
            ValidateNameParameter(GetInput(BuildName), nameof(BuildName));
            ValidateInputIsSet(BuildPath, nameof(BuildPath));
            ValidateInputIsSet(ExecutablePath, nameof(ExecutablePath));
            ValidateInputIsSet(BuildHash, nameof(BuildHash));

            if (!Directory.Exists(GetInput(BuildPath)))
                throw new FileNotFoundException(k_ErrorMessage_BuildPathDoesNotExist, GetInput(BuildPath));

            if (!File.Exists(GetInput(ExecutablePath)))
                throw new FileNotFoundException(k_ErrorMessage_ExecutablePathDoesNotExist, GetInput(ExecutablePath));
        }

        private void OnBuildStatusChanged(object sender, PropertyChangedEventArgs args)
        {
            var buildItem = (BuildItem)sender;
            SetProgress(buildItem.Progress / 100f);

            if(args.PropertyName == "Status")
                DebugUtils.Trace($"Build deployment status changed: {buildItem.Status.Message}");
        }

        private void ValidateBuildUpload(IMultiplayDeployer.UploadResult result)
        {
            if (result.FailedUploads.Count > 0)
            {
                var errors = result.FailedUploads.Select(
                    bi => bi.Status.Message
                ).ToList();
                var errList = string.Join("; ", errors);
                throw new Exception($"{k_ErrorMessage_BuildUploadFailed}\n{errList}");
            }

            if (result.FailedSyncs.Count > 0)
            {
                var errors = result.FailedSyncs.Select(
                    bi => bi.Status.Message
                ).ToList();
                var errList = string.Join("; ", errors);
                throw new Exception($"{k_ErrorMessage_BuildSyncFailed}\n{errList}");
            }
        }

        private BuildItem GenerateBuildItem()
        {
            return new ()
            {
                OriginalName = new BuildName
                {
                    Name = GetInput(BuildName)
                },
                Definition = new MultiplayConfig.BuildDefinition
                {
                    BuildPath = GetInput(BuildPath),
                    ExecutableName = Path.GetFileName(GetInput(ExecutablePath))
                }
            };
        }
    }
}
#endif

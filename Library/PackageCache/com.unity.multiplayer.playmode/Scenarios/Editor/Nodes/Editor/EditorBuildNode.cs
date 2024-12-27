using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Build.Profile;
using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor
{
    class EditorBuildNode : Node
    {
        [SerializeReference] public NodeInput<string> BuildPath;
        [SerializeReference] public NodeInput<BuildProfile> Profile;

        [SerializeReference] public NodeOutput<string> ExecutablePath;
        [SerializeReference] public NodeOutput<string> OutputPath;
        [SerializeReference] public NodeOutput<string> RelativeExecutablePath;
        [SerializeReference] public NodeOutput<Hash128> BuildHash;

        public EditorBuildNode(string name) : base(name)
        {
            BuildPath = new(this);
            Profile = new(this);

            OutputPath = new(this);
            ExecutablePath = new(this); ;
            RelativeExecutablePath = new(this);
            BuildHash = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                DebugUtils.Trace("Can't build when there are compile errors, aborting build");
                throw new ApplicationException("Script compilation failed, aborting build.");
            }

            // TODO: Make sure we can reuse builds when possible.

            ExecuteBuildCommand();

            return Task.CompletedTask;
        }

        void ExecuteBuildCommand()
        {
            var buildPath = GetInput(BuildPath);
            var buildProfile = GetInput(Profile);


            // Save the currently active build target, it's sub-target and the multiplayer role in case of they would be modified by the build.
            var previousProfile = InternalUtilities.GetActiveOrClassicProfile();
            DebugUtils.Trace("Building");

            BuildReport report;
            try
            {
                // Build the player with the specified build profile.
                report = BuildPipeline.BuildPlayer(
                    new BuildPlayerWithProfileOptions
                    {
                        buildProfile = buildProfile,
                        locationPathName = InternalUtilities.AddBuildExtension(buildPath, buildProfile),
                    });

                if (report == null)
                    throw new Exception("BuildPipeline.BuildPlayer failed to generate a build report. The build artifact is likely corrupted.");
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new Exception(report.SummarizeErrors());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            var outputPath = Path.GetDirectoryName(report.summary.outputPath);
            var executablePath = ExtractExecutablePath(report);
            SetOutput(OutputPath, outputPath);
            SetOutput(ExecutablePath, executablePath);
            SetOutput(RelativeExecutablePath, Path.GetRelativePath(outputPath, executablePath));
            SetOutput(BuildHash, ComputeBuildHash(outputPath));

            // Restore the active build target, it's sub-target and the multiplayer role to the state they were prior to the build.
            var currentProfile = InternalUtilities.GetActiveOrClassicProfile();
            if (previousProfile != currentProfile)
                InternalUtilities.SetActiveOrClassicProfile(previousProfile);
        }

        private static readonly ProfilerMarker s_ComputeBuildHash = new("EditorBuildNode.ComputeBuildHash");
        private static Hash128 ComputeBuildHash(string buildPath)
        {
            // This provides a unique hash for the build based on its content.
            // If two builds share the same hash, it means they have equivalent content.
            // The current implementation is focused on Linux builds, that is because those are the
            // ones uploaded to the cloud. Mac builds, for instance, have a signing process that alter
            // this hash, and therefore extending the support to Mac builds will require more work and
            // potentially a different approach.

            using var _ = s_ComputeBuildHash.Auto();

            var files = GetAllBuildFilesForHash(buildPath);
            return HashUtils.ComputeForFiles(files);
        }

        private static string[] GetAllBuildFilesForHash(string buildPath)
        {
            var files = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories);
            var filteredFiles = new List<string>(files.Length);
            for (var i = files.Length - 1; i >= 0; i--)
            {
                var file = files[i];
                // boot.config contains a build guid which is unique to each build, so we should not
                // use it to compute the build hash.
                if (file.EndsWith(".DS_Store") || file.EndsWith("boot.config"))
                    continue;

                filteredFiles.Add(file);
            }

            return filteredFiles.ToArray();
        }

        private static string ExtractExecutablePath(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
                if (report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server)
                    return $"{report.summary.outputPath}/{Application.productName}";
                return $"{report.summary.outputPath}/Contents/MacOS/{Application.productName}";
            }

            return report.summary.outputPath;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    [Serializable]
    class BuildConfiguration
    {
        public string BuildName;
        public BuildPlayerOptions BuildOptions;
#if UNITY_USE_MULTIPLAYER_ROLES
        public MultiplayerRoleFlags MultiplayerRoleFlags;
#endif
        public NamedBuildTarget NamedBuildTarget
        {
            get
            {
                var subtarget = BuildOptions.subtarget;
                var buildTarget = BuildOptions.target;
                var buildTargetGroup = BuildOptions.targetGroup;
                var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
                if (namedBuildTarget == NamedBuildTarget.Standalone && (StandaloneBuildSubtarget)subtarget == StandaloneBuildSubtarget.Server)
                    namedBuildTarget = NamedBuildTarget.Server;
                return namedBuildTarget;
            }
        }
    }

    [Serializable]
    class BuildDescription
    {
        public BuildConfiguration BuildConfiguration;
        public int BuildId;
        public string BuildPath;
        public string ExecutableName;
        public BuildReport BuildReport;
        public Dictionary<string, string> Manifest;
    }

    [Serializable]
    class DeploymentDescription
    {
        public string WorkingDirectory;
        public BuildDescription BuildDescription { get; set; }
    }

    [Serializable]
    class ConnectionData
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 7777;

        public override string ToString()
        {
            return $"IP: {IpAddress}, Port: {Port}";
        }
    }

    [Serializable]
    class RunConfiguration
    {
        public string CommandlineArguments { get; set; }
        public string LogsColorHex { get; set; }
        public string EnvironmentArguments { get; set; }
        public string WorkingDirectory { get; set; }
    }

    [Serializable]
    class RunResult
    {
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public long RemoteId { get; set; }
    }

    [Serializable]
    class StopResult
    {
        public int ExitCode;
    }
}

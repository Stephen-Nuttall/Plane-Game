using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEditorInternal;

namespace Unity.Multiplayer.PlayMode.Editor.Bridge
{
    internal class InternalUtilities
    {
        public static bool IsDomainReloadRequested() => InternalEditorUtility.IsScriptReloadRequested();

        public static bool IsServerProfile(BuildProfile buildProfile)
        {
            return buildProfile.subtarget == StandaloneBuildSubtarget.Server && BuildProfileModuleUtil.IsStandalonePlatform(buildProfile.buildTarget);
        }

        internal static string AddBuildExtension(string path, BuildProfile profile)
        {
            switch (profile.buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    return Path.ChangeExtension(path, ".exe");
                case BuildTarget.StandaloneOSX:
                    if (profile.subtarget == StandaloneBuildSubtarget.Player)
                        return Path.ChangeExtension(path, ".app");
                    return Path.ChangeExtension(path, "");
            }

            return path;
        }

        internal static Texture2D GetBuildProfileTypeIcon(BuildProfile buildProfile)
        {
            if (buildProfile == null)
            {
                return Texture2D.grayTexture;
            }

            return BuildProfileModuleUtil.GetPlatformIconSmall(buildProfile.platformId);
        }

        internal static bool IsBuildProfileSupported(BuildProfile buildProfile)
        {
            var supported = BuildProfileModuleUtil.IsBuildProfileSupported(buildProfile.platformId);
            var installed = BuildProfileModuleUtil.IsModuleInstalled(buildProfile.platformId);
            return supported && installed;
        }

        internal static bool BuildProfileCanRunOnCurrentPlatform(BuildProfile buildProfile)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return buildProfile.buildTarget is BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows;
            }

            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                return buildProfile.buildTarget is BuildTarget.StandaloneLinux64 or BuildTarget.LinuxHeadlessSimulation or BuildTarget.EmbeddedLinux;
            }

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return buildProfile.buildTarget is BuildTarget.StandaloneOSX;
            }

            return IsBuildProfileSupported(buildProfile);
        }

        public static BuildProfile GetActiveOrClassicProfile()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null)
                return profile;

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var subtarget = BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone
                ? EditorUserBuildSettings.standaloneBuildSubtarget
                : StandaloneBuildSubtarget.Default;

            return BuildProfileContext.instance.GetForClassicPlatform(buildTarget, subtarget);
        }

        public static void SetActiveOrClassicProfile(BuildProfile profile)
        {
            if (BuildProfileContext.IsClassicPlatformProfile(profile))
            {
                var buildTarget = profile.buildTarget;
                if (BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone)
                    EditorUserBuildSettings.standaloneBuildSubtarget = profile.subtarget;

                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget);
                BuildProfile.SetActiveBuildProfile(null);
            }
            else
                BuildProfile.SetActiveBuildProfile(profile);
        }
    }
}

using System;
using UnityEditor;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class AssetDatabaseCallbacks : AssetPostprocessor
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once Unity.IncorrectMethodSignature
        // ReSharper disable SuggestBaseTypeForParameter
        /* OnPostProcessAllAssets has two signatures and the one with didDomainReload is hidden */
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            var totalCount = importedAssets.Length + deletedAssets.Length + movedAssets.Length + movedFromAssetPaths.Length;
            // AssetPostprocessingInternal::PostprocessAllAssets also does logic based off of containing no assets.
            // We need to pass this through to mirror the same logic
            // NOTE: This does mean this event fires more times than before!
            OnPostprocessAllAssetsCallback?.Invoke(didDomainReload, totalCount);
        }

        public static event Action<bool, int> OnPostprocessAllAssetsCallback;
    }
}

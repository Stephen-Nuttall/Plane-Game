using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    class AssetMonitor : AssetPostprocessor
    {
        static bool s_HasChanged = false;

        public static bool HasChanges => s_HasChanged;

        public static void Reset()
        {
            s_HasChanged = false;
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0)
            {
                DebugUtils.Trace($"Changes detected, importedAssets: {importedAssets.Length}, deletedAssets: {deletedAssets.Length}, movedAssets: {movedAssets.Length}, movedFromAssetPaths: {movedFromAssetPaths.Length}, didDomainReload: {didDomainReload}");
                s_HasChanged = true;
            }
        }

        private static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] compilerMessages)
        {
            s_HasChanged = true;
            DebugUtils.Trace("Assembly compilation finished for: " + assembly);
        }

        private static void OnCompilationFinished(object result)
        {
            s_HasChanged = true;
            DebugUtils.Trace("Compilation finished for: " + result);
        }
    }
}

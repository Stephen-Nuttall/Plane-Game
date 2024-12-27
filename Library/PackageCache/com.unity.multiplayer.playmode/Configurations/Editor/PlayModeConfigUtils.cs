using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor
{
    internal static class PlayModeConfigUtils
    {
        /// <summary>
        /// Will get invoked when a ScenarioConfig is added or removed.
        /// </summary>
        internal static event Action ConfigurationAddedOrRemoved;

        private static List<PlayModeConfig> s_AllConfigs;

        internal static bool IsPlayModeConfigAsset(string assetPath)
        {
            return typeof(PlayModeConfig).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        internal static List<PlayModeConfig> GetAllConfigs()
        {
            if (s_AllConfigs == null)
            {
                s_AllConfigs = new List<PlayModeConfig>();
                var guids = AssetDatabase.FindAssets($"t:{nameof(PlayModeConfig)}");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var config = (PlayModeConfig)AssetDatabase.LoadAssetAtPath(path, typeof(PlayModeConfig));
                    if (config == null)
                        continue;
                    s_AllConfigs.Add(config);
                }

                s_AllConfigs = s_AllConfigs.OrderBy(c => c.name).ToList();
            }

            return s_AllConfigs;
        }

        internal static Type[] GetAllConfigTypes()
        {
            return TypeCache.GetTypesDerivedFrom<PlayModeConfig>().ToArray();
        }

        internal static PlayModeConfig CreatePlayModeConfig(string name, Type type)
        {
            const string k_FolderPath = "Assets/Settings/PlayMode";
            Assert.IsTrue(typeof(PlayModeConfig).IsAssignableFrom(type));

            if (!Directory.Exists(k_FolderPath))
                Directory.CreateDirectory(k_FolderPath);

            var config = ScriptableObject.CreateInstance(type) as PlayModeConfig;
            config.name = name;
            AssetDatabase.CreateAsset(config, $"{k_FolderPath}/{config.name}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return config;
        }

        /// <summary>
        /// Copies the given PlayModeConfig and increments the number at the end if necessary.
        /// Behaviour is the same as in ProjectBrowser.
        /// </summary>
        /// <param name="conf"></param>
        internal static void CopyPlayModeConfiguration(PlayModeConfig conf)
        {
            var path = AssetDatabase.GetAssetPath(conf);
            var last = path.Split(' ').Last();
            var withoutExtension = last.Split('.').First();

            // True if the path already has a space followed with a number
            // for example "MyConfig 1.asset"
            var pathAlreadyHasCounter = int.TryParse(withoutExtension, out var counter);

            // make sure we have at least 1
            counter = Math.Max(counter, 1);

            var pathIsValid = false;
            while (pathIsValid == false)
            {
                var newPath = path.Replace(".asset", $" {counter}.asset");
                if (pathAlreadyHasCounter)
                    newPath = path.Replace(last, $"{counter}.asset");

                pathIsValid = !AssetDatabase.AssetPathExists(newPath);
                if (pathIsValid)
                {
                    AssetDatabase.CopyAsset(path, newPath);
                }
                counter++;
            }
        }

        private static void ClearCache()
        {
            s_AllConfigs = null;
        }

        /// <summary>
        /// Tracks changes to ScenarioConfigs in the project.
        /// </summary>
        private class ConfigAssetsTracker : AssetPostprocessor
        {
            public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var needsUpdate = false;

                foreach (var changedAsset in importedAssets)
                {
                    if (IsPlayModeConfigAsset(changedAsset))
                    {
                        needsUpdate = true;
                        break;
                    }
                }

                // If something got deleted we have to refresh, because we cannot check
                // if the deleted asset was a ScenarioConfig.
                if (deletedAssets.Length > 0)
                    needsUpdate = true;

                if (needsUpdate)
                {
                    ClearCache();
                    ConfigurationAddedOrRemoved?.Invoke();
                }
            }
        }
    }
}

using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class AssetDatabaseHelper
    {
        /// <summary>
        /// At the moment there's an existent issue with the AssetDatabase that in certain situations the asset isn't fully loaded,
        /// for instance, when using Overlays, if load an asset from CreatePanelContent method, the asset might not be available yet.
        /// This method waits for the asset to be fully loaded asynchronously.
        /// </summary>
        /// <param name="filePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> LoadAssetAtPathAsync<T>(string filePath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(filePath);

            while (asset == null)
            {
                await Task.Yield();
                asset = AssetDatabase.LoadAssetAtPath<T>(filePath);
            }

            return asset;
        }
    }
}

using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    internal static class UIUtils
    {
        internal static void ApplyStyleSheet(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"Style asset '{path}' does not exist.");

            var loadAssetAtPath = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            visualElement.styleSheets.Add(loadAssetAtPath);
        }

        internal static async Task ApplyStyleSheetAsync(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"Style asset '{path}' does not exist.");

            var loadAssetAtPath = await AssetDatabaseHelper.LoadAssetAtPathAsync<StyleSheet>(path);
            visualElement.styleSheets.Add(loadAssetAtPath);
        }

        internal static async Task LoadUxmlAsync(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"UXML asset '{path}' does not exist.");


            var loadAssetAtPath = await AssetDatabaseHelper.LoadAssetAtPathAsync<VisualTreeAsset>(path);
            loadAssetAtPath.CloneTree(visualElement);
        }
    }
}

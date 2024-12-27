using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class AssetUtils
    {
        public static string PathFromConstant(string constantValue, params string[] paths)
        {
            var combinedPaths = new string[paths.Length + 1];
            combinedPaths[0] = constantValue;
            paths.CopyTo(combinedPaths, 1);
            return Path.Combine(paths);
        }

        public static string Style(params string[] paths)
        {
            return Path.Combine(PathFromConstant(Constants.StylesRootPath, paths));
        }

        public static string Style(string filePath)
        {
            return Path.Combine(Constants.StylesRootPath, filePath);
        }

        public static StyleSheet LoadStyle(string filePath)
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(Style(filePath));
        }

        public static StyleSheet LoadStyle(params string[] paths)
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(Style(paths));
        }
    }
}

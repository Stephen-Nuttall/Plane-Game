using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Helper class to load Icons that will get assigned via code
    /// Use Icons via USS if possible and only assign via code if
    /// there is no good way to assign via USS.
    ///
    /// Code parts taken from MPPM Images.cs
    /// </summary>

    static class Icons
    {
        internal enum ImageName
        {
            Loading,
            Error,
            CompletedTask,
            PlayModeScenario,
            Warning,
            UnityLogo,
            Idle,
            Help
        }

        /// <summary>
        /// Used to have a central place to use in editor shipped icons, so if a
        /// icon that exists in the editor is used in many places add it here, so
        /// if we have to change it in the future we only have one spot to change.
        /// </summary>
        static readonly Dictionary<ImageName, string> k_InternalIcons = new()
        {
            { ImageName.Warning, "console.warnicon" },
            { ImageName.UnityLogo, "UnityLogo" },
            { ImageName.Help, "_Help" }
        };


        static readonly Dictionary<string, Texture2D> CachedImagesByPath = new();

        internal static Texture2D GetImage(ImageName imageName)
        {
            // Try to return a internal icon first.
            if (k_InternalIcons.TryGetValue(imageName, out var iconName))
            {
                return EditorGUIUtility.FindTexture(iconName);
            }

            var imageFileName2X = imageName + "@2x.png";
            var darkImageFileName2X = $"d_{imageFileName2X}";
            var imageFileRelativePath2X = Path.Combine(Constants.RootPath, "PlaymodeConfig", "UI", "Icons", imageFileName2X);
            var darkImageFileRelativePath2X = Path.Combine(Constants.RootPath, "PlaymodeConfig", "UI", "Icons", darkImageFileName2X);

            Texture2D result = null;

            if (EditorGUIUtility.isProSkin)
                result = LoadImageFromCacheOrFile(darkImageFileRelativePath2X);

            if (result != null)
                return result;

            result = LoadImageFromCacheOrFile(imageFileRelativePath2X);

            if (result != null)
                return result;

            return null; // If we can't find the image it is a developer issue or the package got altered. Please address it!
        }

        static Texture2D LoadImageFromCacheOrFile(string image2XFilePath)
        {
            var isInCache = CachedImagesByPath.TryGetValue(image2XFilePath, out var result);
            if (!isInCache || result == null) // The texture can randomly become null after a domain reload!
            {
                // If we fail to find the image in cache, or the image in cache is invalidated,
                // reread the image and place into the cache
                if (File.Exists(Path.GetFullPath(image2XFilePath)))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(image2XFilePath);
                    CachedImagesByPath[image2XFilePath] = tex;
                    return tex;
                }
            }

            return result;
        }
    }
}

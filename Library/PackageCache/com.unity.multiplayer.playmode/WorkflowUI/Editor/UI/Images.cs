using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.PlayMode.Common.Runtime;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.WorkflowUI.Editor
{
    static class Images
    {
        internal enum ImageName
        {
            Loading,
            MainEditorIcon,
            CloneEditorIcon,
            Settings,
        }

        static readonly Dictionary<string, Texture2D> CachedImagesByPath = new Dictionary<string, Texture2D>();

        internal static Texture2D GetImage(ImageName imageName)
        {
            var imageFileName2X = imageName + "@2x.png";
            var darkImageFileName2X = $"dark_{imageFileName2X}";
            var imageFileRelativePath2X = Path.Combine(UXMLPaths.UXMLWorkflowRoot, "UI", imageFileName2X);
            var darkImageFileRelativePath2X = Path.Combine(UXMLPaths.UXMLWorkflowRoot, "UI", darkImageFileName2X);

            Texture2D result = null;

            if (EditorGUIUtility.isProSkin)
                result = LoadImageFromCacheOrFile(darkImageFileRelativePath2X);

            if (result != null)
                return result;

            result = LoadImageFromCacheOrFile(imageFileRelativePath2X);

            if (result != null)
                return result;

            MppmLog.Error($"Image not found: '{imageFileRelativePath2X};");
            return null;    // If we can't find the image it is a developer issue or the package got altered. Please address it!
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
                    var fileData = File.ReadAllBytes(image2XFilePath);
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(fileData); //auto-resizes the texture dimensions
                    CachedImagesByPath[image2XFilePath] = texture;
                    return texture;
                }
            }

            return result;
        }
    }
}

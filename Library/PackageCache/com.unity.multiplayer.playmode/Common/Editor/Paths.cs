using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    // We should use this class instead of
    // using Application.dataPath or EditorApplication.applicationPath directly
    //
    // This class also completely removes the need to type Path.Combine and Path.GetFullPath
    // GetCurrentProjectDataPath("..", "Library", "VP") does all this automatically
    //
    // NOTE: These paths are all from the reference point of the current editor.

    static class Paths
    {
        public static string GetCurrentProjectDataPath(params string[] additionalPaths)
        {
            var paths = new List<string> { Application.dataPath };
            paths.AddRange(additionalPaths);
            return Path.GetFullPath(Path.Combine(paths.ToArray()));
        }

        public static string GetApplicationPath(params string[] additionalPaths)
        {
            var paths = new List<string> { EditorApplication.applicationPath };
            paths.AddRange(additionalPaths);
            return Path.GetFullPath(Path.Combine(paths.ToArray()));
        }

        public static string CurrentProjectVirtualProjectsFolder =>
            GetCurrentProjectDataPath("..", Constants.k_VirtualProjectsFolder);
    }
}

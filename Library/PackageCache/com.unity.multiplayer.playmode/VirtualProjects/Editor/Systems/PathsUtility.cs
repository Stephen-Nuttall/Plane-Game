using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.PlayMode.Common.Editor;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    static class PathsUtility
    {
        public static string GetProjectPathByIdentifier(VirtualProjectIdentifier identifier, params string[] additionalPaths)
        {
            var paths = new List<string>
            {
                Paths.CurrentProjectVirtualProjectsFolder,
                identifier.ToString(),
            };
            paths.AddRange(additionalPaths);
            return Path.GetFullPath(Path.Combine(paths.ToArray()));
        }
    }
}

using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    internal static class HashUtils
    {
        private static readonly ProfilerMarker s_ComputeForFiles = new("HashUtils.ComputeForFiles");
        public static Hash128 ComputeForFiles(params string[] files)
        {
            using var _ = s_ComputeForFiles.Auto();

            Array.Sort(files, StringComparer.Ordinal);

            var hash = new Hash128();
            foreach (var file in files)
            {
                var bytes = File.ReadAllBytes(file);
                hash.Append(bytes);
            }

            return hash;
        }
    }
}

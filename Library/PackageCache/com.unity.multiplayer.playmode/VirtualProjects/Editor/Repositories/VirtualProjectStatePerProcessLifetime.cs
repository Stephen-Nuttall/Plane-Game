using System;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    // State we keep on the main editor that is capable of surviving a domain reload
    [Serializable]
    class VirtualProjectStatePerProcessLifetime
    {
        public string[] LaunchArgs;
        public int Retry;
        public bool IsCommunicative;

        public override string ToString()
        {
            return $"{nameof(Retry)}: {Retry}, {nameof(IsCommunicative)}: {IsCommunicative}";
        }
    }
}

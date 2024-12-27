using System;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    [Serializable]
    internal struct BuildConfigurationSettings
    {
        public string CommandLineArguments;
        public int CoresCount;
        public int MemoryMiB;
        public int SpeedMhz;
    }
}

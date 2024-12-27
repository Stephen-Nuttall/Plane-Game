using JetBrains.Annotations;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    // This should eventually consolidate into VirtualProjectStatePerRun since
    // ProcessId is just another int that is persisted through domain reloads
    class ProcessId
    {
        public ProcessId()
            : this(-1)
        {
        }

        public ProcessId(int value)
        {
            Value = value;
        }

        public int Value
        {
            get;

            // Setter is used by serialization
            [UsedImplicitly]
            set;
        }
    }
}

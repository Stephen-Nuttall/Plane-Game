using System;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    [Serializable]
    struct CloneState
    {
        public bool StreamLogsToMainEditor;

        // We might want to add scenes hierarchy and play mode state here
        // so sync happens in one atomic operation.

        public override string ToString()
        {
            return $"{{StreamLogsToMainEditor: {StreamLogsToMainEditor}}}";
        }
    }
}

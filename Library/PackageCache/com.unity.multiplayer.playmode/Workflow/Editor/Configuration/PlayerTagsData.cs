using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    [Serializable]
    struct PlayerTagsData
    {
        public List<string> PlayerTags;
        public string version;
    }
}

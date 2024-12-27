using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [Serializable]
    class VirtualEditorInstanceDescription : EditorInstanceDescription
    {
        [Serializable]
        public class AdvancedConfig
        {
            [InspectorName("Stream Logs To Main Editor")] public bool StreamLogsToMainEditor;
            public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);
        }

        [SerializeField] private AdvancedConfig m_AdvancedConfiguration;

        public AdvancedConfig AdvancedConfiguration => m_AdvancedConfiguration;
    }
}

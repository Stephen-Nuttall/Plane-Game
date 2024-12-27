using System;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [Serializable]
    class LocalInstanceDescription : InstanceDescription, IBuildableInstanceDescription
    {
        //[Tooltip("Select the Build profile that this instance will be based  as.")]
        [SerializeField] private BuildProfile m_BuildProfile;

        [SerializeField] private AdvancedConfig advancedConfiguration = new();
        [Serializable]
        public class AdvancedConfig
        {
            [Tooltip("Box checked : The logs will be streamed from local instance to the editor logs \n Unchecked : The logs will be captured from local instance into the logfile under {InstanceName}.txt")]
            [SerializeField] private bool m_StreamLogsToMainEditor = true;
            [SerializeField] private Color m_LogsColor = new Color(0.3643f, 0.581f, 0.8679f);
            [SerializeField] private string m_Arguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";

            public bool StreamLogsToMainEditor
            {
                get => m_StreamLogsToMainEditor;
                set => m_StreamLogsToMainEditor = value;
            }

            public string Arguments
            {
                get => m_Arguments;
                set => m_Arguments = value;
            }

            public Color LogsColor
            {
                get => m_LogsColor;
                set => m_LogsColor = value;
            }
        }

        public BuildProfile BuildProfile
        {
            get => m_BuildProfile;
            set => m_BuildProfile = value;
        }

        public AdvancedConfig AdvancedConfiguration
        {
            get => advancedConfiguration;
            set => advancedConfiguration = value;
        }
    }
}

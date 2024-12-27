using System;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class CreatePlayModeConfigurationMenuAttribute : Attribute
    {
        private string m_Label;
        private string m_NewItemName;

        public string Label => m_Label;
        public string NewItemName => m_NewItemName;

        public CreatePlayModeConfigurationMenuAttribute(string label, string newItemName = "NewPlayModeConfiguration")
        {
            m_Label = label;
            m_NewItemName = newItemName;
        }
    }
}

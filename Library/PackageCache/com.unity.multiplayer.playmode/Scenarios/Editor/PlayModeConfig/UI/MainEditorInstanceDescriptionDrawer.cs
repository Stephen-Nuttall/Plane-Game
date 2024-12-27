
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [CustomPropertyDrawer(typeof(MainEditorInstanceDescription))]
    class MainEditorInstanceDescriptionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            container.Add(CreateRoleField(property));
            container.Add(CreateTagsField(property));
            container.Add(new PropertyField(property.FindPropertyRelative("m_InitialScene")));
            return container;
        }

        private static VisualElement CreateRoleField(SerializedProperty property)
        {
#if UNITY_USE_MULTIPLAYER_ROLES
            property = property.FindPropertyRelative("m_Role");
            var dropdown = new PopupField<MultiplayerRoleFlags>() { label = "Multiplayer Role", name= InstanceDescriptionDrawer.k_MultiplayerRolePopupName };

            dropdown.choices = Enum.GetValues(typeof(MultiplayerRoleFlags)).Cast<MultiplayerRoleFlags>().ToList();
            MultiplayerRoleFlags currentSelected = (MultiplayerRoleFlags)property.enumValueFlag;
            dropdown.SetValueWithoutNotify(currentSelected);
            var enumProp = property.Copy();
            dropdown.AddToClassList("unity-base-field__aligned");

            dropdown.RegisterValueChangedCallback(evt =>
            {
                if (RoleChoiceIsAllowed(evt.newValue, evt.previousValue, enumProp.serializedObject.targetObject as ScenarioConfig))
                {
                    enumProp.intValue = (int)evt.newValue;
                    enumProp.serializedObject.ApplyModifiedProperties();

                    return;
                }

                dropdown.SetValueWithoutNotify(evt.previousValue);
            });
            return dropdown;
#else
            return null;
#endif
        }

        private static VisualElement CreateTagsField(SerializedProperty property)
        {
            property = property.FindPropertyRelative("m_PlayerTag");
            List<string> choices = ProjectDataStore.GetMain().GetAllPlayerTags().ToList();
            choices.Insert(0, "None");
            var tagsField = new PopupField<String>() { label = "Tag", choices = choices };
            tagsField.SetValueWithoutNotify(property.stringValue == "" ? "None" : property.stringValue);
            var tagProp = property.Copy();
            tagsField.AddToClassList("unity-base-field__aligned");
            tagsField.tooltip =
                "Currently only one tag is supported per editor instance. To add a tag to the list of tags go to Project Settings->Multiplayer->Playmode, " +
                "then select the tag from the dropdown menu.";
            tagsField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "None")
                {
                    if (evt.newValue != evt.previousValue)
                    {
                        tagProp.stringValue = "";
                        tagProp.serializedObject.ApplyModifiedProperties();
                    }
                    return;
                }
                tagProp.stringValue = evt.newValue;
                tagProp.serializedObject.ApplyModifiedProperties();
            });

            return tagsField;
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        static bool RoleChoiceIsAllowed(MultiplayerRoleFlags newRole, MultiplayerRoleFlags oldRole, ScenarioConfig config)
        {
            // if it was already a server, it's ok
            if (oldRole != MultiplayerRoleFlags.Client)
                return true;

            if (newRole != MultiplayerRoleFlags.Client)
            {
                if (config == null)
                    return false;

                var currentServerCount = config.EditorInstance.RoleMask != MultiplayerRoleFlags.Client ? 1 : 0;
                currentServerCount += config.VirtualEditorInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);
                currentServerCount += config.LocalInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);
                currentServerCount += config.RemoteInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);

                if (currentServerCount + 1 > ScenarioConfigEditor.MaxServerCount)
                {
                    EditorUtility.DisplayDialog("Info", $"You can only have {ScenarioConfigEditor.MaxServerCount} server instances", "Ok");
                    return false;
                }
            }

            return true;
        }
#endif
    }
}

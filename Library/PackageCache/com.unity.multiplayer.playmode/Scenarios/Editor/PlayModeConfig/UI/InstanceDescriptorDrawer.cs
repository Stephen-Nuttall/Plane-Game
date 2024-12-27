#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [CustomPropertyDrawer(typeof(InstanceDescription), true)]
    class InstanceDescriptionDrawer : PropertyDrawer
    {
        const string k_InstanceDrawerClass = "instance-drawer";
        const string k_WarnIconClass = "warn-icon";
        internal const string k_MultiplayerRolePopupName = "main-multiplayer-role-field";
        internal const string k_MultiplayerAdditionalRolePopupName = "additional-multiplayer-role-field";


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumerator = property.GetEnumerator();
            var container = new VisualElement();
            var instanceProp = property.Copy();
            container.AddToClassList(k_InstanceDrawerClass);

            container.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Remove", action =>
                {
                    instanceProp.serializedObject.Update();
                    var so = instanceProp.serializedObject;
                    instanceProp.DeleteCommand();
                    so.ApplyModifiedProperties();
                });

                evt.menu.AppendAction("Duplicate", action =>
                {
                    instanceProp.serializedObject.Update();
                    var so = instanceProp.serializedObject;
                    instanceProp.DuplicateCommand();

                    so.ApplyModifiedProperties();
                }, _ => ValidateDuplicationOption(_, instanceProp));
            }));

            while (enumerator.MoveNext())
            {
                // CorrespondingNodeID is a property with auto backing field. We don't want to show it in the UI.
                if (property.name == "<CorrespondingNodeId>k__BackingField")
                    continue;

                if (property.name == "Name")
                {
                    var nameField = new TextField();
                    if (property.propertyPath.Contains("Editor"))
                    {
                        var visualElement = nameField.Children().ToList()[0];
                        visualElement.enabledSelf = false;

                    }
                    nameField.AddToClassList("instance-name-field");
                    nameField.label = "Name";
                    nameField.AddToClassList("unity-base-field__aligned");
                    nameField.Bind(property.serializedObject);
                    nameField.BindProperty(property);
                    var nameProp = property.Copy();
                    nameField.RegisterValueChangedCallback(evt =>
                    {
                        var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                        ValidateInstanceName(warnIcon, evt.target as TextField);
                    });

                    var warnIcon = CreateWarnIcon("Please select a unique name.");
                    nameField.Insert(1, warnIcon);
                    container.Add(nameField);
                    continue;
                }

                if (property.name == "m_BuildProfile")
                {
                    var objectField = new ObjectField() { label = "Build Profile", objectType = typeof(BuildProfile) };
                    objectField.allowSceneObjects = false;
                    objectField.SetValueWithoutNotify(property.objectReferenceValue);
                    objectField.AddToClassList("unity-base-field__aligned");
                    var buildProfileProp = property.Copy();

#if UNITY_USE_MULTIPLAYER_ROLES
                    var roleField = new TextField("Multiplayer Role") { isReadOnly = true, enabledSelf = false, focusable = false };
                    roleField.tooltip = "The multiplayer role can be modified in the build profile window or from the build profile asset.";
                    roleField.AddToClassList("unity-base-field__aligned");
#endif

                    objectField.RegisterValueChangedCallback(evt =>
                    {
                        buildProfileProp.objectReferenceValue = evt.newValue;
                        buildProfileProp.serializedObject.ApplyModifiedProperties();
                        var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                        SetBuildProfileWarnIcon(warnIcon, evt.newValue as BuildProfile, instanceProp);

                    });

#if UNITY_USE_MULTIPLAYER_ROLES
                    // Todo remove this hack, as soon as we can track the serialized object of BuildProfile
                    //https://unity.slack.com/archives/C06KY1VAH63/p1717485694720109
                    objectField.schedule.Execute(() =>
                    {
                        SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);
                    }).Every(1000);
#endif

                    var buildProfileButton = new Button(() => EditorApplication.ExecuteMenuItem("File/Build Profiles")) { name = "build-profile-button", text = "Open Build Profiles" };
                    objectField.Add(buildProfileButton);

                    var warnIcon = CreateWarnIcon("Build Profile is not supported, please check File - BuildProfiles for more info.");
                    objectField.Insert(1, warnIcon);
                    SetBuildProfileWarnIcon(warnIcon, buildProfileProp.objectReferenceValue as BuildProfile, instanceProp);

                    container.Add(objectField);
#if UNITY_USE_MULTIPLAYER_ROLES
                    container.Add(roleField);
                    SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);
#endif
                    continue;
                }

                if (property.name.Contains("m_PlayerTag"))
                {
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
                    container.Add(tagsField);
                    continue;
                }

#if UNITY_USE_MULTIPLAYER_ROLES
                // not needed anymore but keep it because maybe we reenable it.
                if (property.name.Contains("m_Role"))
                {
                    var dropdown = new PopupField<MultiplayerRoleFlags>() { label = "Multiplayer Role" , name = k_MultiplayerAdditionalRolePopupName};

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
                    container.Add(dropdown);
                    continue;
                }
#endif

                var propField = new PropertyField(property);
                propField.RegisterValueChangeCallback(evt => { });
                propField.Bind(property.serializedObject);
                container.Add(propField);
                if (property.name == "advancedConfiguration" || property.name == "m_AdvancedConfiguration")
                {
                    break;
                }
            }

            return container;
        }

        static Image CreateWarnIcon(string tooltip)
        {
            var warnIcon = new Image();
            warnIcon.AddToClassList("propertyfield-icon");
            warnIcon.AddToClassList(k_WarnIconClass);
            warnIcon.image = Icons.GetImage(Icons.ImageName.Warning);
            warnIcon.tooltip = tooltip;
            return warnIcon;
        }

        DropdownMenuAction.Status ValidateDuplicationOption(DropdownMenuAction arg, SerializedProperty prop)
        {
            var playerInstances = prop.serializedObject.FindProperty("m_EditorInstances");
            if (prop.boxedValue.GetType() == typeof(EditorInstanceDescription) && playerInstances.arraySize >= ScenarioConfigEditor.MaxEditorInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var localInstances = prop.serializedObject.FindProperty("m_LocalInstances");
            if (prop.boxedValue.GetType() == typeof(LocalInstanceDescription) && localInstances.arraySize >= ScenarioConfigEditor.MaxLocalInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var remoteInstances = prop.serializedObject.FindProperty("m_RemoteInstances");
            if (prop.boxedValue.GetType() == typeof(RemoteInstanceDescription) && remoteInstances.arraySize >= ScenarioConfigEditor.MaxServerCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            return DropdownMenuAction.Status.Normal;
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        void SetRoleDisplay(TextField field, BuildProfile profile)
        {
            field.style.display = profile == null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            if (profile == null)
                return;

            field.value = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(profile).ToString();
        }
#endif

        void SetBuildProfileWarnIcon(Image warnIcon, BuildProfile newProfile, SerializedProperty so)
        {
            if (newProfile == null)
                return;
            var isAllowed = InternalUtilities.IsBuildProfileSupported(newProfile);
            if (!isAllowed)
                warnIcon.tooltip = "BuildProfile is not supported or installed.";

            var canRun = true;
            if (so.boxedValue is LocalInstanceDescription)
            {
                canRun = InternalUtilities.BuildProfileCanRunOnCurrentPlatform(newProfile);
                if (!canRun)
                    warnIcon.tooltip = "BuildProfile cannot be run as local instance";
            }

            warnIcon.style.display = isAllowed && canRun ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        void ValidateInstanceName(Image warnIcon, TextField nameField)
        {
            var textfields = nameField.panel.visualTree.Query<TextField>(className: "instance-name-field").ToList();

            List<string> takenNames = new List<string>();
            textfields.ForEach(textField =>
            {
                var warningIcon = textField.Q<Image>();
                if (takenNames.Contains(textField.value))
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                }
                else
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                    takenNames.Add(textField.value);
                }
            });
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

    [CustomPropertyDrawer(typeof(RemoteInstanceDescription.AdvancedConfig), true)]
    class AdvancedConfigDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new Foldout();
            container.AddToClassList("unity-base-field__aligned");

            container.text = "Advanced Configuration";
            var instanceProp = property.Copy();
            var enumerator = property.GetEnumerator();

            container.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Remove", action =>
                {
                    instanceProp.serializedObject.Update();
                    var so = instanceProp.serializedObject;
                    instanceProp.DeleteCommand();
                    so.ApplyModifiedProperties();
                });
            }));

            while (enumerator.MoveNext())
            {
                if (property.name == "m_Identifier")
                {
                    var nameField = new PropertyField(property);
                    var remoteNameField = new TextField() { label = "Name In Unity Cloud Dashboard", tooltip = RemoteInstanceDescription.k_ServerNameTooltip };

                    nameField.RegisterValueChangeCallback(evt => remoteNameField.value = RemoteInstanceDescription.ComputeMultiplayName(evt.changedProperty.stringValue));
                    nameField.Bind(property.serializedObject);
                    remoteNameField.SetEnabled(false);

                    container.Add(nameField);
                    container.Add(remoteNameField);
                    continue;
                }

                if (property.name == "m_InstanceAmountOfMemoryMB")
                {
                    var slider = UIFactory.CreateStepSlider(property, "Instance Memory", 100, 7500);
                    container.Add(slider);
                    continue;
                }

                if (property.name == "m_InstanceCpuFrequencyMHz")
                {
                    var slider = UIFactory.CreateStepSlider(property, "Instance CPU Clockspeed", 100, 5500);
                    container.Add(slider);
                    continue;
                }

                if (property.name == "m_FleetRegion")
                {
                    // Todo: hook into GetAvailableFleetRegions() when test/multiplay-api lands
                    var choices = new List<String>() { "North America", "Europe", "Asia", "Australia" };
                    var dropDown = new DropdownField("Fleet Region", choices, 0);
                    dropDown.tooltip = property.tooltip;
                    dropDown.BindProperty(property);
                    dropDown.Bind(property.serializedObject);
                    container.Add(dropDown);
                    continue;
                }

                if (property.name == "m_Arguments")
                {
                    var field = UIFactory.CreateTextfieldWithDefault(property, "Arguments");
                    var linkIcon = new Image();
                    linkIcon.AddToClassList("propertyfield-icon");
                    linkIcon.AddToClassList("link-icon");
                    linkIcon.image = Icons.GetImage(Icons.ImageName.Help);
                    linkIcon.RegisterCallback<ClickEvent>(evt =>
                    {
                        Application.OpenURL("https://docs.unity.com/ugs/manual/game-server-hosting/manual/concepts/launch-parameters");
                    });
                    field.Insert(1, linkIcon);
                    container.Add(field);
                    continue;
                }


                var propField = new PropertyField(property);
                propField.RegisterValueChangeCallback(evt => { });
                propField.Bind(property.serializedObject);
                container.Add(propField);

            }

            foreach (var child in container.Children())
            {
                child.AddToClassList("unity-base-field__aligned");
            }

            return container;
        }
    }
}

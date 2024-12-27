using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Threading;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Custom Editor for ScenarioConfigurations. Used inside PlayModeScenariosWindow.
    /// </summary>
    [CustomEditor(typeof(ScenarioConfig))]
    class ScenarioConfigEditor : Editor
    {
        internal const string k_StylePath = "Scenarios/Editor/PlayModeConfig/UI/ScenarioConfigEditor.uss";
        internal const string k_LocalInstanceListName = "local-instance-list";
        internal const string k_RemoteInstanceListName = "remote-instance-list";
        internal const string k_EditorInstancesContainerName = "editor-instances-container";
        internal const string k_RemoteInstancesFoldoutName = "remote-instances-foldout";
        internal const string k_InstallMissingPackagesButtonName = "install-missing-packages-button";

        internal const string k_VirtualEditorInstanceFoldoutName = "virtual-editor-instance-foldout";

        internal const int MaxServerCount = 1;
        internal const int MaxEditorInstanceCount = 3;
        internal const int MaxLocalInstanceCount = 4;

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            UIUtils.ApplyStyleSheet(k_StylePath, container);

            // Description Field
            var descriptionField = serializedObject.FindProperty("m_Description");
            var descriptionText = new TextField("Description");
            descriptionText.AddToClassList("unity-base-field__aligned");
            descriptionText.multiline = true;
            descriptionText.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            descriptionText.BindProperty(descriptionField);
            descriptionText.Bind(serializedObject);
            container.Add(descriptionText);

            container.Add(CreateEditorInstancesElement());
            container.Add(CreateLocalInstancesElement());
            container.Add(CreateRemoteInstanceElement());

            return container;
        }

        Foldout CreateMissingPackageHelpbox(List<string> missingPacks)
        {
            var foldout = new Foldout() { text = "Remote Instances", name = k_RemoteInstancesFoldoutName };
            foldout.AddToClassList("missing-packages-foldout");

            var helpBox = new HelpBox("Remote server will be deployed to Unity Game Server Hosting. Make sure you have the necessary packages installed.\n\t" +
                string.Join("\n\t", missingPacks)
                , HelpBoxMessageType.Info);
            var dashboardButton = new Button(() => Application.OpenURL("https://cloud.unity.com/")) { text = "Open Dashboard" };
            var installPackages = new Button(() =>
            {
                var request = Client.AddAndRemove(missingPacks.ToArray());
                while (!request.IsCompleted)
                {
                    Thread.Sleep(100);
                    Thread.Yield();
                }
                if (request.Error != null)
                    Debug.LogError($"Failed to install packages: {request.Error.message}");
            })
            {
                name = k_InstallMissingPackagesButtonName,
                text = "Install missing packages"
            };

            var buttonContainer = new VisualElement() { name = "missing-packages-button-container" };
            buttonContainer.Add(dashboardButton);
            buttonContainer.Add(installPackages);
            helpBox.Add(buttonContainer);
            foldout.Add(helpBox);
            return foldout;
        }

        // We have to override the default behaviour of the list view, because we need some custom logic in it.
        void SetupListView(ListView listView, SerializedProperty listProperty, Type instanceType, int maxInstanceCount)
        {
            if (listView == null)
            {
                return;
            }

            // listView.showFoldoutHeader = false;
            listView.showBoundCollectionSize = false;
            listView.reorderable = false;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            listView.onAdd = _ =>
            {
                listProperty.serializedObject.Update();

                if (listProperty.arraySize >= maxInstanceCount)
                {
                    EditorUtility.DisplayDialog("Warning", $"You can't have more than {maxInstanceCount} instances", "Ok");
                    return;
                }

                listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
                var instanceProperty = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                var instance = Activator.CreateInstance(instanceType) as InstanceDescription;
                instance.Name = GenerateInstanceName(listProperty);
                instanceProperty.boxedValue = instance;
                listProperty.serializedObject.ApplyModifiedProperties();
                listProperty.serializedObject.Update();
            };
        }

        // Mimic the naming that is used in Unity.
        static string GenerateInstanceName(SerializedProperty instanceArrayProperty)
        {
            var instanceList = new List<InstanceDescription>();
            for (var i = 0; i < instanceArrayProperty.arraySize; i++)
            {
                if (instanceArrayProperty.GetArrayElementAtIndex(i).boxedValue != null && instanceArrayProperty.GetArrayElementAtIndex(i).boxedValue is InstanceDescription instanceDescription)
                    instanceList.Add(instanceDescription);
            }

            var configName = "Instance";
            var counter = 1;
            while (instanceList.Any(c => c.Name == configName))
            {
                configName = "Instance" + $"({counter})";
                counter++;
            }

            return configName;
        }

        /// <summary>
        /// Hides the name field of the main Editor instance.
        /// and adds a label to the top of the editor.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        VisualElement CreateMainEditorInstance(SerializedProperty property)
        {
            var container = new VisualElement();
            container.name = "main-editor-instance";
            container.AddToClassList("instances-group");

            InstanceDescriptionDrawer mainDescriptionDrawer = new InstanceDescriptionDrawer();
            var inspector = mainDescriptionDrawer.CreatePropertyGUI(property);
            inspector.name = "main-editor-content";
            var nameField = inspector.Q<TextField>(className: "instance-name-field");
            nameField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            // Initial scene
            var initialSceneField = serializedObject.FindProperty("m_InitialScene");
            var initialSceneUI = new PropertyField(initialSceneField) { name = "initial-scene" };
            initialSceneUI.AddToClassList("unity-base-field__aligned");
            inspector.Add(initialSceneUI);

            container.Add(inspector);

            return container;
        }

        private VisualElement CreateEditorInstancesElement()
        {
            var container = new VisualElement();
            container.AddToClassList("instances-group");
            var enableEditorsProperty = serializedObject.FindProperty("m_EnableEditors");

            var editorsToggle = new Toggle("Editor") { name = "EditorInstancesToggle" };
            container.Add(editorsToggle);
            editorsToggle.BindProperty(enableEditorsProperty);

            var content = new VisualElement() { name = k_VirtualEditorInstanceFoldoutName };
            content.AddToClassList("unity-foldout__content");
            container.Add(content);

            editorsToggle.RegisterValueChangedCallback(evt =>
            {
                content.style.display = evt.newValue
                    ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                    : new StyleEnum<DisplayStyle>(DisplayStyle.None);
            });

            // Main editor instance
            var multiplayerPlaymodeProperty = serializedObject.FindProperty("m_MainEditorInstance");
            var mainEditorField = new PropertyField(multiplayerPlaymodeProperty);
            content.Add(mainEditorField);

            // Editor Instances List
            var editorInstancesProperty = serializedObject.FindProperty("m_EditorInstances");
            var additionalEditors = new PropertyField(editorInstancesProperty) { label = "Additional Editor Instances", name = k_EditorInstancesContainerName };
            additionalEditors.AddToClassList("instances-group");

            //Todo: Found no better way for now, AttachToPanelEvent is not working.
            additionalEditors.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var listView = additionalEditors.Q<ListView>();

                // Use the reorderable flag to check if the setup already happened.
                if (listView == null || listView.reorderable == false)
                    return;

                SetupListView(listView, serializedObject.FindProperty("m_EditorInstances"), typeof(VirtualEditorInstanceDescription), MaxEditorInstanceCount);
            });

            content.Add(additionalEditors);
            return container;
        }

        private VisualElement CreateLocalInstancesElement()
        {
            var container = new VisualElement();
            container.AddToClassList("instances-group");

            var localInstancesProperty = serializedObject.FindProperty("m_LocalInstances");
            var localInstanceUI = new PropertyField(localInstancesProperty) { name = k_LocalInstanceListName };

            localInstanceUI.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var listView = localInstanceUI.Q<ListView>();

                // Use the reorderable flag to check if the setup already happened.
                if (listView == null || listView.reorderable == false)
                    return;
                SetupListView(listView, serializedObject.FindProperty("m_LocalInstances"), typeof(LocalInstanceDescription), MaxLocalInstanceCount);
            });

            container.Add(localInstanceUI);
            return container;
        }

        private VisualElement CreateRemoteInstanceElement()
        {
            var container = new VisualElement();
            container.AddToClassList("instances-group");
            var remoteInstancesProperty = serializedObject.FindProperty("m_RemoteInstances");

            if (!ScenarioConfig.PackagesForRemoteDeployInstalled(out var missingPacks))
            {
                container.Add(CreateMissingPackageHelpbox(missingPacks));
                return container;
            }

            var remoteInstancesUI = new PropertyField(remoteInstancesProperty) { name = k_RemoteInstanceListName };
            remoteInstancesUI.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var listView = remoteInstancesUI.Q<ListView>();

                // Use the reorderable flag to check if the setup already happened.
                if (listView == null || listView.reorderable == false)
                    return;
                SetupListView(listView, remoteInstancesProperty, typeof(RemoteInstanceDescription), MaxServerCount);
            });

            container.Add(remoteInstancesUI);
            return container;
        }
    }
}

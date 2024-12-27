using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    /// <summary>
    /// Listview to render all available configurations
    /// </summary>
    class PlayModeListView : VisualElement
    {
        ListView m_ListView;
        internal Action<PlayModeConfig> OnConfigSelected;
        readonly LabelWithIcon m_NewItemTextField;

        private const string k_Stylesheet = "Configurations/Editor/GUI/PlayModeScenariosWindow/PlayModeListView.uss";

        internal PlayModeConfig ConfigurationToEdit => m_ListView.selectedItem as PlayModeConfig;

        internal PlayModeListView()
        {
            name = "playmode-list-view";

            PlayModeConfigUtils.ConfigurationAddedOrRemoved -= RefreshList;
            PlayModeConfigUtils.ConfigurationAddedOrRemoved += RefreshList;

            m_ListView = new ListView { fixedItemHeight = 16, selectionType = SelectionType.Single };
            m_ListView.selectionChanged += OnItemSelected;

            m_NewItemTextField = LabelWithIcon.Create("PlaymodeConfig", "");
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_NewItemTextField.name = "add-new-item-textfield";

            m_NewItemTextField.OnFinishEdit += s =>
            {
                m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                PlayModeConfigUtils.CreatePlayModeConfig(s, m_NewItemTextField.userData as Type);
            };

            m_NewItemTextField.OnEdit += s =>
            {
                m_NewItemTextField.InputIsValid = PlayModeConfigUtils.GetAllConfigs().All(c => c.name != s);
            };

            m_NewItemTextField.OnCancel += () => m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            Add(m_NewItemTextField);
            Add(m_ListView);
            RefreshList();
            UIUtils.ApplyStyleSheetAsync(k_Stylesheet, this).Forget();
        }

        void OnItemSelected(IEnumerable<object> selection)
        {
            OnConfigSelected?.Invoke((PlayModeConfig)selection.FirstOrDefault());
        }

        void RefreshList()
        {
            m_ListView.itemsSource = PlayModeConfigUtils.GetAllConfigs();
            m_ListView.Rebuild();
            m_ListView.makeItem = () =>
            {
                var element = new LabelWithIcon("", "", "_Popup");
                element.AddManipulator(InstanceListItemContextMenu(element));
                element.AddToClassList("playmode-list-item");
                element.OnFinishEdit += s =>
                {
                    var path = AssetDatabase.GetAssetPath(element.userData as PlayModeConfig);
                    AssetDatabase.RenameAsset(path, s);
                    this.schedule.Execute(() => RefreshList());
                };

                element.OnEdit += s =>
                {
                    var allowRename = PlayModeConfigUtils.GetAllConfigs().All(c => c.name != s);
                    element.InputIsValid = true;
                };

                return element;
            };

            m_ListView.bindItem = (element, i) =>
            {
                var labelAndIcon = (LabelWithIcon)element;
                var config = PlayModeConfigUtils.GetAllConfigs()[i];
                SerializedObject so = new SerializedObject(config);
                labelAndIcon.Unbind();
                labelAndIcon.TrackSerializedObjectValue(so, _ =>
                {
                    var configIsValid = config.IsConfigurationValid(out var tooltipText);
                    labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                });
                var configIsValid = config.IsConfigurationValid(out var tooltipText);
                labelAndIcon.ShowWarningIcon(!configIsValid, tooltipText);
                labelAndIcon.SetIcon(config.Icon);
                labelAndIcon.Text = config.name;
                labelAndIcon.userData = config;
                labelAndIcon.tooltip = config.Description;
            };


            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig;


            // Select the config that is currently selected if not available (because maybe it just got deleted) select first
            var allConfigs = PlayModeConfigUtils.GetAllConfigs();
            if (currentConfig != null)
                m_ListView.selectedIndex = allConfigs.IndexOf(currentConfig);

            var selectedConfig = allConfigs.ElementAtOrDefault(m_ListView.selectedIndex);

            // Null is allowed here, it means that the we will show the not seleccted view.
            OnConfigSelected?.Invoke(selectedConfig);
        }

        ContextualMenuManipulator InstanceListItemContextMenu(LabelWithIcon element)
        {
            return new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", _ =>
                {
                    var config = element.userData as PlayModeConfig;

                    var delete = EditorUtility.DisplayDialog($"{config.name}", $"Do you really want to delete {config.name}", "Delete", "Cancel");

                    if (delete)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(config));
                    }
                });

                evt.menu.AppendAction("Rename", _ => element.EnableEditMode());

                evt.menu.AppendAction("Duplicate", _ => PlayModeConfigUtils.CopyPlayModeConfiguration(element.userData as PlayModeConfig));
            });
        }

        internal void ShowAddTextField(Type type, string newItemName)
        {
            var finalName = newItemName;
            int counter = 1;
            while (PlayModeConfigUtils.GetAllConfigs().Any(c => c.name == finalName))
            {
                finalName = newItemName + $"({counter})";
                counter++;
            }

            m_NewItemTextField.Text = finalName;
            m_NewItemTextField.userData = type;
            m_NewItemTextField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            m_NewItemTextField.EnableEditMode();
        }
    }
}

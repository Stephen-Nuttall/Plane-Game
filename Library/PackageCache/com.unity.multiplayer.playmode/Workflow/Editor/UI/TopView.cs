using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using PopupWindow = UnityEditor.PopupWindow;
#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
#endif

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class TopViewPermanence
    {
        public const string k_FrameAfterTopViewSwitch = "frameAfterTopViewSwitch";
#if UNITY_USE_MULTIPLAYER_ROLES
        public const string k_EnableMultiplayerRoles = nameof(k_EnableMultiplayerRoles);
#endif

        public static readonly bool Initialized; // Note: the constructor won't run without some data being in the static class
        public static Action EnableMultiplayerRolesEvent;

        static TopViewPermanence()
        {
            Initialized = true;
#if UNITY_USE_MULTIPLAYER_ROLES
            SessionState.SetBool(k_EnableMultiplayerRoles, EditorMultiplayerRolesManager.EnableMultiplayerRoles);
#endif
            EditorApplication.update += () =>
            {
#if UNITY_USE_MULTIPLAYER_ROLES
                if (SessionState.GetBool(k_EnableMultiplayerRoles, false) != EditorMultiplayerRolesManager.EnableMultiplayerRoles)
                {
                    SessionState.SetBool(k_EnableMultiplayerRoles, EditorMultiplayerRolesManager.EnableMultiplayerRoles);
                    EnableMultiplayerRolesEvent?.Invoke();
                }
#endif
                // The window can't fully close with TopView having a callback.
                // So we come here to make sure we can fully close the window
                if (SessionState.GetBool(k_FrameAfterTopViewSwitch, false))
                {
                    SessionState.SetBool(k_FrameAfterTopViewSwitch, false);
                    Debug.Assert(TopView.NumberOfTopViews == 0 || TopView.NumberOfTopViews == 1, $"An editor should only have 0 or 1 TopView. [{TopView.NumberOfTopViews}]");
                    var editorMode = EditorApplication.isPlaying
                        ? CloneDataFile.LoadFromFile(VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile).PlayModeLayoutFlags
                        : CloneDataFile.LoadFromFile(VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile).EditModeLayoutFlags;
                    ModeSwitcher.SwitchToView(editorMode);
                }
            };
        }
    }

    class TopView : EditorWindow
    {
        class MarksApplied
        {
            public WindowLayoutCheckboxes.Marks Marks;
        }

        MarksApplied m_MarksFromOpening;
        public static int NumberOfTopViews;
#if UNITY_USE_MULTIPLAYER_ROLES
        static MultiplayerRolesToolbarExtensions.MultiplayerRolesToolbarDropdown s_ToolbarButton;
#endif

        public void OnDestroy()
        {
            NumberOfTopViews--;
        }

        public void CreateGUI()
        {
            Debug.Assert(TopViewPermanence.Initialized, "The TopView did not initialize correctly.");
            NumberOfTopViews++;
            if (NumberOfTopViews > 1)
            {
                Debug.LogWarning("An editor should only have 0 or 1 TopView so something bad happened");
                return;
            }

            var windowLayoutPopup = new WindowLayoutPopoutWindow();
            var toolbar = new Toolbar();

#if UNITY_USE_MULTIPLAYER_ROLES
            AddMultiplayerRoleDropdown(toolbar);
#endif
            var layoutDropdown = new EditorToolbarDropdown
            {
                icon = GetLayoutIcon(),
                text = "Layout",
            };
            toolbar.Add(layoutDropdown);

            // Apply styles for the entire TopView to more match the standard editor
            rootVisualElement.AddToClassList("unity-editor-toolbar-container");
            toolbar.AddToClassList("unity-editor-toolbar-container__zone");
            toolbar.style.height = 30f;
            toolbar.style.borderBottomWidth = 0f;
            LoadStyleSheets("MainToolbar", rootVisualElement);
            LoadStyleSheets("EditorToolbar", toolbar);

            rootVisualElement.Add(toolbar);
            rootVisualElement.style.flexDirection = FlexDirection.RowReverse;
            rootVisualElement.style.maxHeight = 30f;
            m_MarksFromOpening = new MarksApplied
            {
                Marks = new WindowLayoutCheckboxes.Marks { Console = true }
            }; // Have to have at least one marked since you must have a layout

#if UNITY_USE_MULTIPLAYER_ROLES
            TopViewPermanence.EnableMultiplayerRolesEvent += () =>
            {
                if (s_ToolbarButton != null)
                {
                    // Update the active appearance
                    s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerRolesManager.EnableMultiplayerRoles
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            };
            EditorMultiplayerRolesManager.EnableMultiplayerRolesChanged += () =>
            {
                if (s_ToolbarButton != null)
                {
                    // Update the active appearance
                    s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerRolesManager.EnableMultiplayerRoles
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            };
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleChanged += () =>
            {
                if (s_ToolbarButton != null)
                {
                    s_ToolbarButton.EditorToolbarDropdown.icon = IconPerRole(EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask);
                }
            };
#endif
            EditorApplication.playModeStateChanged += state =>
            {
#if UNITY_USE_MULTIPLAYER_ROLES
                if (s_ToolbarButton != null)
                {
                    s_ToolbarButton.EditorToolbarDropdown.SetEnabled(state != PlayModeStateChange.EnteredPlayMode);
                }
#endif
                windowLayoutPopup.CheckBoxes.RefreshLayout();
            };
            layoutDropdown.RegisterCallback<ClickEvent>(_ =>
            {
                WindowLayoutCheckboxes.ListenToCheckBoxes(windowLayoutPopup.CheckBoxes);
                PopupWindow.Show(layoutDropdown.worldBound, windowLayoutPopup);
            });
            windowLayoutPopup.CloseEvent += () =>
            {
                if (m_MarksFromOpening != null)
                {
                    windowLayoutPopup.CheckBoxes.FromMarksWithoutNotify(m_MarksFromOpening.Marks);
                }

                m_MarksFromOpening = null;
            };
            windowLayoutPopup.OpenEvent += () =>
            {
                m_MarksFromOpening = new MarksApplied
                { Marks = WindowLayoutCheckboxes.Marks.FromWindowLayoutCheckboxes(windowLayoutPopup.CheckBoxes) };

                windowLayoutPopup.CheckBoxes.ApplyEvent += marks =>
                {
                    m_MarksFromOpening = null;
                    // On UI event update the local state
                    var cloneConfiguration = VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile;
                    var cloneData = cloneConfiguration.Data;
                    if (EditorApplication.isPlaying)
                    {
                        cloneData.PlayModeLayoutFlags = LayoutFlags.None;
#if UNITY_USE_NETCODE_FOR_ENTITIES
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.MultiplayerPlayModeWindow, marks.PlaymodeTools);
#endif
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.GameView, marks.Game);
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.ConsoleWindow, marks.Console);
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.InspectorWindow, marks.Inspector);
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.SceneHierarchyWindow, marks.Hierarchy);
                        cloneData.PlayModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.PlayModeLayoutFlags,
                            LayoutFlags.SceneView, marks.Scene);
                    }
                    else
                    {
                        cloneData.EditModeLayoutFlags = LayoutFlags.None;
#if UNITY_USE_NETCODE_FOR_ENTITIES
                        cloneData.EditModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.EditModeLayoutFlags,
                            LayoutFlags.MultiplayerPlayModeWindow, marks.PlaymodeTools);
#endif
                        cloneData.EditModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.EditModeLayoutFlags,
                            LayoutFlags.GameView, marks.Game);
                        cloneData.EditModeLayoutFlags = LayoutFlagsUtil.SetFlag(cloneData.EditModeLayoutFlags,
                            LayoutFlags.ConsoleWindow, marks.Console);
                    }

                    // Update and save to disk
                    cloneConfiguration.Data = cloneData;
                    CloneDataFile.SaveToFile(cloneConfiguration);

                    // Use local state to update editor mode
                    var editorMode = EditorApplication.isPlaying
                        ? cloneData.PlayModeLayoutFlags
                        : cloneData.EditModeLayoutFlags;


                    if (!ModeSwitcher.IsView(editorMode))
                    {
#if UNITY_2023_2_OR_NEWER
                        ModeSwitcher.SaveCurrentView();
                        // Close the existing window if its open since we are about to reopen once we switch to view
                        // NOTE: We close the window AFTER saving the window layout!
                        ModeSwitcher.CloseCurrentWindow();
                        focusedWindow?.Close();  // Close the focused window so it doesn't appear to be open after we switch layouts
                        SessionState.SetBool(TopViewPermanence.k_FrameAfterTopViewSwitch, true);
#endif
                    }
                };
            };
        }

        static Texture2D GetLayoutIcon()
        {
            var layoutIconPath = EditorGUIUtility.isProSkin ? $"{UXMLPaths.UXMLWorkflowUIRoot}/UI/d_layout@2x.png" : $"{UXMLPaths.UXMLWorkflowUIRoot}/UI/layout@2x.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(layoutIconPath);
            return new GUIContent("Layout", texture).image as Texture2D;
        }

        static void LoadStyleSheets(string name, VisualElement target)
        {
            const string k_StyleSheetsPath = "StyleSheets/Toolbars/";

            var path = k_StyleSheetsPath + name;

            var common = EditorGUIUtility.Load($"{path}Common.uss") as StyleSheet;
            if (common != null)
                target.styleSheets.Add(common);

            var themeSpecificName = EditorGUIUtility.isProSkin ? "Dark" : "Light";
            var themeSpecific = EditorGUIUtility.Load($"{path}{themeSpecificName}.uss") as StyleSheet;
            if (themeSpecific != null)
                target.styleSheets.Add(themeSpecific);
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        static void OnDropDownMenuClick(MultiplayerRoleFlags dropdownEntryRole)
        {
            // Update the API
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask = dropdownEntryRole;
            // Update the file on disk that tracks each players current role
            var dataStore = SystemDataStore.GetClone(); // get latest data store
            foreach (var playerStateJson in UnityPlayer.GetPlayers(dataStore))
            {
                if (playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier == VirtualProjectsEditor.CloneIdentifier)
                {
                    Debug.Assert(playerStateJson.Type == PlayerType.Clone);
                    playerStateJson.MultiplayerRole = (int)dropdownEntryRole;
                    dataStore.SavePlayerJson(playerStateJson.Index, playerStateJson);  // updates file on disk
                    break;
                }
            }
        }

        static bool OnIsCheckMarked(MultiplayerRoleFlags role)
        {
            return role == EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
        }

        static Texture2D IconPerRole(MultiplayerRoleFlags role)
        {
            return EditorGUIUtility.IconContent(MultiplayerRoleField.GetIconForRoleFlags(role)).image as Texture2D;
        }

        static void AddMultiplayerRoleDropdown(Toolbar toolbar)
        {
            var dropDownInfo = new MultiplayerRolesToolbarExtensions.DropDownInfo
            {
                MapIconToRole = IconPerRole,
                OnIsCheckMarked = OnIsCheckMarked,
                OnHandleClick = OnDropDownMenuClick,
            };
            var initialRole = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
            s_ToolbarButton = MultiplayerRolesToolbarExtensions.CreateDropdown(initialRole, dropDownInfo);
            // Update the active appearance
            s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerRolesManager.EnableMultiplayerRoles
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            // Add to the toolbar
            toolbar.Add(s_ToolbarButton.EditorToolbarDropdown);

            // Note: Clicking the buttons will update the role and the new value is read in
            // the method StandardCloneWorkflow::Initialize()
        }
#endif
    }
}

using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Common.Runtime;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class StandardMainEditorWorkflow
    {
        static string s_LastWriteTime;
        bool m_QueuedToBroadcastPlay;

        /*
         * The main editor keeps its functionality of starting on the scene tab and switching to game view when going into play mode (if OpenGameViewOnPlay is active)
         * However, the clones always run with which ever view was selected from layout (since OpenGameViewOnPlay is always off)
         *
         * Edges cases:
         * This is currently covering the main editor crashing (it uses the non deleted key in the local data store)
         * The clone crashing (it just always runs with OpenGameViewOnPlay off)
         * As well as these crashes happening in either order (main should be able to crash first or last and etc...)
         *
         * See CloneDisablePlayOnGameView for clone behaviour
         */
        static bool OpenWindowOnEnteringPlayMode
        {
            get => EditorPrefs.GetBool(k_OpenGameViewOnPlay, true);
            set
            {
                if (EditorPrefs.GetBool(k_OpenGameViewOnPlay) != value)
                {
                    EditorPrefs.SetBool(k_OpenGameViewOnPlay, value);
                }
            }
        }

        // The following string and property needs to match the functionality
        //  of PlayModeView.openWindowOnEnteringPlayMode currently
        const string k_OpenGameViewOnPlay = "OpenGameViewOnEnteringPlayMode";
        const string k_OpenGameViewOnPlayCache = "OpenGameViewOnEnteringPlayModeCache";

        public void Initialize(WorkflowMainEditorContext mppmContext, MainEditorContext vpContext)
        {
            // If the editor closed unexpectedly we pull the value
            // that was in memory before we went into playmode
            // in case the clones have altered it
            if (EditorPrefs.HasKey(k_OpenGameViewOnPlayCache))
            {
                OpenWindowOnEnteringPlayMode = EditorPrefs.GetBool(k_OpenGameViewOnPlayCache);
                EditorPrefs.DeleteKey(k_OpenGameViewOnPlayCache);
            }

#if UNITY_USE_MULTIPLAYER_ROLES
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleChanged += () =>
            {
                // Based on other system actually updating the role, we update the JSON, which is the authority of the role in MPPM
                // This is just for this editors role
                foreach (var player in UnityPlayer.GetPlayers(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore))
                {
                    if (player.Type == PlayerType.Main)
                    {
                        var role = (int)EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
                        if (role != player.MultiplayerRole)
                        {
                            player.MultiplayerRole = role;
                            VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.SavePlayerJson(player.Index, player);  // updates file on disk
                            break;
                        }
                    }
                }
            };
#endif

            mppmContext.MainPlayerSystems.ApplicationEvents.SceneChanged += (_, _) =>
            {
                if (EditorApplication.isPaused)
                {
                    vpContext.MessagingService.Broadcast(new PauseMessage());
                }
            };
            mppmContext.MainPlayerSystems.ApplicationEvents.LogCountsChanged += (identifier, logCounts) =>
            {
                if (!mppmContext.LogsRepository.ContainsKey(identifier))
                {
                    mppmContext.LogsRepository.Create(identifier, new BoxedLogCounts { LogCounts = logCounts, });
                }
                else
                {
                    mppmContext.LogsRepository.Update(identifier, playerLogs => { playerLogs.LogCounts = logCounts; }, out _);
                }
            };

            mppmContext.MainPlayerSystems.ApplicationEvents.UIPollUpdate += () =>
            {
                /*****
                 * Keep the MPPM local cache of the state of the clones up to date
                 * (for the UI and other systems)
                 */

#if UNITY_USE_MULTIPLAYER_ROLES
                if(!EditorApplication.isPlaying)
                {
                    if (s_LastWriteTime != SystemDataStore.GetMainLastWriteTime())
                    {
                        s_LastWriteTime = SystemDataStore.GetMainLastWriteTime();
                        ///////////////
                        // We load in a new data store (and not overwrite the object
                        // for now because we don't want to invalidate pointers.
                        // So we just read in the new values if they changed on the object that
                        // already exists in memory (across the whole system)
                        // see :UpdatedDataStore for what this effects
                        var updatedDataStore = SystemDataStore.GetMain();
                        foreach (var player in UnityPlayer.GetPlayers(mppmContext.SystemDataStore))
                        {
                            // Make sure we read and update *every* player
                            var newPlayers = updatedDataStore.LoadAllPlayerJson();
                            var newRole = newPlayers[player.Index].MultiplayerRole;
                            if (newRole != player.MultiplayerRole)
                            {
                                player.MultiplayerRole = newRole;
                                // We don't update file on disk. just read new values from disk
                            }
                            if (player.Type == PlayerType.Main)
                            {
                                EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask = (MultiplayerRoleFlags)newRole;
                            }
                        }
                    }
                }
#endif

                foreach (var player in UnityPlayer.GetPlayers(mppmContext.SystemDataStore))
                {
                    // If a player's editor clone has unexpectedly stopped, prompt for a restart.
                    if (player.Active
                     && player.Type == PlayerType.Clone
                     && player.TypeDependentPlayerInfo.VirtualProjectIdentifier != null
                     && vpContext.VirtualProjectsApi.TryGetFunc(player.TypeDependentPlayerInfo.VirtualProjectIdentifier, out var project, out _)
                     && project.EditorState == EditorState.UnexpectedlyStopped)
                    {
                        var choice = PlayerCrashModal.DisplayPlayerCrashModal(player.Name);
                        if (choice == PlayerCrashModal.Choices.Restart)
                        {
                            var hasProjectState = vpContext.StateRepository.TryGetValue(project.Identifier, out var statePerProcessLifetime);
                            var args = statePerProcessLifetime.LaunchArgs;
                            Debug.Assert(hasProjectState);
                            Debug.Assert(args.Length != 0, $"MPPM has its own arguments that should be in the {nameof(statePerProcessLifetime)}. We should be relaunching with those same arguments (before calling Close which deletes the arguments)");
                            project.Close(out _);   // NOTE: This will kill the state per life time
                            project.Launch(out _, out _, args);
                        }
                        else
                        {
                            project.Close(out _);   // NOTE: This will kill the state per life time
                            player.Active = false;
                            mppmContext.SystemDataStore.SavePlayerJson(player.Index, player);
                        }
                    }
                }

                // The messaging is failing because of the domain reload that happens when entering playmode
                // This is indicative of a deeper low level bug that we should fix in the future
                if (m_QueuedToBroadcastPlay)
                {
                    m_QueuedToBroadcastPlay = false;
                    vpContext.MessagingService.Broadcast(new PlayMessage());
                }
            };

            mppmContext.MainPlayerSystems.ApplicationEvents.PausedOnPlayer += () =>
            {
                if (!EditorApplication.isPaused)
                {
                    EditorApplication.isPaused = true;
                    vpContext.MessagingService.Broadcast(new PauseMessage());
                }
            };

            mppmContext.MainPlayerSystems.PlaymodeEvents.Play += () =>
            {
                if (IsAnyClonesOpen(mppmContext))
                {
                    if (IsAnyDirtySceneOpen(out var openSceneNames))
                    {
                        MppmLog.Warning("Unsaved scene changes in the main editor will not be loaded on other players. " +
                            $"Currently open scenes with unsaved changes include: {openSceneNames}");
                    }
                }

                EditorPrefs.SetBool(k_OpenGameViewOnPlayCache, OpenWindowOnEnteringPlayMode);
                m_QueuedToBroadcastPlay = true;

                foreach (var player in UnityPlayer.GetPlayers(mppmContext.SystemDataStore))
                {
                    if (!player.Active) continue;
                    if (player.Type == PlayerType.Main) continue;

                    // Ths is because a domain happens on the clones that is NOT triggered by the main editor.
                    // Clones are supposed to be read only. and as such.
                    // MPPM was built to be readonly and only asset sync by request of the main editor. What ever is causing this extra domain reload needs to be fixed
                    var vpID = player.TypeDependentPlayerInfo.VirtualProjectIdentifier;
                    vpContext.MessagingService.Send(new PlayMessage(), vpID, () =>
                    {
                        MppmLog.Debug("Sent the play message");
                    }, (e) =>
                    {
                        MppmLog.Debug("#1 Failed to send the play message for {vpID}");

                        vpContext.MessagingService.Send(new PlayMessage(), vpID, () =>
                         {
                             MppmLog.Debug("Sent the play message");
                         }, (e2) =>
                         {
                             MppmLog.Debug("#2 Failed to send the play message for {vpID}");

                             vpContext.MessagingService.Send(new PlayMessage(), vpID, () =>
                             {
                                 MppmLog.Debug("Sent the play message");
                             }, (e3) =>
                             {
                                 MppmLog.Debug("#3 Failed to send the play message for {vpID}");

                                 vpContext.MessagingService.Send(new PlayMessage(), vpID, () =>
                                 {
                                     MppmLog.Debug("Sent the play message");
                                 }, (e4) =>
                                 {
                                     MppmLog.Debug("Unable to send the play message for {vpID}");
                                 });
                             });
                         });
                    });
                }
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Pause += () =>
            {
                vpContext.MessagingService.Broadcast(new PauseMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Step += () =>
            {
                vpContext.MessagingService.Broadcast(new StepMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Unpause += () =>
            {
                vpContext.MessagingService.Broadcast(new UnpauseMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Stop += () =>
            {
                vpContext.MessagingService.Broadcast(new StopMessage());
                OpenWindowOnEnteringPlayMode = EditorPrefs.GetBool(k_OpenGameViewOnPlayCache);
                EditorPrefs.DeleteKey(k_OpenGameViewOnPlayCache);
            };
        }

        static bool IsAnyClonesOpen(WorkflowMainEditorContext context)
        {
            foreach (var player in UnityPlayer.GetPlayers(context.SystemDataStore))
            {
                if (player.Type == PlayerType.Clone && player.Active)
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsAnyDirtySceneOpen(out string openSceneNames)
        {
            openSceneNames = string.Empty;

            var openDirtyScenes = new List<Scene>();
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                {
                    openDirtyScenes.Add(scene);
                }
            }
            if (openDirtyScenes.Count <= 0)
            {
                return false;
            }

            var sceneNames = new List<string>();
            foreach (var scene in openDirtyScenes)
            {
                sceneNames.Add(scene.name);
            }
            openSceneNames = string.Join(", ", sceneNames);
            return true;
        }
    }
}

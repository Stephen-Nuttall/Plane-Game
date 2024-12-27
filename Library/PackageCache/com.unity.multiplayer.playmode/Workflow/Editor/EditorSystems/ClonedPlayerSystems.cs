using System;
using Unity.Multiplayer.PlayMode.Common.Runtime;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    struct Interval
    {
        public float Duration;
        public DateTime StartTime;

        public static bool HasHitInterval(ref Interval interval)
        {
            if ((DateTime.UtcNow - interval.StartTime).TotalSeconds >= interval.Duration)
            {
                interval.StartTime = DateTime.UtcNow;
                return true;
            }

            return false;
        }
    }


    class ClonedPlayerSystems
    {
        const string k_InitializeMessageSent = "mppm_InitializeMessageSent";

        // This is done because something like a EditorApplication.delayCall does not survive a domain reload
        // that could occur when going in or out of playmode
        const string k_FrameAfterPlaymodeMessage = "frameAfterPlaymodeMessage";

        readonly PlaymodeMessageQueue m_PlaymodeMessageQueue;
        LayoutFlags m_TestFlags;

        internal PlaymodeEvents PlaymodeEvents { get; }

        internal ClonedPlayerApplicationEvents ClonedPlayerApplicationEvents { get; }

        internal ClonedPlayerSystems()
        {
            ClonedPlayerApplicationEvents = new ClonedPlayerApplicationEvents();
            PlaymodeEvents = new PlaymodeEvents();
            m_PlaymodeMessageQueue = new PlaymodeMessageQueue();
        }

        private bool CanClose()
        {
            return true;
        }

        internal void Listen(WorkflowCloneContext mppmContext, CloneContext vpContext)
        {
            /*
             * These system classes are simply an aggregation of logic and other events
             *
             * Its only purpose is to forward events to the Internal Runtimes, Workflows, and MultiplayerPlaymode (UI)
             */
            var messagingService = vpContext.MessagingService;

            ContainerWindowProxy.SetMppmCanCloseCallback(CanClose);

            EditorApplicationProxy.RegisterUpdateMainWindowTitle(applicationTitleDescriptor =>
            {
                ClonedPlayerApplicationEvents.InvokeEditorStarted(applicationTitleDescriptor);
            });
            EditorApplication.pauseStateChanged += pauseState =>
            {
                if (pauseState == PauseState.Paused && EditorApplication.isPlaying)
                {
                    // Note: Just handle any case (including pause on error)
                    // Where the player ends up paused.
                    // Pause all the other players as well
                    ClonedPlayerApplicationEvents.InvokeClonePlayerPaused();
                }
            };

            messagingService.Receive<SyncStateMessage>(message => ClonedPlayerApplicationEvents.InvokeSyncStateRequested(message.State));

            messagingService.Receive<RuntimeSceneSwitchMessage>(message =>
            {
                Debug.Log($"Runtime Scene switch to '{message.Scene}'");
                SceneManager.LoadScene(message.Scene, LoadSceneMode.Single);
            });
            messagingService.Receive<PauseMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Pause));
            messagingService.Receive<UnpauseMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Unpause));
            messagingService.Receive<StepMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Step));
            messagingService.Receive<PlayMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Play));
            messagingService.Receive<StopMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Stop));
            messagingService.Receive<OpenPlayerWindowMessage>(_ =>
            {
                var viewFlags = EditorApplication.isPlaying
                    ? CloneDataFile.LoadFromFile(mppmContext.CloneDataFile).PlayModeLayoutFlags
                    : CloneDataFile.LoadFromFile(mppmContext.CloneDataFile).EditModeLayoutFlags;
#if UNITY_2023_2_OR_NEWER
                ModeSwitcher.SaveCurrentView();
                // Close the existing window if its open since we are about to reopen once we switch to view
                // NOTE: We close the window AFTER saving the window layout!
                ModeSwitcher.CloseCurrentWindow();
#endif
                ModeSwitcher.SwitchToView(viewFlags);
            });

            ConsoleWindowUtility.consoleLogsChanged += ClonedPlayerApplicationEvents.InvokeConsoleLogMessagesChanged;

            // Because we don't want to enter and exit playmode on the same frame (or immediately right after each other)
            // We instead wait on an interval to find a moment when the editor is not busy
            // At that moment we then process events in the order that they were added (with duplicates removed)
            var halfSecondInterval = new Interval { Duration = 0.5f, StartTime = DateTime.UtcNow, };
            var oneHalfSecondInterval = new Interval { Duration = 1.5f, StartTime = DateTime.UtcNow, };
            var initialTime = EditorApplication.timeSinceStartup;
            var systemDataStore = SystemDataStore.GetClone();
            Filters.FindFirstPlayerWithVirtualProjectsIdentifier(systemDataStore.LoadAllPlayerJson(),
                VirtualProjectsEditor.CloneIdentifier, out var player);
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup - initialTime > .05 && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    if (!SessionState.GetBool(k_InitializeMessageSent, false))
                    {
                        SessionState.SetBool(k_InitializeMessageSent, true);
                        messagingService.Broadcast(new PlayerInitializedMessage(VirtualProjectsEditor.CloneIdentifier));
                        ClonedPlayerApplicationEvents.InvokePlayerActive();
                    }
                }

                if (SessionState.GetBool(k_FrameAfterPlaymodeMessage, false))
                {
                    SessionState.SetBool(k_FrameAfterPlaymodeMessage, false);
                    ClonedPlayerApplicationEvents.InvokeFrameAfterPlaymodeMessage();
                }

                if (player.Tags.Contains("__test_layout"))
                {
                    if (Interval.HasHitInterval(ref oneHalfSecondInterval))
                    {
                        if ((int)m_TestFlags <= 0)
                        {
                            m_TestFlags = (LayoutFlags)1;
                        }

                        if (!ModeSwitcher.IsView(m_TestFlags))
                        {
                            Debug.Log($"Switching from '{ModeSwitcher.CurrentMode}' to '{m_TestFlags}'");
                            ModeSwitcher.CloseCurrentWindow();
                            ModeSwitcher.SwitchToView(m_TestFlags);
                        }
                        m_TestFlags++;
                        if (m_TestFlags >= LayoutFlagsUtil.All)
                        {
                            m_TestFlags = LayoutFlagsUtil.All;

                            var m = new TestResultMessage(VirtualProjectsEditor.CloneIdentifier, "", 0, /*needs to be false to be picked up*/ false, "Finished iterating layouts");
                            vpContext.MessagingService.Broadcast(m);
                        }
                    }
                }

                if (Interval.HasHitInterval(ref halfSecondInterval))
                {
                    ClonedPlayerApplicationEvents.InvokeUIPollUpdate();
                    bool isTooltipViewVisible = WindowLayout.IsTooltipViewVisible();

                    if (ModeSwitcher.CurrentWindow != null && !isTooltipViewVisible)
                    {
                        // Ensure the tooltip isn't visible; otherwise, it will automatically close
                        // Just save the view every second while the window is open just in case
                        // the process exits OR they click the close button (which currently just does a minimize)
                        //
                        // Now when that happens we will be able to load up the last positions of their view just fine
                        ModeSwitcher.SaveCurrentView();
                    }

                    if (SystemDataStore.GetClone().GetMutePlayers() != EditorUtility.audioMasterMute)
                    {
                        MppmLog.Debug("Audio setting was updated");
                        EditorUtility.audioMasterMute = SystemDataStore.GetClone().GetMutePlayers();
                    }

                    var isEditorBusy = EditorApplication.isCompiling
                                       || EditorApplication.isUpdating
                                       || EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode; // See https://docs.unity3d.com/ScriptReference/PlayModeStateChange.ExitingEditMode.html and https://docs.unity3d.com/ScriptReference/PlayModeStateChange.ExitingPlayMode.html
                    if (!isEditorBusy && m_PlaymodeMessageQueue.ReadEvent(out var pm))
                    {
                        switch (pm)
                        {
                            case PlayModeMessageTypes.Pause:
                                PlaymodeEvents.InvokePause();
                                break;
                            case PlayModeMessageTypes.Unpause:
                                PlaymodeEvents.InvokeUnpause();
                                break;
                            case PlayModeMessageTypes.Step:
                                PlaymodeEvents.InvokeStep();
                                break;
                            case PlayModeMessageTypes.Play:
                                PlaymodeEvents.InvokePlay();
                                SessionState.SetBool(k_FrameAfterPlaymodeMessage, true);
                                break;
                            case PlayModeMessageTypes.Stop:
                                PlaymodeEvents.InvokeStop();
                                SessionState.SetBool(k_FrameAfterPlaymodeMessage, true);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"{nameof(pm)}:{pm}");
                        }
                    }
                }
            };
        }
    }
}

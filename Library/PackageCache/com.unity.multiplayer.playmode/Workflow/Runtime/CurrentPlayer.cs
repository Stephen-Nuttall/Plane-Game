using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using Unity.Multiplayer.Playmode.Workflow.Editor;
#endif

namespace Unity.Multiplayer.Playmode
{
    /// <summary>
    /// Utility class to access information about the multiplayer play mode player while in playmode.
    /// </summary>
    public static class CurrentPlayer
    {
        // We only want to load the tag from the store once during play mode
        static bool s_Loaded;
        static List<string> s_Tags = new List<string>();
#if UNITY_EDITOR
        static SystemDataStore s_SystemDataStore;
#endif

        /// <summary>
        /// Returns the tag(s) assigned to the currently running player.
        /// </summary>
        /// <example>
        /// <code>
        /// void Update()
        /// {
        ///     if (CurrentPlayer.ReadOnlyTags().Contains("YellowTeam")) {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>Returns the array of assigned tags</returns>
        public static string[] ReadOnlyTags()
        {
            if (!s_Loaded)
            {
                s_Loaded = true;
                LoadTag();
            }

            return s_Tags.ToArray();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReloadLatestTagsOnEnterPlaymode()
        {
            s_Loaded = false;
        }

        static void LoadTag()
        {
#if UNITY_EDITOR
            s_SystemDataStore = VirtualProjectsEditor.IsClone
                ? SystemDataStore.GetClone()
                : SystemDataStore.GetMain();

            bool hasPlayer;
            PlayerStateJson player;
            if (VirtualProjectsEditor.IsClone)
            {
                hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(s_SystemDataStore.LoadAllPlayerJson(),
                    VirtualProjectsEditor.CloneIdentifier, out player);
                Debug.Assert(hasPlayer, $"Could not find player using virtual project {VirtualProjectsEditor.CloneIdentifier}");
            }
            else
            {
                hasPlayer = Filters.FindFirstPlayerWithPlayerType(s_SystemDataStore.LoadAllPlayerJson(), PlayerType.Main, out player);
                Debug.Assert(hasPlayer, "Could not find player for the main editor");
            }

            s_Tags = player.Tags;
#endif
        }

        /// <summary>
        /// This allows for asserting on the main editor from any Player (Useful for Runtime tests involving MonoBehaviour)
        /// </summary>
        /// <param name="condition"> The Condition</param>
        /// <param name="message"> The Message </param>
        /// <param name="callingFilePath"> The Calling File Path</param>
        /// <param name="lineNumber"> The Line Number</param>
        /// <example>
        /// <code>
        /// void Update()
        /// {
        ///     CurrentPlayer.ReportResult(Condition, "We successfully reported from a test");
        /// }
        /// </code>
        /// </example>
        public static void ReportResult(bool condition, string message = "", [CallerFilePath] string callingFilePath = "", [CallerLineNumber] int lineNumber = 0)
        {
#if UNITY_EDITOR
            if (VirtualProjectsEditor.IsClone)
            {
                var vpContext = EditorContexts.CloneContext;
                var m = new TestResultMessage(VirtualProjectsEditor.CloneIdentifier, callingFilePath, lineNumber, condition, message);
                vpContext.MessagingService.Broadcast(m);
            }
            else
            {
                var m = new TestResultMessage(null, callingFilePath, lineNumber, condition, message);
                if (!m.ResultCondition)
                {
                    VirtualProjectWorkflow.WorkflowMainEditorContext.TestFailure = m;
                }
            }
#endif
        }
    }
}

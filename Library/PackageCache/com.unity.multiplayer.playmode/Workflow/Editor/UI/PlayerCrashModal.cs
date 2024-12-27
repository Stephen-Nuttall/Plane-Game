using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class PlayerCrashModal
    {
        public enum Choices
        {
            Restart,
            Continue,
        }

        public static Choices DisplayPlayerCrashModal(string player)
        {
            var option = EditorUtility.DisplayDialog(
                $"{player} unexpectedly stopped",
                $"It appears that {player} has unexpectedly stopped.\nDo you want to restart it?",
                "Yes",
                "No");

            return option ? Choices.Restart : Choices.Continue;
        }
    }
}

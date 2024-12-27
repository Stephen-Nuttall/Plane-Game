using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    [Flags]
    enum LayoutFlags
    {
        None = 0,

        SceneHierarchyWindow = 1 << 0,
        GameView = 1 << 1,
        SceneView = 1 << 2,
        ConsoleWindow = 1 << 3,
        InspectorWindow = 1 << 4,
        MultiplayerPlayModeWindow = 1 << 5,
    }

    static class LayoutFlagsUtil
    {
        public static LayoutFlags[] AllAsArray { get; } = (LayoutFlags[])Enum.GetValues(typeof(LayoutFlags));
        public static LayoutFlags All { get; } = LayoutFlags.InspectorWindow
                                               | LayoutFlags.GameView
                                               | LayoutFlags.SceneHierarchyWindow
                                               | LayoutFlags.ConsoleWindow
                                               | LayoutFlags.SceneView
                                               | LayoutFlags.MultiplayerPlayModeWindow;
        public static string GenerateLayoutName(LayoutFlags layoutFlags) => $"layout_{(int)layoutFlags:0000}";
        public static LayoutFlags SetFlag(LayoutFlags flags, LayoutFlags flag, bool set) => set ? flag | flags : ~flag & flags;
        public static bool IsAnyFlagsSetInFlag(LayoutFlags flags, params LayoutFlags[] checkingAgainstFlags)
        {
            foreach (var flag in checkingAgainstFlags)
            {
                if (flags.HasFlag(flag)) return true;
            }

            return false;
        }

        // NOTE: it is intentional that this checks in the order that the array or params are passed
        // in order to easily respect the ordering from left to right (on the panels)
        public static LayoutFlags GetFirstMatchingFlag(LayoutFlags flags, params LayoutFlags[] checkingAgainstFlags)
        {
            foreach (var check in checkingAgainstFlags)
            {
                if (IsAnyFlagsSetInFlag(flags, check)) return check;
            }

            return LayoutFlags.None;
        }

        static bool IsOneFlagInCommon(LayoutFlags flags, LayoutFlags checkingAgainstFlag)
        {
            return (flags & checkingAgainstFlag) != 0;
        }
        public static LayoutFlags[] GetAsFlagsArray(LayoutFlags flags)
        {
            var result = new List<LayoutFlags>();
            foreach (var singleFlag in AllAsArray)
            {
                if (IsOneFlagInCommon(flags, singleFlag))
                    result.Add(singleFlag);
            }

            return result.ToArray();
        }
        public static int NumFlagsSet(LayoutFlags flags)
        {
            var result = 0;
            foreach (var singleFlag in AllAsArray)
            {
                if (IsOneFlagInCommon(flags, singleFlag))
                    result++;
            }

            return result;
        }
        public static string ToEditorClassName(LayoutFlags modeFlags) =>
            modeFlags switch
            {
                LayoutFlags.InspectorWindow => nameof(LayoutFlags.InspectorWindow),
                LayoutFlags.GameView => nameof(LayoutFlags.GameView),
                LayoutFlags.SceneView => nameof(LayoutFlags.SceneView),
                LayoutFlags.ConsoleWindow => nameof(LayoutFlags.ConsoleWindow),
                LayoutFlags.SceneHierarchyWindow => nameof(LayoutFlags.SceneHierarchyWindow),
                LayoutFlags.MultiplayerPlayModeWindow => nameof(LayoutFlags.MultiplayerPlayModeWindow),
                LayoutFlags.None => throw new ArgumentOutOfRangeException(nameof(modeFlags), modeFlags, null),
                _ => throw new ArgumentOutOfRangeException(nameof(modeFlags), modeFlags, null),
            };
    }
}

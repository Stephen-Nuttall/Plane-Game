using System.Collections.Generic;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    // These panels are supposed to be a visual representation of
    // the window management representation of MPPM Debugging Code (made by Selena)
    // It should be noted that these have diverged over time
    struct FullPanel
    {
        public LeftPanel Left;
        public SinglePanel Right;
        public LayoutFlags Flag;

        public LayoutFlags[] GetFlags()
        {
            if (Flag != LayoutFlags.None) return new[] { Flag };

            var flags = new List<LayoutFlags>();
            if (Left.Flag != LayoutFlags.None) flags.Add(Left.Flag);
            if (Right.Flag != LayoutFlags.None) flags.Add(Right.Flag);
            return flags.ToArray();
        }

        public override string ToString()
        {
            return Flag != LayoutFlags.None
                ? $"{nameof(Left)}: {Left}, {nameof(Right)}: {Right}, {nameof(Flag)}: {Flag}"
                : $"{nameof(Left)}: {Left}, {nameof(Right)}: {Right}";
        }
    }

    struct LeftPanel
    {
        public LeftTopPanel Top;
        public SinglePanel Bottom;
        public LayoutFlags Flag;

        public override string ToString()
        {
            return Flag != LayoutFlags.None
            ? $"[{nameof(Top)}: {Top}, {nameof(Bottom)}: {Bottom}, {nameof(Flag)}: {Flag}]"
            : $"[{nameof(Top)}: {Top}, {nameof(Bottom)}: {Bottom}]";
        }
    }

    struct LeftTopPanel
    {
        public SinglePanel Left;
        public SinglePanel Middle;
        public SinglePanel Right;
        public SinglePanel TabRight;
        public LayoutFlags Flag;

        public LayoutFlags[] GetFlags()
        {
            if (Flag != LayoutFlags.None) return new[] { Flag };

            var flags = new List<LayoutFlags>();
            if (Left.Flag != LayoutFlags.None) flags.Add(Left.Flag);
            if (Middle.Flag != LayoutFlags.None) flags.Add(Middle.Flag);
            if (Right.Flag != LayoutFlags.None) flags.Add(Right.Flag);
            if (TabRight.Flag != LayoutFlags.None) flags.Add(TabRight.Flag);
            return flags.ToArray();
        }

        public bool HasAllFlags(params LayoutFlags[] flags)
        {
            var hasAllFlags = true;
            foreach (var flag in flags)
            {
                hasAllFlags &= Left.Flag == flag || Middle.Flag == flag || Right.Flag == flag || TabRight.Flag == flag;
            }
            return hasAllFlags;
        }

        public override string ToString()
        {
            return Flag != LayoutFlags.None
            ? $"{nameof(Left)}: {Left}, {nameof(Middle)}: {Middle}, {nameof(Right)}: {Right}, {nameof(TabRight)}: {TabRight}, {nameof(Flag)}: {Flag}"
            : $"{nameof(Left)}: {Left}, {nameof(Middle)}: {Middle}, {nameof(Right)}: {Right}, {nameof(TabRight)}: {TabRight}";
        }
    }

    struct SinglePanel
    {
        public LayoutFlags Flag;

        public override string ToString()
        {
            return Flag != LayoutFlags.None
                ? $"[{nameof(Flag)}: {Flag}]"
                : "";
        }
    }
}

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    struct LayoutFile
    {
        // NOTE: We auto fill in the other layout file's fields with pre-known values/strings
        public View View;
    }

    enum Direction
    {
        None,
        Vertical,
        Horizontal,
    }

    struct View
    {
        public string EditorClassName;
        public Direction Direction;
        public bool Tabs;
        public LayoutFlags Flag;
        public float Size;
        public View[] Views;
    }
}

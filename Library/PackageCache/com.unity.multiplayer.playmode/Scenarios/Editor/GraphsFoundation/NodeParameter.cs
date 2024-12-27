using System;
using System.ComponentModel;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    [Serializable]
    internal abstract class NodeParameter
    {
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract Node GetNode();
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract T GetValue<T>();
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract void SetValue<T>(T value);
    }
}

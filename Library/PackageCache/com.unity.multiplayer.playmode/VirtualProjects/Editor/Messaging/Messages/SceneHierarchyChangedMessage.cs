using System.IO;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class SceneHierarchyChangedMessage
    {
        public SceneHierarchy SceneHierarchy { get; }

        public SceneHierarchyChangedMessage(SceneHierarchy sceneHierarchy)
        {
            SceneHierarchy = sceneHierarchy;
        }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as SceneHierarchyChangedMessage;
            SceneHierarchy.Serialize(writer, value.SceneHierarchy);
        }

        static object Deserialize(BinaryReader reader)
        {
            SceneHierarchy.TryParse(reader, out var sceneHierarchy);
            return new SceneHierarchyChangedMessage(sceneHierarchy);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}

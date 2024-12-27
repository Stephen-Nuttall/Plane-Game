using System.IO;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class SceneSavedMessage
    {
        public string SceneSaved { get; }

        public SceneSavedMessage(string sceneSaved)
        {
            SceneSaved = sceneSaved;
        }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as SceneSavedMessage;
            writer.Write(value.SceneSaved);
        }

        static object Deserialize(BinaryReader reader)
        {
            return new SceneSavedMessage(reader.ReadString());
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}

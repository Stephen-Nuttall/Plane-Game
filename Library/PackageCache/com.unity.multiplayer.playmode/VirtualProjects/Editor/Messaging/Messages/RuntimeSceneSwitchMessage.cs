using System.IO;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class RuntimeSceneSwitchMessage
    {
        public RuntimeSceneSwitchMessage(string scene)
        {
            Scene = scene;
        }
        public string Scene { get; }
        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as RuntimeSceneSwitchMessage;
            writer.Write(value.Scene);
        }

        static object Deserialize(BinaryReader reader)
        {
            return new RuntimeSceneSwitchMessage(reader.ReadString());
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}

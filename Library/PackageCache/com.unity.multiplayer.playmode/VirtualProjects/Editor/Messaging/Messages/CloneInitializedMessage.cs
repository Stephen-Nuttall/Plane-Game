using System.IO;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class CloneInitializedMessage
    {
        public CloneInitializedMessage(VirtualProjectIdentifier identifier)
        {
            Identifier = identifier;
        }

        public VirtualProjectIdentifier Identifier { get; }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as CloneInitializedMessage;
            writer.Write(value.Identifier.ToString());
        }

        static object Deserialize(BinaryReader reader)
        {
            VirtualProjectIdentifier.TryParse(reader.ReadString(), out var identifier);
            return new CloneInitializedMessage(identifier);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}

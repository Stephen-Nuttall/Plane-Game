using System.IO;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class AckMessage
    {
        public AckMessage(MessageId messageId)
        {
            MessageId = messageId;
        }

        public MessageId MessageId { get; }

        static void Serialize(BinaryWriter writer, object value)
        {
            var message = value as AckMessage;
            writer.Write(message.MessageId.ToString());
        }

        static object Deserialize(BinaryReader reader)
        {
            MessageId.TryParse(reader.ReadString(), out var messageId);
            return new AckMessage(messageId);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}

using System;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    struct PendingAck
    {
        public Action SuccessCallback;
        public Action<string> ErrorCallback;
        public DateTime SentTimestamp;
        public string EventType;
        public string Sender;
    }
}

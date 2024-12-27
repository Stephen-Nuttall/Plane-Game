using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    [Serializable]
    class SystemData
    {
        [JsonProperty]
        public bool IsMppmActive { get; internal set; }
        [JsonProperty]
        public bool IsMutePlayers { get; internal set; }

        [JsonProperty(Required = Required.Always)]
        public readonly Dictionary<int, PlayerStateJson> Data = new Dictionary<int, PlayerStateJson>();

        internal static string Serialize(ParsingSystemDelegates parsing, SystemData systemData)
        {
            return parsing.SerializeObjectFunc(systemData);
        }

        internal static bool TryDeserialize(ParsingSystemDelegates parsing, string data, out SystemData systemData)
        {
            try
            {
                systemData = (SystemData)parsing.DeserializeObjectFunc(data, typeof(SystemData));
            }
            catch (JsonException e) when (e is JsonSerializationException or JsonReaderException)
            {
                systemData = null;
            }

            return systemData != null;
        }
    }
}

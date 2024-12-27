using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    struct ParsingSystemDelegates
    {   // This simply represents the static methods of Newtonsoft.Json
        public delegate JObject Parse(string json);
        public delegate JObject FromObject(object o);
        public delegate object DeserializeObject(string data, Type type);
        public delegate string SerializeObject([CanBeNull] object data, Formatting formatting = Formatting.Indented);

        public Parse ParseFunc;
        public FromObject FromObjectFunc;
        public DeserializeObject DeserializeObjectFunc;
        public SerializeObject SerializeObjectFunc;
    }

    static class ParsingSystem
    {
        public static ParsingSystemDelegates Delegates { get; } = new ParsingSystemDelegates
        {
            ParseFunc = JObject.Parse,
            FromObjectFunc = JObject.FromObject,
            SerializeObjectFunc = JsonConvert.SerializeObject,
            DeserializeObjectFunc = JsonConvert.DeserializeObject,
        };
    }
}

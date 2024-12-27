using System;
using Newtonsoft.Json;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    [Serializable]
    struct CloneData
    {
        const LayoutFlags k_DefaultLayout = LayoutFlags.GameView | LayoutFlags.ConsoleWindow;

        public LayoutFlags EditModeLayoutFlags;
        public LayoutFlags PlayModeLayoutFlags;

        public override string ToString()
        {
            return $"{nameof(EditModeLayoutFlags)}: {EditModeLayoutFlags}, {nameof(PlayModeLayoutFlags)}: {PlayModeLayoutFlags}";
        }

        public static CloneData NewDefault()
        {
            return new CloneData
            {
                EditModeLayoutFlags = k_DefaultLayout,
                PlayModeLayoutFlags = k_DefaultLayout,
            };
        }

        public static string Serialize(CloneData data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static bool TryDeserialize(string data, out CloneData cloneData)
        {
            try
            {
                cloneData = JsonConvert.DeserializeObject<CloneData>(data);
                return true;
            }
            catch (JsonException e) when (e is JsonSerializationException or JsonReaderException)
            {
                cloneData = NewDefault();
                return false;
            }
        }
    }
}

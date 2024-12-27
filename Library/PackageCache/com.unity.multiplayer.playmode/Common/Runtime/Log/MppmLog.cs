using System.Diagnostics;

namespace Unity.Multiplayer.PlayMode.Common.Runtime
{
    static class MppmLog
    {
        const string k_ToolsPrefix = "[MultiplayerPlaymode]";

        [Conditional("UNITY_MP_TOOLS_DEV")]
        public static void Debug(object message) => UnityEngine.Debug.Log($"{k_ToolsPrefix}: {message}");

        public static void Warning(object message) => UnityEngine.Debug.LogWarning($"{k_ToolsPrefix}: {message}");

        public static void Error(object message) => UnityEngine.Debug.LogError($"{k_ToolsPrefix}: {message}");
    }
}

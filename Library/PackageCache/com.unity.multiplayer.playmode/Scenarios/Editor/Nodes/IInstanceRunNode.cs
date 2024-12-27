using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    internal interface IInstanceRunNode : IConnectableNode
    {
        public bool IsRunning();

        public static void PrintReceivedLog(string identifier, Color color, string message, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "{0}", CalculateLogString(identifier, color, message));
        }

        public static string CalculateLogString(string identifier, Color color, string message)
        {
            var colorHex = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            return $"<color={colorHex}>[{identifier}]</color> {message}";
        }
    }
}

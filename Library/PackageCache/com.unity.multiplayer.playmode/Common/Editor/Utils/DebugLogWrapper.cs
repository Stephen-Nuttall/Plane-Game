using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class LogWrapper
    {
        const bool showParameters = true;
        const string k_LogMethodName = "LogInformation";
        static readonly MethodBase m_Log;

        static LogWrapper()
        {
            m_Log = typeof(UnityEngine.Debug).GetMethod(k_LogMethodName, BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static void Log(string msg, int skipFrames = 1)
        {
            if (m_Log == null)
            {
                UnityEngine.Debug.LogError(msg);
                return;
            }

            var message = new StringBuilder();
            var stackTrace = new StackTrace(true);
            var stackFrames = stackTrace.GetFrames();

            message.AppendLine(msg);

            GetStack(stackFrames, message, skipFrames, out string file, out int line, out int col);
            m_Log.Invoke(null, new object[] { message.ToString(), file, line, col });
        }
        static void GetStack(StackFrame[] stackFrames, StringBuilder message, int skipFrames, out string file, out int line, out int col)
        {
            file = string.Empty;
            line = 0;
            col = 0;

            if (stackFrames == null || stackFrames.Length == 0) return;

            for (var index = skipFrames; index < stackFrames.Length; index++)
            {
                var stackFrame = stackFrames[index];
                var mb = stackFrame.GetMethod();
                var fileName = stackFrame.GetFileName();
                var declaringType = mb.DeclaringType;

                if (index == skipFrames)
                {
                    file = fileName;
                    line = stackFrame.GetFileLineNumber();
                    col = stackFrame.GetFileColumnNumber();
                }

                message.AppendFormat("{0}:{1}", declaringType?.FullName, mb.Name);

                if (showParameters)
                {
                    AppendParameters(message, mb);
                }

                if (fileName != null)
                {
                    message.AppendFormat("(at <a href=\"{0}\" line=\"{1}\">{0}:{1}</a>)", fileName, stackFrame.GetFileLineNumber());
                }

                message.AppendLine();
            }
        }
        static void AppendParameters(StringBuilder message, MethodBase mb)
        {
            message.Append(" (");
            var parameters = mb.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                message.Append(parameters[i].ParameterType.Name);
                if (i + 1 < parameters.Length)
                {
                    message.Append(", ");
                }
            }
            message.Append(") ");
        }
    }
}

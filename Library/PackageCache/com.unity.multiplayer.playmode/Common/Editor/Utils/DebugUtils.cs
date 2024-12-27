using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class DebugUtils
    {
        /// <summary>
        /// Define UNITY_DEDICATED_SERVER_DEBUG_TRACE, either at the call site or in project settings, to enable logging
        /// </summary>
        /// <param name="message"> The Message</param>
        /// <param name="skipFrames"> Frames to skip</param>
        /// <param name="filepath"> The File Path</param>
        /// <param name="methodName"> The Method Name</param>
        /// <param name="line"> The Line</param>
        [Conditional("UNITY_DEDICATED_SERVER_DEBUG_TRACE")]
        public static void Trace(string message,
            int skipFrames = 0,
            [CallerFilePath] string filepath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int line = 0)
        {
            LogWrapper.Log(message, skipFrames + 2);
        }

#if UNITY_DEDICATED_SERVER_DEBUG_TRACE
        static string FormatMethodName(string typeName, string methodName)
        {
            const string k_ConstructorMethodName = ".ctor";
            const string k_StaticConstructorMethodName = ".cctor";
            return methodName switch
            {
                k_ConstructorMethodName => $"{typeName}()",
                k_StaticConstructorMethodName => $"static {typeName}()",
                _ => $"{typeName}.{methodName}()",
            };
        }
#endif

        /// <summary>
        /// Logs the name of the calling method as &lt;FileName&gt;.&lt;MethodName&gt;
        /// </summary>
        /// <remarks>
        /// This approach is ~25x faster than <see cref="TraceMethodNameUsingStackFrame"/>,
        /// but will be less clear in cases in which the typename does not match the file name,
        /// or in which there are multiple constructors or methods of the same name belonging to
        /// different types in the same file.
        /// <br/>
        /// In such cases you can use <see cref="TraceMethodNameUsingStackFrame"/> for a less ambiguous alternative.
        /// </remarks>
        [Conditional("UNITY_DEDICATED_SERVER_DEBUG_TRACE")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TraceMethodName(
            [CallerFilePath] string filepath = "",
            [CallerMemberName] string methodName = "")
        {
#if UNITY_MP_TOOLS_DEBUG_TRACE
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filepath);
            Debug.Log(FormatMethodName(filenameWithoutExtension, methodName));
#endif
        }

        [Conditional("UNITY_DEDICATED_SERVER_DEBUG_TRACE")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TraceMethodNameUsingStackFrame()
        {
#if UNITY_MP_TOOLS_DEBUG_TRACE
            var sf = new StackFrame(1);
            var method = sf.GetMethod();
            var typeName = method.DeclaringType!.Name;
            var methodName = method.Name;
            Debug.Log(FormatMethodName(typeName, methodName));
#endif
        }
    }
}

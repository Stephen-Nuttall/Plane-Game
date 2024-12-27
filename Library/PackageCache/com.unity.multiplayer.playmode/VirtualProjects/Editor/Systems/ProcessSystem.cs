using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    struct ProcessSystemDelegates
    {   // This simply represents the static methods of a ProcessSystem
        public delegate bool IsRunning(int processID);
        public delegate void Kill(int processId);
        public delegate bool TryRun(string filename, string arguments, out int processID, out string error);
        public delegate int OurId();

        public IsRunning IsRunningFunc;
        public Kill KillFunc;
        public TryRun TryRunFunc;
        public OurId OurIdFunc;
    }

    static class ProcessSystem
    {
        public static ProcessSystemDelegates Delegates { get; } = new ProcessSystemDelegates
        {
            IsRunningFunc = IsRunning,
            KillFunc = Kill,
            TryRunFunc = TryRunProcess,
            OurIdFunc = GetCurrentProcessId,
        };

        public static bool IsRunning(int processId)
        {
            using var process = GetProcessById(processId);
            return !(process == null || process.HasExited);
        }

        public static bool TryRunProcess(string executableFullPath, string executableArguments, out int processID,
            out string error)
        {
            return InnerTryRunProcess(waitForExit: false,
                executableFullPath, executableArguments, out processID, out error);
        }

        public static bool TryRunProcessWaitForExit(string executableFullPath, string executableArguments,
            out int processID, out string error)
        {
            return InnerTryRunProcess(waitForExit: true,
                executableFullPath, executableArguments, out processID, out error);
        }

        static bool InnerTryRunProcess(bool waitForExit, string executableFullPath, string executableArguments,
            out int processID, out string error)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executableFullPath,
                Arguments = executableArguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            using var process = new Process();
            process.StartInfo = startInfo;

            var success = process.Start();
            if (!success)
            {
                var output = process.StandardOutput.ReadLine();
                var error1 = process.StandardError.ReadLine();

                error = output + error1;
                processID = -1;
                return false;
            }

            if (waitForExit)
            {
                process.WaitForExit();

                if (!process.StandardError.EndOfStream)
                {
                    error = process.StandardError.ReadToEnd();
                    processID = process.Id;
                    return false;
                }
            }

            error = string.Empty;
            processID = process.Id;
            return true;
        }

        public static void Kill(int processId)
        {
            using var process = GetProcessById(processId);
            if (process == null || process.HasExited) return;
            if (process.CloseMainWindow()) return;

            const int numAttempts = 3;
            for (var attempted = 0; attempted < numAttempts; attempted++)
            {
                if (attempted > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }

                if (TryProcessKill(process, out var exception))
                {
                    break;
                }

                if (attempted >= numAttempts - 1)
                {
                    throw exception;
                }
            }
        }

        static bool TryProcessKill(Process process, out Exception outException)
        {
            outException = null;
            try
            {
                process.Kill();
            }
            catch (Win32Exception e)
            {
                // Happens if still terminating
                outException = e;
                return false;
            }
            catch (InvalidOperationException)
            {
                // Happens if already terminated
            }

            return true;
        }

        public static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

        internal static Process GetProcessById(int processId)
        {
            try { return Process.GetProcessById(processId); }
            catch (ArgumentException) { return null; }
        }
    }
}

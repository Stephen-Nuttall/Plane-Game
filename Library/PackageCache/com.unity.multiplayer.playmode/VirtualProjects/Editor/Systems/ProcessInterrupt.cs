using System.Diagnostics;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    // This class, for now, does NOT survive domain reloads. (all we need to do in order to do that is use virtualProjectStatePerProcessLifetime and listen to the domain reload event and resubscribe)
    // That is a larger refactor that can be done later. Right now we only care about the licence server exiting, which currently when this occurs, it happens almost immediately.
    static class ProcessInterrupt
    {
        public delegate void ProcessExitHandler(Process process, int exitCode);

        public static bool SubscribeToProcessExit(int processId, ProcessExitHandler onProcessExit)
        {
            var process = ProcessSystem.GetProcessById(processId);
            if (process != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += (sender, _) =>
                {
                    if (sender is Process p)
                    {
                        onProcessExit(p, p.ExitCode);
                    }
                };
                return true;
            }

            return false;
        }
    }
}

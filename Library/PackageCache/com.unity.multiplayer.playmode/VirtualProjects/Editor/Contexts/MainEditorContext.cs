using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor.MPE;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class MainEditorContext
    {
        internal MainEditorContext()
        {
            if (!ChannelService.IsRunning())
            {
                ChannelService.Start();
            }

            MainEditorSystems = new MainEditorSystems();
            {
                MessagingService = MessagingService.GetMain(MessagingService.k_DefaultChannelName);
                StateRepository = SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime>.GetMain(SessionStateRepository.Get, nameof(StateRepository), out _);
                ProcessRepository = SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId>.GetMain(SessionStateRepository.Get, nameof(ProcessRepository), out _);
                ProcessSystemDelegates = ProcessSystem.Delegates;
                FileSystemDelegates = FileSystem.Delegates;
                ParsingSystemDelegates = ParsingSystem.Delegates;
                {
                    Editor.VirtualProjectsApi.Initialize(FileSystemDelegates, ParsingSystemDelegates, ProcessSystemDelegates, ProcessRepository, StateRepository, ProcessLaunchTimes);
                    ////////VVV  Initialize before getting the delegates  VVV////////
                    VirtualProjectsApi = Editor.VirtualProjectsApi.Delegates;
                }
                var internalRuntime = new MainEditorInternalRuntime();
                internalRuntime.HandleEvents(this);
            }
            MainEditorSystems.Listen(this);
        }

        public Dictionary<VirtualProjectIdentifier, double> ProcessLaunchTimes { get; } = new Dictionary<VirtualProjectIdentifier, double>();
        public MessagingService MessagingService { get; }
        public SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> StateRepository { get; }
        public SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> ProcessRepository { get; }
        public MainEditorSystems MainEditorSystems { get; }
        internal VirtualProjectsApiDelegates VirtualProjectsApi { get; }
        ProcessSystemDelegates ProcessSystemDelegates { get; }
        FileSystemDelegates FileSystemDelegates { get; }
        ParsingSystemDelegates ParsingSystemDelegates { get; }
    }
}

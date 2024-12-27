namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class CloneContext
    {
        internal CloneContext()
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX // Remove once minimum editor version is >=2023.3.0b8.
            if (!UnityEditor.MPE.ChannelService.IsRunning())
            {
                UnityEditor.MPE.ChannelService.Start();
            }
#endif

            CloneSystems = new CloneSystems();
            {
                MessagingService = MessagingService.GetClone(CommandLineParameters.ReadCurrentChannelName());
                ProcessSystemDelegates = ProcessSystem.Delegates;
                var internalRuntime = new CloneInternalRuntime();
                internalRuntime.HandleEvents(this);
            }
            CloneSystems.Listen(vpContext: this);
        }

        public MessagingService MessagingService { get; }
        public CloneSystems CloneSystems { get; }
        public ProcessSystemDelegates ProcessSystemDelegates { get; }
    }
}

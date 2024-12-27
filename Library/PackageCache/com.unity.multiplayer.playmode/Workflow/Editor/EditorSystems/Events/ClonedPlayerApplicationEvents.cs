using System;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class ClonedPlayerApplicationEvents
    {
        public event Action PlayerActive;
        public event Action<EditorTitleUpdater> PlayerTitleRename;
        public event Action ConsoleLogMessagesChanged;
        public event Action PlayerPaused;
        public event Action FrameAfterPlaymodeMessage;
        public event Action UIPollUpdate;
        public event Action<CloneState> SyncStateRequested;

        public void InvokeClonePlayerPaused()
        {
            PlayerPaused?.Invoke();
        }

        public void InvokeEditorStarted(ApplicationTitleDescriptorProxy applicationTitle)
        {
            var updater = new EditorTitleUpdater(applicationTitle);
            PlayerTitleRename?.Invoke(updater);
        }

        public void InvokeConsoleLogMessagesChanged()
        {
            ConsoleLogMessagesChanged?.Invoke();
        }

        public void InvokePlayerActive()
        {
            PlayerActive?.Invoke();
        }

        internal void InvokeFrameAfterPlaymodeMessage()
        {
            FrameAfterPlaymodeMessage?.Invoke();
        }

        public void InvokeUIPollUpdate()
        {
            UIPollUpdate?.Invoke();
        }

        public void InvokeSyncStateRequested(CloneState state)
        {
            SyncStateRequested?.Invoke(state);
        }
    }
}

using System;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class PlaymodeEvents
    {
        public event Action Play;
        public event Action Pause;
        public event Action Step;
        public event Action Unpause;
        public event Action Stop;

        internal void InvokePlay()
        {
            Play?.Invoke();
        }

        internal void InvokePause()
        {
            Pause?.Invoke();
        }

        internal void InvokeStep()
        {
            Step?.Invoke();
        }

        internal void InvokeUnpause()
        {
            Unpause?.Invoke();
        }

        internal void InvokeStop()
        {
            Stop?.Invoke();
        }
    }
}

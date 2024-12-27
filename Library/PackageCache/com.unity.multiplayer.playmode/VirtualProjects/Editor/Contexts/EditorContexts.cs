using System;
using UnityEditor;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    static class EditorContexts
    {
        public static event Action OnInitialized
        {
            add
            {
                if (IsInitialized)
                {
                    value?.Invoke();
                    return;
                }

                s_PendingOnInitializedCallbacks += value;
            }
            remove
            {
                if (IsInitialized)
                {
                    return;
                }

                s_PendingOnInitializedCallbacks -= value;
            }
        }

        static Action s_PendingOnInitializedCallbacks;

        public static bool IsInitialized { get; set; }

        static MainEditorContext s_MainEditorContext;
        static CloneContext s_CloneContext;

        [InitializeOnLoadMethod]
        static void SendReadyEvent()
        {
            if (VirtualProjectsEditor.IsClone)
            {
                s_CloneContext = new CloneContext();
            }
            else
            {
                if (!CommandLineParameters.IsUMPE())
                {
                    s_MainEditorContext = new MainEditorContext();
                }
            }

            if (!CommandLineParameters.ReadNoDownChainDependencies() && !CommandLineParameters.IsUMPE())
            {
                IsInitialized = true;
                s_PendingOnInitializedCallbacks?.Invoke();
                s_PendingOnInitializedCallbacks = null;
            }
        }

        public static MainEditorContext MainEditorContext
        {
            get
            {
                if (VirtualProjectsEditor.IsClone)
                {
                    throw new NotSupportedException("Main Editor functionality cannot be accessed from Clones.");
                }

                return s_MainEditorContext;
            }
        }

        public static CloneContext CloneContext
        {
            get
            {
                if (!VirtualProjectsEditor.IsClone && !CommandLineParameters.IsUMPE())
                {
                    throw new NotSupportedException("Clone functionality cannot be accessed from the Main Editor.");
                }

                return s_CloneContext;
            }
        }
    }
}

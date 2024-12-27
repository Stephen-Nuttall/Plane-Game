using System;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation
{
    internal class DomainReloadScopeLock : IDisposable
    {
        public DomainReloadScopeLock()
        {
            EditorApplication.LockReloadAssemblies();
        }

        public void Dispose()
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}

using System;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class SceneEvents
    {
        public event Action<SceneHierarchy> SceneHierarchyChanged;
        public event Action<string> SceneSaved;

        internal void InvokeSceneHierarchyChanged(SceneHierarchy sceneHierarchy)
        {
            SceneHierarchyChanged?.Invoke(sceneHierarchy);
        }

        internal void InvokeSceneSaved(string savedScene)
        {
            SceneSaved?.Invoke(savedScene);
        }
    }
}

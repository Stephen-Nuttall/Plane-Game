using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Multiplayer.PlayMode.Common.Runtime;
using UnityEditor;
using UnityEngine;
#if UNITY_MP_TOOLS_DEV
using UnityEditor;
#endif

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class MainEditorInternalRuntime
    {
        const int k_MessageDispatchRateMs = 25;

        internal delegate DateTime CurrentTimeProviderFunc();

        internal delegate void ThreadSleepFunc(int ms);

        internal delegate void ThreadBlockingOperation(
            Func<bool> completionCondition,
            int timeoutMs,
            Action tickFunction,
            int tickRateMs);
        public void HandleEvents(MainEditorContext vpContext)
        {
            vpContext.MainEditorSystems.AssetImportEvents.RequestImport += (didDomainReload, numAssetsChanged) =>
            {
#if UNITY_MP_TOOLS_DEV_LOGMESSAGES && UNITY_MP_TOOLS_DEV
            Debug.Log("Running with both [UNITY_MP_TOOLS_DEV_LOGMESSAGES] and [UNITY_MP_TOOLS_DEV]");
#elif UNITY_MP_TOOLS_DEV
            Debug.Log("Running with only [UNITY_MP_TOOLS_DEV]");
#elif UNITY_MP_TOOLS_DEV_LOGMESSAGES
            Debug.Log("Running with only [UNITY_MP_TOOLS_DEV_LOGMESSAGES]");
#endif
                var runningClones = new List<VirtualProjectIdentifier>();
                foreach (var p in vpContext.VirtualProjectsApi.GetProjectsFunc(VirtualProjectsApi.k_FilterAll))
                {
                    if (p.EditorState == EditorState.Launched)
                    {
                        runningClones.Add(p.Identifier);
                    }
                }

                if (runningClones.Count == 0)
                {
                    MppmLog.Debug("No Virtual projects to sync assets with.");
                    return;
                }

                // We have to check if the domain reloads because sometimes rider will cause the number of assets changed to be 0 during code changes
                // This is a result of MTTB-331 and MTTB-390
                if (numAssetsChanged == 0 && !didDomainReload)
                {
                    MppmLog.Debug("No assets to actually sync with clones.");
                    return;
                }

                // Tell the clones to perform asset import
                vpContext.MessagingService.Broadcast(new TriggerCloneRefreshMessage(didDomainReload, numAssetsChanged));
            };

            vpContext.MainEditorSystems.SceneEvents.SceneHierarchyChanged += newSceneHierarchy =>
            {
                vpContext.MessagingService.Broadcast(new SceneHierarchyChangedMessage(newSceneHierarchy));
            };
            vpContext.MainEditorSystems.SceneEvents.SceneSaved += sceneSaved =>
            {
                if (string.IsNullOrWhiteSpace(sceneSaved))
                {
                    return;
                }
                vpContext.MessagingService.Broadcast(new SceneSavedMessage(sceneSaved));
            };
        }
    }
}

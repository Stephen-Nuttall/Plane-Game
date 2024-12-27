using System.Diagnostics.CodeAnalysis;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor;
using System;
using System.Threading;
using Unity.Multiplayer.PlayMode.Common.Editor;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    internal class ScenarioRunner : ScriptableSingleton<ScenarioRunner>
    {
        private static readonly ScenarioStatus k_DefaultScenarioState = new ScenarioStatus(ScenarioStage.None, NodeState.Invalid);

        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (instance.m_Scenario != null && instance.m_Scenario.Status.State == ScenarioState.Running)
                instance.RunOrResume();
        }

        private Scenario m_Scenario;
        private CancellationTokenSource m_CancellationTokenSource;

        internal Scenario ActiveScenario => m_Scenario;

        internal static event Action<ScenarioStatus> StatusChanged;

        public static void StartScenario([NotNull] Scenario scenario)
        {
            if (instance.m_Scenario != null && instance.m_Scenario.Status.State == ScenarioState.Running)
            {
                instance.m_CancellationTokenSource?.Cancel();
                instance.m_Scenario.StatusChanged -= OnStatusChanged;
                instance.m_Scenario = null;

                throw new InvalidOperationException("There is already a scenario running. Stopping it before starting a new one.");
            }

            instance.m_Scenario = scenario;
            instance.RunOrResume();
        }

        private void RunOrResume()
        {
            m_CancellationTokenSource = new CancellationTokenSource();
            instance.m_Scenario.StatusChanged += OnStatusChanged;
            instance.m_Scenario.RunOrResumeAsync(m_CancellationTokenSource.Token).Forget();
        }

        private static void OnStatusChanged(ScenarioStatus status)
        {
            StatusChanged?.Invoke(status);
        }

        public static ScenarioStatus GetScenarioStatus()
        {
            return instance.m_Scenario?.Status ?? k_DefaultScenarioState;
        }

        public static void StopScenario()
        {
            // instance.m_Scenario = null;
            AssetMonitor.Reset();

            if (instance.m_Scenario != null)
            {
                instance.m_CancellationTokenSource?.Cancel();

                // We cannot guarantee that the scenario will be stopped as it depends on the nodes implementation.
                // We assume they consume the cancellation token properly.
                instance.m_Scenario = null;
                instance.m_CancellationTokenSource = null;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Trying to stop an scenario but there is currently no scenario running.");
            }
        }
    }
}

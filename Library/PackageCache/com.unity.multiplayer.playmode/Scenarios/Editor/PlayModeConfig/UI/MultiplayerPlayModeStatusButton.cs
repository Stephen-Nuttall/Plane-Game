using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Views;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Button that opens the status of current selected playmode config
    /// aswell as visualizes the progress of the deployment
    /// </summary>
    internal class MultiplayerPlayModeStatusButton : EditorToolbarButton
    {
        const string k_StatusButtonName = "multiplayer-playmodestatus-button";
        const string k_DeployProgressbarName = "deploy-progress";
        const string k_MainIconName = "icon";

        // used in tests therefore it is internal
        internal const string ussClassIdle = "idle";
        internal const string ussClassProcessing = "processing";
        internal const string ussClassRunning = "running";
        internal const string ussClassError = "error";

        VisualElement m_Progressbar;
        readonly VisualElement m_StatusIndicatior;
        const string k_StylePath = "Scenarios/Editor/PlayModeConfig/UI/MultiplayerPlayModeStatusButton.uss";

        public MultiplayerPlayModeStatusButton(ScenarioConfig config)
        {
            name = k_StatusButtonName;
            m_Progressbar = new VisualElement();
            m_Progressbar.name = k_DeployProgressbarName;

            icon = Icons.GetImage(Icons.ImageName.Loading);
            var buttonIcon = this.Q<Image>();
            buttonIcon.name = k_MainIconName;

            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            Add(arrow);

            Add(m_Progressbar);
            SetProgress(ScenarioRunner.GetScenarioStatus());

            UIUtils.ApplyStyleSheet(k_StylePath, this);
            ///schedule.Execute(SetProgress(ScenarioRunner.GetScenarioStatus())).Every(500); // [JRU] - Probably not needed anymore.

            PlayModeManager.instance.StateChanged += _ => SetProgress(ScenarioRunner.GetScenarioStatus());
            ScenarioRunner.StatusChanged += _ => SetProgress(ScenarioRunner.GetScenarioStatus());
            clicked += () => UnityEditor.PopupWindow.Show(new Rect(worldBound.x + worldBound.width - PlaymodeStatusPopupContent.windowSize.x, worldBound.y, worldBound.width, worldBound.height), new PlaymodeStatusPopupContent()); ;
        }

        void ClearUssClasses()
        {
            RemoveFromClassList(ussClassError);
            RemoveFromClassList(ussClassRunning);
            RemoveFromClassList(ussClassProcessing);
            RemoveFromClassList(ussClassIdle);
        }

        void SetProgress(ScenarioStatus state)
        {
            var stage = state.CurrentStage;

            var currentStage = state.State.ToString();
            var stageProgress = state.Progress;

            ClearUssClasses();
            switch (state.State)
            {
                case ScenarioState.Idle or ScenarioState.Aborted or ScenarioState.Completed:
                    AddToClassList(ussClassIdle);
                    icon = Icons.GetImage(Icons.ImageName.Idle);
                    m_Progressbar.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
                    break;
                case ScenarioState.Running:
                {
                    icon = Icons.GetImage(Icons.ImageName.Loading);

                    // Set label to stage because we are actually running a scenario
                    currentStage = stage.ToString();

                    // Run has 2 states, basically when we start running and we are in the run state itself.
                    if (stage == ScenarioStage.Run)
                        currentStage = "Start";
                    if (state.CurrentStage == ScenarioStage.Run && state.StageState == NodeState.Active)
                    {
                        m_Progressbar.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
                        AddToClassList(ussClassRunning);
                        currentStage = "Running";
                        icon = Icons.GetImage(Icons.ImageName.CompletedTask);
                    }
                    else
                    {
                        m_Progressbar.AddToClassList("animate");
                        var overallProgress = ((int)stage - 1) * 33 + stageProgress * 33;
                        m_Progressbar.style.width = new StyleLength(new Length(overallProgress, LengthUnit.Percent));
                        var tooltipText = $"Scenario Progress\n{currentStage} - {Mathf.Ceil(stageProgress * 100)}%\nOverall -  {Mathf.Ceil(overallProgress)}% ";
                        tooltip = tooltipText;

                        AddToClassList(ussClassProcessing);
                    }

                    break;
                }
                case ScenarioState.Failed:
                    AddToClassList(ussClassError);
                    icon = Icons.GetImage(Icons.ImageName.Error);
                    m_Progressbar.RemoveFromClassList("animate");
                    m_Progressbar.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
                    break;
                default:
                    AddToClassList(ussClassIdle);
                    m_Progressbar.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
                    break;
            }

            text = currentStage;
        }
    }
}

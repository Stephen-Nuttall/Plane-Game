using Unity.Multiplayer.Playmode.VirtualProjects.Editor;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class MultiplayerPlaymodeLogUtility
    {
        public static BoxedLogCounts PlayerLogs(PlayerIdentifier identifier)
        {
            var mppmContext = VirtualProjectWorkflow.WorkflowMainEditorContext;

            if (!mppmContext.LogsRepository.TryGetValue(identifier, out var playerLogs))
            {
                playerLogs = new BoxedLogCounts();
                mppmContext.LogsRepository.Create(identifier, playerLogs);
            }

            return playerLogs;
        }
    }
}

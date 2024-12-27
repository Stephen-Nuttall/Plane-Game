using Unity.Multiplayer.PlayMode.Common.Editor;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class WorkflowMainEditorContext
    {
        public WorkflowMainEditorContext(MainEditorContext mainEditorContext)
        {
            MainPlayerSystems = new MainPlayerSystems();
            {
                var workflow = new StandardMainEditorWorkflow();
                LogsRepository = new InMemoryRepository<PlayerIdentifier, BoxedLogCounts>();
                SystemDataStore = SystemDataStore.GetMain();
                ProjectDataStore = ProjectDataStore.GetMain();
                workflow.Initialize(mppmContext: this, vpContext: mainEditorContext);
            }
            MainPlayerSystems.Listen(mppmContext: this, vpContext: mainEditorContext);
        }

        internal InMemoryRepository<PlayerIdentifier, BoxedLogCounts> LogsRepository { get; }
        internal ProjectDataStore ProjectDataStore { get; }
        internal SystemDataStore SystemDataStore { get; }

        internal MainPlayerSystems MainPlayerSystems { get; }
        public TestResultMessage TestFailure { get; set; }
    }
}

using System.IO;
using Unity.Multiplayer.Playmode.VirtualProjects.Editor;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class WorkflowCloneContext
    {
        internal CloneDataFile CloneDataFile { get; }
        internal ClonedPlayerSystems ClonedPlayerSystems { get; }

        static readonly string DefaultCloneEditorPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        internal WorkflowCloneContext(CloneContext cloneContext)
        {
            ClonedPlayerSystems = new ClonedPlayerSystems();
            {
                CloneDataFile = new CloneDataFile
                {
                    Path = CloneDataFile.CloneDataPath(DefaultCloneEditorPath),
                    Data = CloneData.NewDefault(),
                };
                var workflow = new StandardCloneWorkflow();
                workflow.Initialize(mppmContext: this, vpContext: cloneContext);
            }
            ClonedPlayerSystems.Listen(mppmContext: this, vpContext: cloneContext);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    class ReadyOnlyEditor : AssetModificationProcessor
    {
        static bool IsReadOnly => VirtualProjectsEditor.IsClone;

        static bool CanOpenForEdit(string[] paths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)
        {
            if (IsReadOnly)
            {
                outNotEditablePaths.Clear();
                outNotEditablePaths.AddRange(paths);
            }

            return !IsReadOnly;
        }

        static bool IsOpenForEdit(string[] paths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)
        {
            if (IsReadOnly)
            {
                outNotEditablePaths.Clear();
                outNotEditablePaths.AddRange(paths);
            }

            return !IsReadOnly;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            return IsReadOnly ? Array.Empty<string>() : paths;
        }

        static bool MakeEditable(string[] paths, string prompt, List<string> outNotEditablePaths)
        {
            if (IsReadOnly)
            {
                outNotEditablePaths.Clear();
                outNotEditablePaths.AddRange(paths);
            }

            return !IsReadOnly;
        }

        [CommandHandler("Commands/No Add Component")]
        static void NoAddComponent(CommandExecuteContext context)
        {
            /*** This disables the 'Add Component' Button in the Inspector view ***/
            // Ideally this would just all be done with an EditorMode capability called 'remove_add_component_button'
            // But the editor code is already set up to do things this way with a command handler
            // and with 'inspector_read_only' as the name of that command handler
            // command handlers need id's (since its done through reflection)
            // and so you need an method with a command handler attribute
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class Convert
    {
        static string WriteFieldVertical { get; } = @"vertical = true" + Environment.NewLine;
        static string WriteFieldHorizontal { get; } = @"horizontal = true" + Environment.NewLine;
        static string WriteFieldTabs { get; } = @"tabs = true" + Environment.NewLine;
        // As long as we generate the layouts on boot (and don't use golden files) we will need to use invariant cultures for float formatting
        static string WriteFieldSize(float size, bool hasNewline = true) => string.Format(CultureInfo.InvariantCulture, "size = {0}", size) + (hasNewline ? Environment.NewLine : string.Empty);

        static string WriteFieldEditorClass(string editorClass) => $@"class_name = ""{editorClass}""";
        static string WriteFieldChildren(IEnumerable<string> childrenJson) => @$"children = [
{string.Join("\n", childrenJson)}
            ]";

        internal static string ToSJSON(View view)
        {
            if (!string.IsNullOrWhiteSpace(view.EditorClassName))
            {
                var sizeString = view.Size > 0 ? $"{WriteFieldSize(view.Size, false)} " : string.Empty;
                return $@"                {{ {WriteFieldEditorClass(view.EditorClassName)} {sizeString}}}";
            }

            var json = string.Empty;
            if (view.Direction == Direction.Vertical) json += WriteFieldVertical;
            if (view.Direction == Direction.Horizontal) json += WriteFieldHorizontal;

            if (view.Tabs)
            {
                json += WriteFieldTabs;
            }

            if ((int)view.Flag != 0)
            {
                var sizeString = view.Size > 0 ? $"{WriteFieldSize(view.Size, false)} " : string.Empty;
                json += $@"                {{ {WriteFieldEditorClass(LayoutFlagsUtil.ToEditorClassName(view.Flag))} {sizeString}}}";
                return json;
            }

            if (view.Size is > 0 and < 1)
            {
                json += WriteFieldSize(view.Size);
            }

            if (view.Views is { Length: > 0 })
            {
                var childrenJson = new List<string>();
                foreach (var child in view.Views)
                {
                    childrenJson.Add(ToSJSON(child));
                }

                json += $@"            {WriteFieldChildren(childrenJson)}";
            }

            return $@"{{
{json}
}}";
        }

        public static string ToSJSON(LayoutFile layoutFile)
        {
            var fileContents = "";
            fileContents += "restore_saved_layout = true" + Environment.NewLine;
            fileContents += "top_view =                 { class_name = \"TopView\" size = 30 }" + Environment.NewLine;
            fileContents += $"view = {ToSJSON(layoutFile.View)}";
            return fileContents;
        }
    }
}

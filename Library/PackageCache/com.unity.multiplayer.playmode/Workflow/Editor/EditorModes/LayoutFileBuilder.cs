using System.Collections.Generic;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class LayoutFileBuilder
    {
        public static LayoutFile BuildLayoutFileBasedOnPanels(FullPanel panelData)
        {
            var layoutView = new View();
            if (panelData.Flag == LayoutFlags.None)
            {
                if (panelData.Right.Flag == LayoutFlags.InspectorWindow && panelData.Left.Flag == LayoutFlags.ConsoleWindow)
                {
                    layoutView.Direction = Direction.Horizontal;
                    layoutView.Views = CreateViewsGroup(new[] { panelData.Left.Flag }, new[] { panelData.Right.Flag });
                }
                else if (panelData.Right.Flag == LayoutFlags.InspectorWindow &&
                         panelData.Left.Top.TabRight.Flag == LayoutFlags.MultiplayerPlayModeWindow && panelData.Left.Bottom.Flag == LayoutFlags.ConsoleWindow)
                {
                    var flags = new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                    var leftView = new View { Direction = Direction.Vertical, Tabs = false, Size = 0.5f };

                    var rightview = new View
                    {
                        Tabs = true,
                        Size = 0.5f,
                        Views = new[]
                    {
                        new View { Flag = panelData.Right.Flag },
                        new View { Flag = panelData.Left.Top.TabRight.Flag }
                    }
                    };
                    var consoleView = new View { Tabs = true, Size = 0.5f, Views = new[] { new View { Flag = panelData.Left.Bottom.Flag } } };
                    rightview.Size = 0.5f;
                    if (flags.Count != 1)
                    {
                        var leftTopViews = CreateViewsGroup(flags.Count, flags.ToArray()[..^1]);
                        leftView.Views = new[] { new View { Direction = Direction.Horizontal, Size = 0.5f, Views = leftTopViews }, consoleView };
                    }
                    else
                    {
                        var leftTopViews = new View { Tabs = true, Size = 0.5f, Views = new[] { new View { Flag = panelData.Left.Top.Flag } } };
                        leftView.Views = new[] { leftTopViews, consoleView };
                    }
                    layoutView.Direction = Direction.Horizontal;
                    layoutView.Views = new[] { leftView, rightview };

                }

                else if (panelData.Right.Flag == LayoutFlags.InspectorWindow &&
                         panelData.Left.Top.TabRight.Flag == LayoutFlags.MultiplayerPlayModeWindow)
                {
                    var rightview = new View
                    {
                        Tabs = true,
                        Size = 0.5f,
                        Views = new[]
                    {
                        new View { Flag = panelData.Right.Flag },
                        new View { Flag = panelData.Left.Top.TabRight.Flag }
                    }
                    };
                    var leftView = new View { Direction = Direction.Vertical, Tabs = false, Size = 0.5f };
                    layoutView.Direction = Direction.Horizontal;
                    var flags = new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                    if (flags.Count == 2)
                    {
                        if (panelData.Left.Top.Left.Flag != LayoutFlags.None ||
                            panelData.Left.Top.Right.Flag != LayoutFlags.None ||
                            panelData.Left.Top.Middle.Flag != LayoutFlags.None)
                        {
                            var leftRightViewsHorizontally =
                                new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                            leftView.Views = CreateViewsGroup(3, leftRightViewsHorizontally.ToArray()[..^1]);
                            layoutView.Views = new[] { leftView, rightview };
                        }
                    }
                    else if (panelData.Left.Bottom.Flag == LayoutFlags.None)
                    {
                        if (panelData.Left.Top.HasAllFlags(LayoutFlags.SceneView, LayoutFlags.SceneHierarchyWindow, LayoutFlags.GameView))
                        {
                            var threeLeftFlagsHorizontally = new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                            leftView.Views = CreateViewsGroup(4, threeLeftFlagsHorizontally.ToArray()[..^1]);
                            layoutView.Views = new[] { leftView, rightview };
                        }
                        else
                        {
                            // Shift flags into the left or right position (while accounting for the middle panel)
                            var leftLeftFlag = panelData.Left.Top.Right.Flag == LayoutFlags.GameView ? panelData.Left.Top.Left.Flag | panelData.Left.Top.Middle.Flag : panelData.Left.Top.Left.Flag;
                            var leftRightFlag = panelData.Left.Top.Right.Flag == LayoutFlags.GameView ? panelData.Left.Top.Right.Flag : panelData.Left.Top.Middle.Flag;
                            var leftRightViewsHorizontally = new List<LayoutFlags>() { leftLeftFlag, leftRightFlag };
                            leftView.Views = CreateViewsGroup(2, leftRightViewsHorizontally.ToArray());
                            layoutView.Views = new[] { leftView, rightview };
                        }
                    }

                }

                else if (panelData.Right.Flag == LayoutFlags.InspectorWindow && panelData.Left.Bottom.Flag == LayoutFlags.ConsoleWindow)
                {
                    var flags = new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                    var leftView = new View { Direction = Direction.Vertical, Tabs = false, Size = 0.5f };
                    var rightView = new View { Tabs = true, Size = 0.5f, Views = new[] { new View { Flag = panelData.Right.Flag } } };
                    var consoleView = new View { Tabs = true, Size = 0.5f, Views = new[] { new View { Flag = panelData.Left.Bottom.Flag } } };
                    if (flags.Count != 1)
                    {
                        var leftTopViews = CreateViewsGroup(flags.Count, flags.ToArray());
                        leftView.Views = new[] { new View { Direction = Direction.Horizontal, Size = 0.5f, Views = leftTopViews }, consoleView };
                    }
                    else
                    {
                        var leftTopViews = new View { Tabs = true, Size = 0.5f, Views = new[] { new View { Flag = panelData.Left.Top.Flag } } };
                        leftView.Views = new[] { leftTopViews, consoleView };
                    }

                    layoutView.Direction = Direction.Horizontal;
                    layoutView.Views = new[] { leftView, rightView };
                }
                else if (panelData.Right.Flag == LayoutFlags.InspectorWindow)
                {
                    layoutView.Direction = Direction.Horizontal;
                    if (panelData.Left.Flag != LayoutFlags.None)
                    {
                        layoutView.Views = CreateViewsGroup(new[] { panelData.Left.Flag }, new[] { panelData.Right.Flag });
                    }
                    else if (panelData.Left.Bottom.Flag == LayoutFlags.None)
                    {
                        if (panelData.Left.Top.HasAllFlags(LayoutFlags.SceneView, LayoutFlags.SceneHierarchyWindow, LayoutFlags.GameView))
                        {
                            var allLeftAndRightFlagsHorizontally = new List<LayoutFlags>(panelData.Left.Top.GetFlags()) { panelData.Right.Flag };
                            layoutView.Views = CreateViewsGroup(4, allLeftAndRightFlagsHorizontally.ToArray());
                        }
                        else
                        {
                            // Shift flags into the left or right position (while accounting for the middle panel)
                            var leftLeftFlag = panelData.Left.Top.Right.Flag == LayoutFlags.GameView ? panelData.Left.Top.Left.Flag | panelData.Left.Top.Middle.Flag : panelData.Left.Top.Left.Flag;
                            var leftRightFlag = panelData.Left.Top.Right.Flag == LayoutFlags.GameView ? panelData.Left.Top.Right.Flag : panelData.Left.Top.Middle.Flag;
                            layoutView.Views = CreateViewsGroup(new[] { leftLeftFlag, leftRightFlag }, new[] { panelData.Right.Flag });
                        }
                    }
                }
                else if (panelData.Left.Bottom.Flag == LayoutFlags.ConsoleWindow)
                {
                    layoutView.Direction = Direction.Vertical;
                    var flags = new List<LayoutFlags>(panelData.Left.Top.GetFlags());
                    if (flags.Count == 1)
                    {
                        layoutView.Views = CreateViewsGroup(flags.ToArray(), new[] { panelData.Left.Bottom.Flag });
                    }
                    else
                    {
                        var innerViews = CreateViewsGroup(flags.Count, flags.ToArray());
                        var views = new List<View>();
                        views.Add(new View { Direction = Direction.Horizontal, Size = 0.5f, Views = innerViews });
                        views.AddRange(CreateViewsGroup(2, panelData.Left.Bottom.Flag));
                        layoutView.Views = views.ToArray();
                    }
                }
                else
                {
                    var flags = panelData.Left.Top.GetFlags();
                    if (flags.Length == 0) flags = panelData.GetFlags();
                    layoutView.Direction = Direction.Horizontal;
                    layoutView.Views = CreateViewsGroup(flags.Length, flags);
                }
            }
            else
            {
                layoutView = new View { Tabs = true, Views = new[] { new View { Flag = panelData.Flag } }, };
            }

            return new LayoutFile { View = layoutView };
        }

        static View[] CreateViewsGroup(int number, params LayoutFlags[] flags)
        {
            var sizePer = (float)1 / number;
            var views = new List<View>();
            foreach (var flag in flags)
            {
                var view = flag == LayoutFlags.None
                    ? new View()
                    : new View { Tabs = true, Views = new[] { new View { Flag = flag } }, };
                view.Size = sizePer;
                views.Add(view);
            }
            return views.ToArray();
        }

        static View[] CreateViewsGroup(LayoutFlags[] left, LayoutFlags[] right)
        {
            var views = new List<View>();
            views.AddRange(CreateViewsGroup(left.Length * 2, left));
            views.AddRange(CreateViewsGroup(right.Length * 2, right));
            return views.ToArray();
        }
    }
}

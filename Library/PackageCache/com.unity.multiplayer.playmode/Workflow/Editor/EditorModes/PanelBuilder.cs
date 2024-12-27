namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    static class PanelBuilder
    {
        internal static FullPanel BuildPanelsBasedOnFlags(LayoutFlags flags) => LayoutFlagsUtil.NumFlagsSet(flags) switch
        {
            1 => OnePanelsBasedOnFlags(flags),
            2 => TwoPanelsBasedOnFlags(flags),
            3 => ThreePanelsBasedOnFlags(flags),
            4 => FourPanelsBasedOnFlags(flags),
            5 => FivePanelsBasedOnFlags(flags),
            _ => SixPanelsBasedOnFlags(),
        };

        static FullPanel OnePanelsBasedOnFlags(LayoutFlags flags) => new FullPanel { Flag = flags };
        static FullPanel TwoPanelsBasedOnFlags(LayoutFlags flags)
        {
            var hasConsole = LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.ConsoleWindow);
            var hasGameOrSceneOrHierarchyOrMultiplayer = LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.GameView,
                LayoutFlags.SceneView, LayoutFlags.SceneHierarchyWindow, LayoutFlags.MultiplayerPlayModeWindow);
            var hasConsoleAndHasGameOrSceneOrHierarchyOrMultiplayer = hasConsole && hasGameOrSceneOrHierarchyOrMultiplayer;
            if (hasConsoleAndHasGameOrSceneOrHierarchyOrMultiplayer)
            {
                var copyWithoutConsole = flags & ~LayoutFlags.ConsoleWindow;
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel { Flag = copyWithoutConsole, },
                        Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, },
                    },
                };
            }

            var twoFlags = LayoutFlagsUtil.GetAsFlagsArray(flags);
            return new FullPanel
            {
                Left = new LeftPanel { Flag = twoFlags[0], },
                Right = new SinglePanel { Flag = twoFlags[1], },
            };
        }

        static FullPanel ThreePanelsBasedOnFlags(LayoutFlags flags)
        {
            var hasConsole = LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.ConsoleWindow);
            var hasInspector = LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.InspectorWindow);
            if (hasConsole)
            {
                if (hasInspector) // :hasConsole And :hasInspector
                {
                    var topPanelFlag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView, LayoutFlags.SceneView,
                        LayoutFlags.SceneHierarchyWindow, LayoutFlags.MultiplayerPlayModeWindow);
                    topPanelFlag = topPanelFlag == LayoutFlags.None ? LayoutFlags.SceneHierarchyWindow : topPanelFlag;
                    return new FullPanel
                    {
                        Left = new LeftPanel
                        {
                            Top = new LeftTopPanel { Flag = topPanelFlag, },
                            Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, },
                        },
                        Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow, },
                    };
                }

                // :hasConsole And :noInspector
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                        Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, },
                    },
                };
            }

            if (hasInspector) // :noConsole And :hasInspector
            {
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                    },
                    Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow, },
                };
            }

            return new FullPanel
            {
                Left = new LeftPanel
                {
                    Top = new LeftTopPanel
                    {
                        Left = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                        },
                        Middle = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                        },
                        Right = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                        },
                        TabRight = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                        },
                    },
                },
            };
        }

        static FullPanel FourPanelsBasedOnFlags(LayoutFlags flags)
        {
            var noConsoleWindow = !LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.ConsoleWindow);
            if (noConsoleWindow)
            {
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                    },
                    Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow },
                };
            }

            var noInspectorWindow = !LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.InspectorWindow);
            if (noInspectorWindow)
            {
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                        Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, }
                    },
                };
            }

            return new FullPanel
            {
                Left = new LeftPanel
                {
                    Top = new LeftTopPanel
                    {
                        Left = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                        },
                        Middle = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                        },
                        Right = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                        },
                        TabRight = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                        }
                    },
                    Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, }
                },
                Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow, },
            };
        }

        static FullPanel FivePanelsBasedOnFlags(LayoutFlags flags)
        {
            var noConsoleWindow = !LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.ConsoleWindow);
            if (noConsoleWindow)
            {
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                    },
                    Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow },
                };
            }

            var noInspectorWindow = !LayoutFlagsUtil.IsAnyFlagsSetInFlag(flags, LayoutFlags.InspectorWindow);
            if (noInspectorWindow)
            {
                return new FullPanel
                {
                    Left = new LeftPanel
                    {
                        Top = new LeftTopPanel
                        {
                            Left = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                            },
                            Middle = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                            },
                            Right = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                            },
                            TabRight = new SinglePanel
                            {
                                Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                            }
                        },
                        Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, }
                    },
                };
            }

            return new FullPanel
            {
                Left = new LeftPanel
                {
                    Top = new LeftTopPanel
                    {
                        Left = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneHierarchyWindow),
                        },
                        Middle = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.SceneView),
                        },
                        Right = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.GameView),
                        },
                        TabRight = new SinglePanel
                        {
                            Flag = LayoutFlagsUtil.GetFirstMatchingFlag(flags, LayoutFlags.MultiplayerPlayModeWindow)
                        }
                    },
                    Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, }
                },
                Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow, },
            };

        }
        static FullPanel SixPanelsBasedOnFlags() => new FullPanel
        {
            Left = new LeftPanel
            {
                Top = new LeftTopPanel
                {
                    Left = new SinglePanel { Flag = LayoutFlags.SceneHierarchyWindow, },
                    Middle = new SinglePanel { Flag = LayoutFlags.SceneView, },
                    Right = new SinglePanel { Flag = LayoutFlags.GameView, },
                    TabRight = new SinglePanel { Flag = LayoutFlags.MultiplayerPlayModeWindow, },
                },
                Bottom = new SinglePanel { Flag = LayoutFlags.ConsoleWindow, }
            },
            Right = new SinglePanel { Flag = LayoutFlags.InspectorWindow, },
        };

    }
}

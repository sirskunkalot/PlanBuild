﻿using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class SelectionCommands
    {
        private const string SnapPointsParam = "saveCurrentSnapPoints";
        private const string MarkersParam = "keepMarkers";

        public static void Init()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            CommandManager.Instance.AddConsoleCommand(new SelectionGUICommand());
            CommandManager.Instance.AddConsoleCommand(new ClearSelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new CopySelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new CutSelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new SaveSelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteSelectionCommand());
        }

        public static bool CheckSelection()
        {
            if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
            {
                Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                return false;
            }

            if (!Selection.Instance.Any())
            {
                Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                return false;
            }

            return true;
        }
        
        /// <summary>
        ///     Console command to show the Selection GUI
        /// </summary>
        private class SelectionGUICommand : ConsoleCommand
        {
            public override string Name => "selection.gui";

            public override string Help => "Show the selection GUI";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }

                if (Console.IsVisible())
                {
                    Console.instance.m_chatWindow.gameObject.SetActive(false);
                }

                SelectionGUI.Instance.Show();
            }
        }

        /// <summary>
        ///     Console command to clear the current selection
        /// </summary>
        private class ClearSelectionCommand : ConsoleCommand
        {
            public override string Name => "selection.clear";

            public override string Help => "Clear the current selection";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }

                Selection.Instance.Clear();
            }
        }

        /// <summary>
        ///     Console command to copy the current selection
        /// </summary>
        private class CopySelectionCommand : ConsoleCommand
        {
            public override string Name => "selection.copy";

            public override string Help => "Copy the current selection as a temporary blueprint";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }

                SelectionTools.Copy(Selection.Instance, args.Contains(SnapPointsParam), args.Contains(MarkersParam));
                Selection.Instance.Clear();
            }

            public override List<string> CommandOptionList()
            {
                return new List<string>
                {
                    SnapPointsParam, MarkersParam
                };
            }
        }
        
        /// <summary>
        ///     Console command to cut the current selection
        /// </summary>
        private class CutSelectionCommand : ConsoleCommand
        {
            public override string Name => "selection.cut";

            public override string Help => "Cut out the current selection as a temporary blueprint";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }
                
                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_select_cut_disabled"));
                    return;
                }

                SelectionTools.Cut(Selection.Instance, args.Contains(SnapPointsParam), args.Contains(MarkersParam));
                Selection.Instance.Clear();
            }

            public override List<string> CommandOptionList()
            {
                return new List<string>
                {
                    SnapPointsParam, MarkersParam
                };
            }
        }
        
        /// <summary>
        ///     Console command to save the current selection as a blueprint
        /// </summary>
        private class SaveSelectionCommand : ConsoleCommand
        {
            public override string Name => "selection.save";

            public override string Help => "Save the current selection as a blueprint";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }

                SelectionTools.SaveWithGUI(Selection.Instance, args.Contains(SnapPointsParam), args.Contains(MarkersParam));
            }

            public override List<string> CommandOptionList()
            {
                return new List<string>
                {
                    SnapPointsParam, MarkersParam
                };
            }
        }
        
        /// <summary>
        ///     Console command to delete the current selection
        /// </summary>
        private class DeleteSelectionCommand : ConsoleCommand
        {
            public override string Name => "selection.delete";

            public override string Help => "Delete all prefabs in the current selection";

            public override void Run(string[] args)
            {
                if (!CheckSelection())
                {
                    return;
                }

                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_select_delete_disabled"));
                    return;
                }

                SelectionTools.Delete(Selection.Instance);
                Selection.Instance.Clear();
            }
        }
    }
}
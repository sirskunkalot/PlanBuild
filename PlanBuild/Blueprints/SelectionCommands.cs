using Jotunn.Entities;
using Jotunn.Managers;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class SelectionCommands
    {
        public static void Init()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            CommandManager.Instance.AddConsoleCommand(new CopySelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new CopySelectionWithSnapPointsCommand());
            CommandManager.Instance.AddConsoleCommand(new SaveSelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteSelectionCommand());
            CommandManager.Instance.AddConsoleCommand(new ClearSelectionCommand());
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
                if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                    return;
                }

                if (!Selection.Instance.Any())
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                    return;
                }

                SelectionTools.Copy(false);
            }
        }
        
        /// <summary>
        ///     Console command to copy the current selection with snap points
        /// </summary>
        private class CopySelectionWithSnapPointsCommand : ConsoleCommand
        {
            public override string Name => "selection.copywithsnappoints";

            public override string Help => "Copy the current selection as a temporary blueprint including the vanilla snap points";

            public override void Run(string[] args)
            {
                if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                    return;
                }

                if (!Selection.Instance.Any())
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                    return;
                }

                SelectionTools.Copy(true);
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
                if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                    return;
                }

                if (!Selection.Instance.Any())
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                    return;
                }

                SelectionTools.Save();
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
                if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                    return;
                }

                if (!Selection.Instance.Any())
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                    return;
                }

                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_select_delete_disabled"));
                    return;
                }

                SelectionTools.Delete();
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
                if (!(Player.m_localPlayer && Player.m_localPlayer.InPlaceMode()))
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_inactive"));
                    return;
                }

                if (!Selection.Instance.Any())
                {
                    Console.instance.Print(Localization.instance.Localize("$msg_blueprint_select_empty"));
                    return;
                }
                
                Selection.Instance.Clear();
            }
        }
    }
}
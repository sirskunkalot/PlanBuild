using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class PlanCommands
    {
        public static void Init()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintBlacklistCommand());
            CommandManager.Instance.AddConsoleCommand(new AddBlacklistCommand());
            CommandManager.Instance.AddConsoleCommand(new RemoveBlacklistCommand());
        }

        /// <summary>
        ///     Console command which outputs the current plan blacklist
        /// </summary>
        private class PrintBlacklistCommand : ConsoleCommand
        {
            public override string Name => "plan.blacklist.print";

            public override string Help => "Print out the server's plan blacklist";

            public override void Run(string[] args)
            {
                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    return;
                }

                Console.instance.Print($"{PlanBlacklist.GetNames().Join()}");
            }
        }

        /// <summary>
        ///     Console command to add a prefab to the blacklist
        /// </summary>
        private class AddBlacklistCommand : ConsoleCommand
        {
            public override string Name => "plan.blacklist.add";

            public override string Help => "Add a prefab to the server's plan blacklist";

            public override void Run(string[] args)
            {
                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    return;
                }

                if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    Console.instance.Print($"Usage: {Name} <prefab_name>");
                    return;
                }

                string prefabName = args[0].Trim();
                GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);

                if (!prefab)
                {
                    Console.instance.Print($"Prefab {prefabName} does not exist");
                    return;
                }

                if (!prefab.GetComponent<Piece>())
                {
                    Console.instance.Print($"Prefab {prefabName} has no piece component");
                    return;
                }
                
                PlanBlacklist.Add(prefabName);
            }

            public override List<string> CommandOptionList()
            {
                return ZNetScene.instance.GetPrefabNames();
            }
        }
        
        /// <summary>
        ///     Console command to remove a prefab from the blacklist
        /// </summary>
        private class RemoveBlacklistCommand : ConsoleCommand
        {
            public override string Name => "plan.blacklist.remove";

            public override string Help => "Removes a prefab from the server's plan blacklist";

            public override void Run(string[] args)
            {
                if (!SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    return;
                }

                if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    Console.instance.Print($"Usage: {Name} <prefab_name>");
                    return;
                }

                string prefabName = args[0].Trim();
                PlanBlacklist.Remove(prefabName);
            }

            public override List<string> CommandOptionList()
            {
                return PlanBlacklist.GetNames();
            }
        }
    }
}
using Jotunn.Entities;
using Jotunn.Managers;

namespace PlanBuild.Plans
{
    internal class PlanCommands
    {
        public static void Init()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintBlacklistCommand());
            CommandManager.Instance.AddConsoleCommand(new AddBlacklistCommand());
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

                Console.instance.Print($"{PlanBlacklist.GetNames()}");
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

                if (args.Length != 1 || string.IsNullOrEmpty(args[0]))
                {
                    Console.instance.Print($"Usage: {Name} <prefab_name>");
                    return;
                }

                PlanBlacklist.Add(args[0]);
            }
        }
    }
}
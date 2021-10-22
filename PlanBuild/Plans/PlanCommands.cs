using Jotunn.Entities;
using Jotunn.Managers;

namespace PlanBuild.Plans
{
    internal class PlanCommands
    {
        public static void Init()
        {
            CommandManager.Instance.AddConsoleCommand(new AddBlacklistCommand());
        }

        /// <summary>
        ///     Console command which outputs the local blueprint list
        /// </summary>
        private class AddBlacklistCommand : ConsoleCommand
        {
            public override string Name => "plan.blacklist.add";

            public override string Help => "Add a prefab to the server's plan blacklist";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print($"Usage: {Name} <prefab_name>\n");
                    return;
                }

                PlanBlacklist.Add(args[0]);
            }
        }
    }
}
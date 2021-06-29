using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace PlanBuild.Blueprints
{
    internal class BlueprintCommands
    {
        public static void Init()
        {
            CommandManager.Instance.AddConsoleCommand(new GetLocalListCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new PushBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new GetServerListCommand());
            CommandManager.Instance.AddConsoleCommand(new PullBlueprintCommand());
        }

        /// <summary>
        ///     Console command which outputs the local blueprint list
        /// </summary>
        private class GetLocalListCommand : ConsoleCommand
        {
            public override string Name => "bp.local";

            public override string Help => "Get the list of your local blueprints";

            public override void Run(string[] args)
            {
                BlueprintSync.GetLocalBlueprints();
                BlueprintManager.Instance.RegisterKnownBlueprints();
                Console.instance.Print(BlueprintManager.LocalBlueprints.ToString());
            }
        }

        /// <summary>
        ///     Console command to delete a local blueprint
        /// </summary>
        private class DeleteBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.remove";

            public override string Help => "Remove a local blueprint";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print($"Usage: {Name} <blueprint_name>\n");
                    return;
                }

                var id = args[0];
                if (BlueprintSync.RemoveLocalBlueprint(id))
                {
                    Console.instance.Print($"Removed blueprint {id}\n");
                }
            }
        }

        /// <summary>
        ///     Console command which uploads a blueprint to the server
        /// </summary>
        private class PushBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.push";

            public override string Help => "Upload a local blueprint to the current connected server";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print($"Usage: {Name} <blueprint_name>\n");
                    return;
                }

                var id = args[0];
                BlueprintSync.PushBlueprint(id, (bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not upload blueprint: {message}\n");
                    }
                    else
                    {
                        Console.instance.Print($"Blueprint {id} uploaded\n");
                    }
                });
            }
        }

        /// <summary>
        ///     Console command which queries and outputs the server blueprint list
        /// </summary>
        private class GetServerListCommand : ConsoleCommand
        {
            public override string Name => "bp.server";

            public override string Help => "Get the list of the current connected servers blueprints";

            public override void Run(string[] args)
            {
                BlueprintSync.GetServerBlueprints((bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not get server list: {message}\n");
                    }
                    else
                    {
                        Console.instance.Print(BlueprintManager.ServerBlueprints.ToString());
                    }
                }, useCache: args.Length == 0
                );
            }
        }

        /// <summary>
        ///     Console command which queries and saves a named blueprint from the server
        /// </summary>
        private class PullBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.pull";

            public override string Help => "Load a blueprint from the current connected server and add it to your local blueprints";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print($"Usage: {Name} <blueprint_name>\n");
                    return;
                }

                var id = args[0];
                BlueprintSync.GetServerBlueprints((bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not load blueprint: {message}\n");
                    }
                    else if (BlueprintSync.PullBlueprint(id))
                    {
                        Console.instance.Print($"Loaded blueprint {id} from server\n");
                    }
                });
            }
        }
    }
}

using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class BlueprintSync
    {
        private static BlueprintList ServerList;
        private static Action OnListReceived;
        
        internal static void Init()
        {
            On.Game.Start += RegisterRPC;
            CommandManager.Instance.AddConsoleCommand(new GetLocalListCommand());
            CommandManager.Instance.AddConsoleCommand(new GetServerListCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new PullBlueprintCommand());
        }

        private static void RegisterRPC(On.Game.orig_Start orig, Game self)
        {
            orig(self);
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_GetServerList), new Action<long, ZPackage>(RPC_PlanBuild_GetServerList));
        }

        /// <summary>
        ///     When connected to a server clear current server list, register callback to the delegate and finally invoke the RPC.<br />
        ///     Per default the server list gets cached after the first load. Set useCache to false to force a refresh from the server.
        /// </summary>
        /// <param name="callback">Delegate method which gets called when the server list was received</param>
        /// <param name="useCache">Return the internal cached list after loading, defaults to true</param>
        internal static void GetServerList(Action callback, bool useCache = true)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (useCache && ServerList != null)
                {
                    Jotunn.Logger.LogDebug("Getting server blueprint list from cache");
                    callback?.Invoke();
                }
                else
                {
                    Jotunn.Logger.LogDebug("Requesting server blueprint list");

                    ServerList = null;
                    OnListReceived += callback;

                    ZRoutedRpc.instance.InvokeRoutedRPC(
                        ZNet.instance.GetServerPeer().m_uid, nameof(RPC_PlanBuild_GetServerList), new ZPackage());
                }
            }
        }

        /// <summary>
        ///     RPC method for sending / receiving the actual blueprint lists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pkg"></param>
        private static void RPC_PlanBuild_GetServerList(long sender, ZPackage pkg)
        {
            // Server receive (local game and dedicated)
            if (ZNet.instance.IsServer())
            {
                // Validate peer
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Jotunn.Logger.LogDebug($"Sending blueprint data to peer #{sender}");

                    // Reload and send current blueprint list in BlueprintManager back to the original sender
                    BlueprintManager.Instance.LoadKnownBlueprints();
                    ZRoutedRpc.instance.InvokeRoutedRPC(
                        sender, nameof(RPC_PlanBuild_GetServerList), BlueprintManager.Instance.Blueprints.ToZPackage());
                }
            }
            // Client receive
            else
            {
                // Validate the message is from the server and not another client.
                if (pkg != null && pkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Jotunn.Logger.LogDebug("Received blueprints from server");
                    
                    // Deserialize list, call delegates and finally clear delegates
                    ServerList = BlueprintList.FromZPackage(pkg);
                    try
                    {
                        OnListReceived?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogError(ex);
                    }
                    finally
                    {
                        OnListReceived = null;
                    }
                }
            }
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
                if (BlueprintManager.Instance.Blueprints.Count() == 0)
                {
                    Console.instance.Print("No blueprints available");
                    return;
                }
                BlueprintManager.Instance.LoadKnownBlueprints();
                BlueprintManager.Instance.RegisterKnownBlueprints();
                Console.instance.Print(BlueprintManager.Instance.Blueprints.ToString());
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
                if (!ZNet.instance || ZNet.instance.IsServer())
                {
                    Console.instance.Print("Not connected");
                    return;
                }
                GetServerList(() => { Console.instance.Print(ServerList?.ToString()); }, args.Length == 0);
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
                    Console.instance.Print($"Usage: {Name} <blueprint_name>");
                    return;
                }
                var id = args[0];
                if (BlueprintManager.Instance.Blueprints.ContainsKey(id))
                {
                    Blueprint oldbp = BlueprintManager.Instance.Blueprints[id];
                    oldbp.Destroy();
                    BlueprintManager.Instance.Blueprints.Remove(id);

                    Console.instance.Print($"Removed blueprint {id}");
                }
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
                    Console.instance.Print($"Usage: {Name} <blueprint_name>");
                    return;
                }
                if (!ZNet.instance || ZNet.instance.IsServer())
                {
                    Console.instance.Print("Not connected");
                    return;
                }
                GetServerList(() => {
                    if (ServerList != null && ServerList.TryGetValue(args[0], out var blueprint))
                    {
                        if (blueprint.Save())
                        {
                            if (BlueprintManager.Instance.Blueprints.ContainsKey(blueprint.ID))
                            {
                                Blueprint oldbp = BlueprintManager.Instance.Blueprints[blueprint.ID];
                                oldbp.Destroy();
                                BlueprintManager.Instance.Blueprints.Remove(blueprint.ID);
                            }

                            blueprint.CreatePrefab();
                            Player.m_localPlayer.UpdateKnownRecipesList();
                            BlueprintManager.Instance.Blueprints.Add(blueprint.ID, blueprint);

                            Console.instance.Print($"Loaded blueprint {blueprint.ID} from server.");
                        }
                    }
                });
            }
        }
    }
}

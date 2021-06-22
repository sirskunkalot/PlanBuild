using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class BlueprintSync
    {
        private static BlueprintList ServerList;
        private static Action<bool, string> OnAnswerReceived;
        
        internal static void Init()
        {
            On.Game.Start += RegisterRPC;
            CommandManager.Instance.AddConsoleCommand(new GetLocalListCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new PushBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new GetServerListCommand());
            CommandManager.Instance.AddConsoleCommand(new PullBlueprintCommand());
        }

        private static void RegisterRPC(On.Game.orig_Start orig, Game self)
        {
            orig(self);
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_GetServerList), new Action<long, ZPackage>(RPC_PlanBuild_GetServerList));
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_PushBlueprint), new Action<long, ZPackage>(RPC_PlanBuild_PushBlueprint));
        }

        /// <summary>
        ///     When connected to a server clear current server list, register callback to the delegate and finally invoke the RPC.<br />
        ///     Per default the server list gets cached after the first load. Set useCache to false to force a refresh from the server.
        /// </summary>
        /// <param name="callback">Delegate method which gets called when the server list was received</param>
        /// <param name="useCache">Return the internal cached list after loading, defaults to true</param>
        internal static void GetServerList(Action<bool, string> callback, bool useCache = true)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (useCache && ServerList != null)
                {
                    Jotunn.Logger.LogMessage("Getting server blueprint list from cache");
                    callback?.Invoke(true, string.Empty);
                }
                else
                {
                    Jotunn.Logger.LogMessage("Requesting server blueprint list");

                    ServerList = null;
                    OnAnswerReceived += callback;

                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_PlanBuild_GetServerList), new ZPackage());
                }
            }
        }

        /// <summary>
        ///     RPC method for sending / receiving the actual blueprint lists.
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
                    try
                    {
                        ServerList = BlueprintList.FromZPackage(pkg);
                        OnAnswerReceived?.Invoke(true, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        OnAnswerReceived?.Invoke(false, ex.ToString());
                    }
                    finally
                    {
                        OnAnswerReceived = null;
                    }
                }
            }
        }

        /// <summary>
        ///     When connected to a server, register a callback and invoke the RPC for uploading 
        ///     a local blueprint to the server directory.
        /// </summary>
        /// <param name="id">ID of the blueprint</param>
        /// <param name="callback">Is called after the server responded</param>
        internal static void PushBlueprint(string id, Action<bool, string> callback)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (BlueprintManager.Instance.Blueprints.TryGetValue(id, out var blueprint))
                {
                    Jotunn.Logger.LogMessage($"Sending blueprint {id} to server");

                    OnAnswerReceived += callback;

                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_PlanBuild_PushBlueprint), blueprint.ToZPackage());
                }
            }
        }

        /// <summary>
        ///     RPC method for pushing blueprints to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pkg"></param>
        private static void RPC_PlanBuild_PushBlueprint(long sender, ZPackage pkg)
        {
            // Server receive (local game and dedicated)
            if (ZNet.instance.IsServer())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Jotunn.Logger.LogDebug($"Received blueprint from peer #{sender}");

                    // Deserialize blueprint
                    bool success = true;
                    string message = string.Empty;
                    try
                    {
                        Blueprint blueprint = Blueprint.FromZPackage(pkg);
                        success = blueprint.ToFile();
                        message = blueprint.ID;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = ex.ToString();
                    }
                    
                    // Invoke answer response
                    ZPackage ret = new ZPackage();
                    ret.Write(success);
                    ret.Write(message);
                    ZRoutedRpc.instance.InvokeRoutedRPC(
                        sender, nameof(RPC_PlanBuild_PushBlueprint), ret);
                }
            }
            // Client receive
            else
            {
                if (pkg != null && pkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Jotunn.Logger.LogDebug($"Received push answer from server");

                    // Check answer
                    bool success = pkg.ReadBool();
                    string message = pkg.ReadString();
                    try
                    {
                        if (success && ServerList != null && BlueprintManager.Instance.Blueprints.TryGetValue(message, out var bp))
                        {
                            ServerList.Add(bp.ID, bp);
                        }
                        OnAnswerReceived?.Invoke(success, message);
                    }
                    finally
                    {
                        OnAnswerReceived = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Save a blueprint from the internal server list as a local blueprint and add it to the <see cref="BlueprintManager"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool SaveBlueprint(string id)
        {
            if (ServerList == null)
            {
                return false;
            }
            if (!ServerList.TryGetValue(id, out var blueprint))
            {
                return false;
            }

            Jotunn.Logger.LogMessage($"Saving server blueprint {id}");

            if (BlueprintManager.Instance.Blueprints.ContainsKey(id))
            {
                BlueprintManager.Instance.Blueprints[id].Destroy();
                BlueprintManager.Instance.Blueprints.Remove(id);
            }

            blueprint.ToFile();
            blueprint.CreatePrefab();
            Player.m_localPlayer.UpdateKnownRecipesList();
            BlueprintManager.Instance.Blueprints.Add(blueprint.ID, blueprint);

            return true;
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
                BlueprintManager.Instance.LoadKnownBlueprints();
                BlueprintManager.Instance.RegisterKnownBlueprints();
                Console.instance.Print(BlueprintManager.Instance.Blueprints.ToString());
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
                    BlueprintManager.Instance.Blueprints[id].Destroy();
                    BlueprintManager.Instance.Blueprints.Remove(id);

                    Console.instance.Print($"Removed blueprint {id}");
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
                    Console.instance.Print($"Usage: {Name} <blueprint_name>");
                    return;
                }
                if (!ZNet.instance || ZNet.instance.IsServer())
                {
                    Console.instance.Print("Not connected");
                    return;
                }

                var id = args[0];
                PushBlueprint(id, (bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not upload blueprint: {message}");
                    }
                    else
                    {
                        Console.instance.Print($"Blueprint {id} uploaded");
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
                if (!ZNet.instance || ZNet.instance.IsServer())
                {
                    Console.instance.Print("Not connected");
                    return;
                }
                GetServerList((bool success, string message) => 
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not get server list: {message}"); 
                    }
                    else
                    {
                        Console.instance.Print(ServerList?.ToString());
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
                    Console.instance.Print($"Usage: {Name} <blueprint_name>");
                    return;
                }
                if (!ZNet.instance || ZNet.instance.IsServer())
                {
                    Console.instance.Print("Not connected");
                    return;
                }

                var id = args[0];
                GetServerList((bool success, string message) => 
                {
                    if (!success) 
                    {
                        Console.instance.Print($"Could not load blueprint: {message}");
                    }
                    else if (SaveBlueprint(id))
                    {
                        Console.instance.Print($"Loaded blueprint {id} from server.");
                    }
                });
            }
        }
    }
}

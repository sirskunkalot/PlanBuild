using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class BlueprintSync
    {
        private static Action<bool, string> OnAnswerReceived;
        
        internal static void Init()
        {
            GetLocalBlueprints();
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
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_GetServerBlueprints), new Action<long, ZPackage>(RPC_PlanBuild_GetServerBlueprints));
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_PushBlueprint), new Action<long, ZPackage>(RPC_PlanBuild_PushBlueprint));
        }

        internal static void GetLocalBlueprints()
        {
            Jotunn.Logger.LogMessage("Loading known blueprints");

            if (!Directory.Exists(BlueprintConfig.blueprintSaveDirectoryConfig.Value))
            {
                Directory.CreateDirectory(BlueprintConfig.blueprintSaveDirectoryConfig.Value);
            }

            List<string> blueprintFiles = new List<string>();
            blueprintFiles.AddRange(Directory.EnumerateFiles(BlueprintConfig.blueprintSearchDirectoryConfig.Value, "*.blueprint", SearchOption.AllDirectories));
            blueprintFiles.AddRange(Directory.EnumerateFiles(BlueprintConfig.blueprintSearchDirectoryConfig.Value, "*.vbuild", SearchOption.AllDirectories));

            blueprintFiles = blueprintFiles.Select(absolute => absolute.Replace(BepInEx.Paths.BepInExRootPath, null)).ToList();

            // Try to load all saved blueprints
            //BlueprintManager.LocalBlueprints.Clear();
            foreach (var relativeFilePath in blueprintFiles.OrderBy(x => Path.GetFileNameWithoutExtension(x)))
            {
                try
                {
                    string id = Path.GetFileNameWithoutExtension(relativeFilePath);
                    if (!BlueprintManager.LocalBlueprints.ContainsKey(id))
                    {
                        Blueprint bp = Blueprint.FromFile(relativeFilePath);
                        BlueprintManager.LocalBlueprints.Add(bp.ID, bp);
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"Could not load blueprint {relativeFilePath}: {ex}");
                }
            }
        }

        /// <summary>
        ///     When connected to a server clear current server list, register callback to the delegate and finally invoke the RPC.<br />
        ///     Per default the server list gets cached after the first load. Set useCache to false to force a refresh from the server.
        /// </summary>
        /// <param name="callback">Delegate method which gets called when the server list was received</param>
        /// <param name="useCache">Return the internal cached list after loading, defaults to true</param>
        internal static void GetServerBlueprints(Action<bool, string> callback, bool useCache = true)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (useCache && BlueprintManager.ServerBlueprints.Count() > 0)
                {
                    Jotunn.Logger.LogMessage("Getting server blueprint list from cache");
                    callback?.Invoke(true, string.Empty);
                }
                else
                {
                    Jotunn.Logger.LogMessage("Requesting server blueprint list");
                    OnAnswerReceived += callback;
                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_PlanBuild_GetServerBlueprints), new ZPackage());
                }
            }
            else
            {
                callback?.Invoke(false, "Not connected");
            }
        }

        /// <summary>
        ///     RPC method for sending / receiving the actual blueprint lists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pkg"></param>
        private static void RPC_PlanBuild_GetServerBlueprints(long sender, ZPackage pkg)
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
                    GetLocalBlueprints();
                    ZRoutedRpc.instance.InvokeRoutedRPC(
                        sender, nameof(RPC_PlanBuild_GetServerBlueprints), BlueprintManager.LocalBlueprints.ToZPackage());
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
                        BlueprintManager.ServerBlueprints.Clear();
                        BlueprintManager.ServerBlueprints = BlueprintDictionary.FromZPackage(pkg, BlueprintLocation.Server);
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
                if (BlueprintManager.LocalBlueprints.TryGetValue(id, out var blueprint))
                {
                    Jotunn.Logger.LogMessage($"Sending blueprint {id} to server");
                    OnAnswerReceived += callback;
                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_PlanBuild_PushBlueprint), blueprint.ToZPackage());
                }
            }
            else
            {
                callback?.Invoke(false, "Not connected");
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
                    ZPackage package = new ZPackage();
                    package.Write(success);
                    package.Write(message);
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_PlanBuild_PushBlueprint), package);
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
                        if (success && BlueprintManager.LocalBlueprints.TryGetValue(message, out var bp))
                        {
                            BlueprintManager.ServerBlueprints.Add(bp.ID, bp);
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
        internal static bool PullBlueprint(string id)
        {
            if (BlueprintManager.ServerBlueprints == null)
            {
                return false;
            }
            if (!BlueprintManager.ServerBlueprints.TryGetValue(id, out var blueprint))
            {
                return false;
            }

            Jotunn.Logger.LogMessage($"Saving server blueprint {id}");

            if (BlueprintManager.LocalBlueprints.ContainsKey(id))
            {
                BlueprintManager.LocalBlueprints[id].Destroy();
                BlueprintManager.LocalBlueprints.Remove(id);
            }

            blueprint.ToFile();
            blueprint.CreatePrefab();
            Player.m_localPlayer.UpdateKnownRecipesList();
            BlueprintManager.LocalBlueprints.Add(blueprint.ID, blueprint);

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
                GetLocalBlueprints();
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
                if (BlueprintManager.LocalBlueprints.ContainsKey(id))
                {
                    BlueprintManager.LocalBlueprints[id].Destroy();
                    BlueprintManager.LocalBlueprints.Remove(id);

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
                PushBlueprint(id, (bool success, string message) =>
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
                GetServerBlueprints((bool success, string message) => 
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
                GetServerBlueprints((bool success, string message) => 
                {
                    if (!success) 
                    {
                        Console.instance.Print($"Could not load blueprint: {message}\n");
                    }
                    else if (PullBlueprint(id))
                    {
                        Console.instance.Print($"Loaded blueprint {id} from server\n");
                    }
                });
            }
        }
    }
}

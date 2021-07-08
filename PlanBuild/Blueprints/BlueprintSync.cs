using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn;

namespace PlanBuild.Blueprints
{
    internal class BlueprintSync
    {
        private static Action<bool, string> OnAnswerReceived;

        public static void Init()
        {
            GetLocalBlueprints();
            On.Game.Start += RegisterRPC;
        }

        private static void RegisterRPC(On.Game.orig_Start orig, Game self)
        {
            orig(self);
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_GetServerBlueprints), new Action<long, ZPackage>(RPC_PlanBuild_GetServerBlueprints));
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_PushBlueprint), new Action<long, ZPackage>(RPC_PlanBuild_PushBlueprint));
            ZRoutedRpc.instance.Register(nameof(RPC_PlanBuild_RemoveServerBlueprint), new Action<long, ZPackage>(RPC_PlanBuild_RemoveServerBlueprint));
        }

        /// <summary>
        ///     Load all local blueprints from disk. Searches recursively for .blueprint and .vbuild files inside 
        ///     the directory configured in the mods config file.
        /// </summary>
        internal static void GetLocalBlueprints()
        {
            Logger.LogMessage("Loading known blueprints");

            if (!Directory.Exists(BlueprintConfig.blueprintSaveDirectoryConfig.Value))
            {
                Directory.CreateDirectory(BlueprintConfig.blueprintSaveDirectoryConfig.Value);
            }

            List<string> blueprintFiles = new List<string>();
            blueprintFiles.AddRange(Directory.EnumerateFiles(BlueprintConfig.blueprintSearchDirectoryConfig.Value, "*.blueprint", SearchOption.AllDirectories));
            blueprintFiles.AddRange(Directory.EnumerateFiles(BlueprintConfig.blueprintSearchDirectoryConfig.Value, "*.vbuild", SearchOption.AllDirectories));

            blueprintFiles = blueprintFiles.Select(absolute => absolute.Replace(BepInEx.Paths.BepInExRootPath, null)).ToList();

            // Try to load all saved blueprints
            foreach (var relativeFilePath in blueprintFiles)
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
                    Logger.LogWarning($"Could not load blueprint {relativeFilePath}: {ex}");
                }
            }

            // Reload GUI if available
            BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);
        }

        /// <summary>
        ///     When connected to a server clear current server list, register callback to the delegate and finally invoke the RPC.<br />
        ///     Per default the server list gets cached after the first load. Set useCache to false to force a refresh from the server.
        /// </summary>
        /// <param name="callback">Delegate method which gets called when the server list was received</param>
        /// <param name="useCache">Return the internal cached list after loading, defaults to true</param>
        internal static void GetServerBlueprints(Action<bool, string> callback, bool useCache = true)
        {
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                callback?.Invoke(false, "Server blueprints disabled");
            }
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (useCache && BlueprintManager.ServerBlueprints.Count() > 0)
                {
                    Logger.LogMessage("Getting server blueprint list from cache");
                    callback?.Invoke(true, string.Empty);
                }
                else
                {
                    Logger.LogMessage("Requesting server blueprint list");
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
            // Globally disabled
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                return;
            }
            // Server receive (local game and dedicated)
            if (ZNet.instance.IsServer())
            {
                // Validate peer
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogDebug($"Sending blueprint data to peer #{sender}");

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
                    Logger.LogDebug("Received blueprints from server");

                    // Deserialize list, call delegates and finally clear delegates
                    bool success = true;
                    string message = string.Empty;
                    try
                    {
                        BlueprintManager.ServerBlueprints.Clear();
                        BlueprintManager.ServerBlueprints = BlueprintDictionary.FromZPackage(pkg, BlueprintLocation.Server);
                        BlueprintGUI.ReloadBlueprints(BlueprintLocation.Server);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = ex.Message;
                    }
                    finally
                    {
                        OnAnswerReceived?.Invoke(success, message);
                        OnAnswerReceived = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Write the local blueprint to disk again
        /// </summary>
        /// <param name="id">ID of the blueprint</param>
        /// <returns>true if the blueprint could be written to disk</returns>
        internal static bool SaveLocalBlueprint(string id)
        {
            if (BlueprintManager.LocalBlueprints == null)
            {
                return false;
            }
            if (!BlueprintManager.LocalBlueprints.TryGetValue(id, out var blueprint))
            {
                return false;
            }

            Logger.LogMessage($"Saving local blueprint {id}");

            blueprint.ToFile();
            BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);
            
            return true;
        }

        /// <summary>
        ///     Save a blueprint from the internal server list as a local blueprint and add it to the <see cref="BlueprintManager"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool SaveServerBlueprint(string id)
        {
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                return false;
            }
            if (BlueprintManager.ServerBlueprints == null)
            {
                return false;
            }
            if (!BlueprintManager.ServerBlueprints.TryGetValue(id, out var bp))
            {
                return false;
            }

            Logger.LogDebug($"Saving server blueprint {id}");

            if (BlueprintManager.LocalBlueprints.ContainsKey(id))
            {
                BlueprintManager.LocalBlueprints[id].DestroyBlueprint();
                BlueprintManager.LocalBlueprints.Remove(id);
            }

            bp.ToFile();
            bp.CreatePiece();
            BlueprintManager.LocalBlueprints.Add(bp.ID, bp);
            BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);

            return true;
        }

        /// <summary>
        ///     When connected to a server, register a callback and invoke the RPC for uploading 
        ///     a local blueprint to the server directory.
        /// </summary>
        /// <param name="id">ID of the blueprint</param>
        /// <param name="callback">Is called after the server responded</param>
        internal static void PushLocalBlueprint(string id, Action<bool, string> callback)
        {
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                callback?.Invoke(false, "Server blueprints disabled");
            }
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                // TODO: this needs a flag is it is local or server push
                if (BlueprintManager.LocalBlueprints.TryGetValue(id, out var blueprint))
                {
                    Logger.LogMessage($"Sending blueprint {id} to server");
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
        ///     When connected to a server, register a callback and invoke the RPC for uploading 
        ///     a local blueprint to the server directory.
        /// </summary>
        /// <param name="id">ID of the blueprint</param>
        /// <param name="callback">Is called after the server responded</param>
        internal static void PushServerBlueprint(string id, Action<bool, string> callback)
        {
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                callback?.Invoke(false, "Server blueprints disabled");
            }
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                // TODO: this needs a flag is it is local or server push
                if (BlueprintManager.ServerBlueprints.TryGetValue(id, out var blueprint))
                {
                    Logger.LogMessage($"Sending blueprint {id} to server");
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
            // Globally disabled
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                return;
            }
            // Server receive (local game and dedicated)
            if (ZNet.instance.IsServer())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogDebug($"Received blueprint from peer #{sender}");

                    // Deserialize blueprint
                    bool success = true;
                    string message = string.Empty;
                    try
                    {
                        Blueprint bp = Blueprint.FromZPackage(pkg);
                        if (BlueprintManager.LocalBlueprints.ContainsKey(bp.ID) && !ZNet.instance.IsAdmin(sender))
                        {
                            throw new Exception($"Only admins can overwrite server blueprints");
                        }
                        if (!bp.ToFile())
                        {
                            throw new Exception("Could not save blueprint");
                        }
                        if (BlueprintManager.LocalBlueprints.ContainsKey(bp.ID))
                        {
                            BlueprintManager.LocalBlueprints.Remove(bp.ID);
                        }
                        BlueprintManager.LocalBlueprints.Add(bp.ID, bp);
                        message = bp.ID;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = ex.Message;
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
                    Logger.LogDebug($"Received push answer from server");

                    // Check answer
                    bool success = pkg.ReadBool();
                    string message = pkg.ReadString();
                    try
                    {
                        if (success)
                        {
                            // TODO: this needs a flag if it was a local or server push
                            if (!BlueprintManager.ServerBlueprints.ContainsKey(message))
                            {
                                BlueprintManager.LocalBlueprints.TryGetValue(message, out var bp);
                                BlueprintManager.ServerBlueprints.Add(bp.ID, bp);
                                BlueprintGUI.ReloadBlueprints(BlueprintLocation.Server);
                            }
                        }
                    }
                    finally
                    {
                        OnAnswerReceived?.Invoke(success, message);
                        OnAnswerReceived = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Delete a local blueprint from the game and filesystem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool RemoveLocalBlueprint(string id)
        {
            if (BlueprintManager.LocalBlueprints == null)
            {
                return false;
            }
            if (!BlueprintManager.LocalBlueprints.TryGetValue(id, out var bp))
            {
                return false;
            }

            Logger.LogDebug($"Removing local blueprint {id}");

            bp.DestroyBlueprint();
            BlueprintManager.LocalBlueprints.Remove(id);
            BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);

            return true;
        }

        /// <summary>
        ///     When connected to a server, register a callback and invoke the RPC for removing 
        ///     a blueprint from the server directory. Only callable by admins on the server.
        /// </summary>
        /// <param name="id">ID of the blueprint</param>
        /// <param name="callback">Is called after the server responded</param>
        internal static void RemoveServerBlueprint(string id, Action<bool, string> callback)
        {
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                callback?.Invoke(false, "Server blueprints disabled");
            }
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
            {
                if (BlueprintManager.ServerBlueprints.TryGetValue(id, out var blueprint))
                {
                    Logger.LogMessage($"Removing blueprint {id} from server");
                    OnAnswerReceived += callback;
                    ZPackage package = new ZPackage();
                    package.Write(id);
                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_PlanBuild_RemoveServerBlueprint), package);
                }
            }
            else
            {
                callback?.Invoke(false, "Not connected");
            }
        }

        /// <summary>
        ///     RPC method for removing blueprints from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pkg"></param>
        private static void RPC_PlanBuild_RemoveServerBlueprint(long sender, ZPackage pkg)
        {
            // Globally disabled
            if (!BlueprintConfig.allowServerBlueprints.Value)
            {
                return;
            }
            // Server receive (local game and dedicated)
            if (ZNet.instance.IsServer())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogDebug($"Received blueprint removal request from peer #{sender}");

                    // Remove blueprint
                    bool success = true;
                    string message = string.Empty;
                    try
                    {
                        string id = pkg.ReadString();
                        if (BlueprintManager.LocalBlueprints.ContainsKey(id) && !ZNet.instance.IsAdmin(sender))
                        {
                            throw new Exception($"Only admins can remove server blueprints");
                        }
                        if (BlueprintManager.LocalBlueprints.TryGetValue(id, out var blueprint))
                        {
                            Logger.LogMessage($"Removing blueprint {id} from server");
                            blueprint.DestroyBlueprint();
                            BlueprintManager.LocalBlueprints.Remove(id);
                        }
                        message = id;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = ex.Message;
                    }

                    // Invoke answer response
                    ZPackage package = new ZPackage();
                    package.Write(success);
                    package.Write(message);
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_PlanBuild_RemoveServerBlueprint), package);
                }
            }
            // Client receive
            else
            {
                if (pkg != null && pkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Logger.LogDebug($"Received remove answer from server");

                    // Check answer
                    bool success = pkg.ReadBool();
                    string message = pkg.ReadString();
                    try
                    {
                        if (success)
                        {
                            if (BlueprintManager.ServerBlueprints.ContainsKey(message))
                            {
                                BlueprintManager.ServerBlueprints.Remove(message);
                                BlueprintGUI.ReloadBlueprints(BlueprintLocation.Server);
                            }
                        }
                    }
                    finally
                    {
                        OnAnswerReceived?.Invoke(success, message);
                        OnAnswerReceived = null;
                    }
                }
            }
        }
    }
}

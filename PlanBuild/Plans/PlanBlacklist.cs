using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace PlanBuild.Plans
{
    /// <summary>
    ///     Planned piece blacklist for non-admin users
    /// </summary>
    internal class PlanBlacklist
    {
        private static readonly List<string> Names = new List<string>();
        private static readonly List<int> Hashes = new List<int>();

        public static void Init()
        {
            Reload();
            SynchronizationManager.OnConfigurationSynchronized += (sender, args) => Reload();
            SynchronizationManager.OnAdminStatusChanged += PlanManager.UpdateKnownRecipes;
        }
        
        public static void Reload()
        {
            Names.Clear();
            Hashes.Clear();
            foreach (var prefabName in Config.PlanBlacklistConfig.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
            {
                int hash = prefabName.GetStableHashCode();
                if (Hashes.Contains(hash))
                {
                    continue;
                }

                Jotunn.Logger.LogDebug($"Adding {prefabName} to plan blacklist");
                Names.Add(prefabName);
                Hashes.Add(hash);
            }

            PlanManager.UpdateKnownRecipes();
        }

        public static List<string> GetNames()
        {
            return Names.OrderBy(x => x).ToList();
        }

        public static void Add(string prefabName)
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                return;
            }

            int hash = prefabName.Trim().GetStableHashCode();
            if (Hashes.Contains(hash))
            {
                return;
            }

            Names.Add(prefabName);
            Hashes.Add(hash);

            Config.PlanBlacklistConfig.Value = Names.OrderBy(x => x).Join();
            PlanBuildPlugin.Instance.Config.Reload();
        }
        
        public static void Remove(string prefabName)
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                return;
            }

            int hash = prefabName.Trim().GetStableHashCode();
            if (!Hashes.Contains(hash))
            {
                return;
            }

            Names.Remove(prefabName);
            Hashes.Remove(hash);

            Config.PlanBlacklistConfig.Value = Names.OrderBy(x => x).Join();
            PlanBuildPlugin.Instance.Config.Reload();
        }

        public static bool Contains(PlanPiecePrefab planPiecePrefab)
        {
            if (SynchronizationManager.Instance.PlayerIsAdmin)
            {
                return false;
            }

            if (!planPiecePrefab.OriginalPiece)
            {
                return false;
            }
            
            return Hashes.Contains(planPiecePrefab.OriginalHash);
        }

        public static bool Contains(Piece piece)
        {
            if (SynchronizationManager.Instance.PlayerIsAdmin)
            {
                return false;
            }

            if (!piece)
            {
                return false;
            }

            int hash = piece.name.Split('(')[0].Trim().GetStableHashCode();

            return Hashes.Contains(hash);
        }

        public static bool Contains(string pieceName)
        {
            if (SynchronizationManager.Instance.PlayerIsAdmin)
            {
                return false;
            }

            if (string.IsNullOrEmpty(pieceName))
            {
                return false;
            }
            
            int hash = pieceName.GetStableHashCode();
            
            return Hashes.Contains(hash);
        }
    }
}

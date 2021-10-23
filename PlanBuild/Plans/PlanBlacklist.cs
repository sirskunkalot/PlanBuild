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
            SynchronizationManager.OnAdminStatusChanged += () => Player.m_localPlayer?.UpdateKnownRecipesList();
        }
        
        public static void Reload()
        {
            Names.Clear();
            Hashes.Clear();
            foreach (var prefabName in PlanConfig.PlanBlacklistConfig.Value.Split(',').Where(x => !string.IsNullOrEmpty(x.Trim())))
            {
                int hash = prefabName.Trim().GetStableHashCode();
                if (Hashes.Contains(hash))
                {
                    continue;
                }

                Jotunn.Logger.LogDebug($"Adding {prefabName} to plan blacklist");
                Names.Add(prefabName);
                Hashes.Add(hash);
            }

            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.UpdateKnownRecipesList();
            }
        }

        public static string GetNames()
        {
            return Names.OrderBy(x => x).Join();
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

            PlanConfig.PlanBlacklistConfig.Value = Names.Join();
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
    }
}

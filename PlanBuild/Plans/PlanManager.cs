using HarmonyLib;
using Jotunn.Managers;
using PlanBuild.Blueprints;
using PlanBuild.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal static class PlanManager
    {
        internal static void Init()
        {
            Logger.LogInfo("Initializing PlanManager");
            
            // Init blacklist
            PlanBlacklist.Init();

            // Init commands
            PlanCommands.Init();

            // Harmony patches (see patch methods below)
            Patches.Harmony.PatchAll(typeof(PlanManager));
        }
        
        public static void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            Logger.LogDebug("Updating known Recipes");
            foreach (PlanPiecePrefab planPiece in PlanDB.Instance.GetPlanPiecePrefabs())
            {
                if (PlanBlacklist.Contains(planPiece) ||
                    (!Config.ShowAllPieces.Value && !PlayerKnowsPiece(player, planPiece.OriginalPiece)))
                {
                    if (player.m_knownRecipes.Contains(planPiece.Piece.m_name))
                    {
                        player.m_knownRecipes.Remove(planPiece.Piece.m_name);
                        Logger.LogDebug($"Removing planned piece from m_knownRecipes: {planPiece.Piece.m_name}");
                    }
                }
                else if (!player.m_knownRecipes.Contains(planPiece.Piece.m_name))
                {
                    player.m_knownRecipes.Add(planPiece.Piece.m_name);
                    Logger.LogDebug($"Adding planned piece to m_knownRecipes: {planPiece.Piece.m_name}");
                }
            }

            PieceManager.Instance.GetPieceTable(PlanHammerPrefab.PieceTableName)
                .UpdateAvailable(player.m_knownRecipes, player, true, false);
        }

        public static void UpdateAllPlanPieceTextures()
        {
            Player self = Player.m_localPlayer;
            if (self && self.m_placementGhost &&
                (self.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName) ||
                 self.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix)))
            {
                if (PlanCrystalPrefab.ShowRealTextures || !Config.ConfigTransparentGhostPlacement.Value)
                {
                    ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Skuld);
                }
                else
                {
                    ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Supported);
                }
            }
            foreach (PlanPiece planPiece in Object.FindObjectsByType<PlanPiece>(FindObjectsSortMode.None))
            {
                planPiece.UpdateTextures();
            }
        }

        public static void UpdateAllPlanTotems()
        {
            PlanTotemPrefab.UpdateGlowColor(PlanTotemPrefab.PlanTotemKitbash?.Prefab);
            foreach (PlanTotem planTotem in PlanTotem.m_allPlanTotems)
            {
                PlanTotemPrefab.UpdateGlowColor(planTotem.gameObject);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownPiece))]
        [HarmonyPrefix]
        private static bool Player_AddKnownPiece_Prefix(Piece piece)
        {
            if (piece.name.EndsWith(PlanPiecePrefab.PlannedSuffix))
            {
#if DEBUG
                Jotunn.Logger.LogDebug($"Prevent notification for {piece.name}");
#endif
                Player.m_localPlayer.m_knownRecipes.Add(piece.m_name);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Check if the player knows this piece
        ///     Has some additional handling for pieces with duplicate m_name
        /// </summary>
        /// <param name="player"></param>
        /// <param name="originalPiece"></param>
        /// <returns></returns>
        private static bool PlayerKnowsPiece(Player player, Piece originalPiece)
        {
            if (!PlanDB.Instance.FindOriginalByPieceName(originalPiece.m_name, out List<Piece> originalPieces))
            {
                return player.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
            }
            foreach (Piece piece in originalPieces)
            {
                if (player.HaveRequirements(piece, Player.RequirementMode.IsKnown))
                {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        private static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            try
            {
                if (piece && PlanDB.Instance.FindOriginalByPrefabName(piece.gameObject.name, out Piece originalPiece))
                {
                    if (PlanBlacklist.Contains(originalPiece))
                    {
                        __result = false;
                        return false;
                    }
                    if (Config.ShowAllPieces.Value)
                    {
                        __result = true;
                        return false;
                    }
                    __result = __instance.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Error while executing Player.HaveRequirements({piece},{mode}): {e}");
            }
            return true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
        [HarmonyPostfix]
        private static void Player_SetupPlacementGhost_Postfix(Player __instance)
        {
            if (!__instance.m_placementGhost)
            {
                return;
            }

            if (PlanCrystalPrefab.ShowRealTextures)
            {
                ShaderHelper.UpdateTextures(__instance.m_placementGhost, ShaderHelper.ShaderState.Skuld);
            }
            else if (Config.ConfigTransparentGhostPlacement.Value
                     && (__instance.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName)
                         || __instance.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix))
            )
            {
                ShaderHelper.UpdateTextures(__instance.m_placementGhost, ShaderHelper.ShaderState.Supported);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
        [HarmonyFinalizer]
        private static Exception Player_SetupPlacementGhost_Finalizer(Exception __exception)
        {
            // Swallowing matches the try/catch the pre-Harmony hook wrapped around the whole
            // call - a throwing SetupPlacementGhost would otherwise break UpdatePlacement.
            if (__exception != null)
            {
                Logger.LogWarning($"Exception caught while executing Player.SetupPlacementGhost(): {__exception}");
            }

            return null;
        }

        // VisEquipment.m_rightItem is a hash of the equipped item's drop prefab name (int), not
        // the name itself, so it must be compared against the hashed name, not the raw string
        private static readonly int PlanHammerNameHash = PlanHammerPrefab.PlanHammerName.GetStableHashCode();

        [HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
        [HarmonyPrefix]
        private static bool Player_CheckCanRemovePiece_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            var planHammer = __instance.m_visEquipment.m_rightItem == PlanHammerNameHash;
            var planPiece = piece.TryGetComponent<PlanPiece>(out _);

            if (planHammer)
            {
                __result = planPiece;
                return false;
            }

            if (planPiece)
            {
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Vanilla's copy-the-hovered-piece matches by prefab name against the equipped table, so
        ///     the Plan Hammer only ever finds plans. Point a real piece at its plan instead.
        ///     Player.SetSelectedPiece(Piece) is the only caller, which covers both the copy keybind
        ///     and picking a piece in the build menu.
        /// </summary>
        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.GetPieceIndex))]
        [HarmonyPrefix]
        private static void PieceTable_GetPieceIndex_Prefix(PieceTable __instance, ref Piece p)
        {
            if (p && __instance.name == PlanHammerPrefab.PieceTableName &&
                PlanDB.Instance.FindPlanByPrefabName(p.gameObject.name, out PlanPiecePrefab planPrefab))
            {
                p = planPrefab.Piece;
            }
        }

        [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start))]
        [HarmonyPostfix]
        private static void DungeonDB_Start_Postfix()
        {
            PlanDB.Instance.ScanPieceTables();
        }
    }
}
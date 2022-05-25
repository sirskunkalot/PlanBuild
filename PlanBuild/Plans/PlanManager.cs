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

            // Hooks
            On.DungeonDB.Start += (orig, self) =>
            {
                orig(self);
                PlanDB.Instance.ScanPieceTables();
            };
            On.Player.AddKnownPiece += Player_AddKnownPiece;
            On.Player.HaveRequirements_Piece_RequirementMode += Player_HaveRequirements;
            On.Player.SetupPlacementGhost += Player_SetupPlacementGhost;
            On.Player.CheckCanRemovePiece += Player_CheckCanRemovePiece;
            On.WearNTear.Highlight += WearNTear_Highlight;
            On.WearNTear.Destroy += WearNTear_Destroy;
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
            foreach (PlanPiece planPiece in Object.FindObjectsOfType<PlanPiece>())
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

        private static void Player_AddKnownPiece(On.Player.orig_AddKnownPiece orig, Player self, Piece piece)
        {
            if (piece.name.EndsWith(PlanPiecePrefab.PlannedSuffix))
            {
#if DEBUG
                Jotunn.Logger.LogDebug($"Prevent notification for {piece.name}");
#endif
                Player.m_localPlayer.m_knownRecipes.Add(piece.m_name);
                return;
            }

            orig(self, piece);
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

        private static bool Player_HaveRequirements(On.Player.orig_HaveRequirements_Piece_RequirementMode orig, Player self, Piece piece, Player.RequirementMode mode)
        {
            try
            {
                if (piece && PlanDB.Instance.FindOriginalByPrefabName(piece.gameObject.name, out Piece originalPiece))
                {
                    if (PlanBlacklist.Contains(originalPiece))
                    {
                        return false;
                    }
                    if (Config.ShowAllPieces.Value)
                    {
                        return true;
                    }
                    return self.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Error while executing Player.HaveRequirements({piece},{mode}): {e}");
            }
            return orig(self, piece, mode);
        }

        private static void Player_SetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
        {
            try
            {
                PlanPiece.m_forceDisableInit = true;
                orig(self);
                if (self.m_placementGhost)
                {
                    if (PlanCrystalPrefab.ShowRealTextures)
                    {
                        ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Skuld);
                    }
                    else if (Config.ConfigTransparentGhostPlacement.Value
                             && (self.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName)
                                 || self.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix))
                    )
                    {
                        ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Supported);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while executing Player.SetupPlacementGhost(): {ex}");
            }
            finally
            {
                PlanPiece.m_forceDisableInit = false;
            }
        }
        
        private static bool Player_CheckCanRemovePiece(On.Player.orig_CheckCanRemovePiece orig, Player self, Piece piece)
        {
            var planHammer = self.m_visEquipment.m_rightItem.Equals(PlanHammerPrefab.PlanHammerName);
            var planPiece = piece.TryGetComponent<PlanPiece>(out _);

            if (planHammer)
            {
                return planPiece;
            }

            if (planPiece)
            {
                return false;
            }

            return orig(self, piece);
        }

        private static void WearNTear_Highlight(On.WearNTear.orig_Highlight orig, WearNTear self)
        {
            if (!PlanCrystalPrefab.ShowRealTextures && self.TryGetComponent(out PlanPiece planPiece))
            {
                planPiece.Highlight();
                return;
            }
            orig(self);
        }
        
        private static void WearNTear_Destroy(On.WearNTear.orig_Destroy orig, WearNTear wearNTear)
        {
            // Check if actually destoyed, not removed by middle clicking with Hammer
            if (wearNTear.m_nview && wearNTear.m_nview.IsOwner()
                                  && wearNTear.GetHealthPercentage() <= 0f
                                  && PlanDB.Instance.FindPlanByPrefabName(wearNTear.name, out PlanPiecePrefab planPrefab))
            {
                foreach (PlanTotem planTotem in PlanTotem.m_allPlanTotems)
                {
                    if (!planTotem.GetEnabled())
                    {
                        continue;
                    }
                    GameObject gameObject = wearNTear.gameObject;
                    if (planTotem.InRange(gameObject))
                    {
                        planTotem.Replace(gameObject, planPrefab);
                        break;
                    }
                }
            }
            orig(wearNTear);
        }
    }
}
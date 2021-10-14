using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints;
using PlanBuild.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal class PlanManager
    {
        private static PlanManager _instance;

        public static PlanManager Instance
        {
            get
            {
                return _instance ??= new PlanManager();
            }
        }

        internal void Init()
        {
            // Init config
            PlanConfig.Init();

            // Hooks
            PieceManager.OnPiecesRegistered += CreatePlanTable;
            SceneManager.sceneLoaded += OnSceneLoaded;

            On.Player.UpdateKnownRecipesList += OnPlayerUpdateKnownRecipesList;
            On.Player.HaveRequirements_Piece_RequirementMode += OnHaveRequirements;
            On.Player.SetupPlacementGhost += OnSetupPlacementGhost;
            On.WearNTear.Highlight += OnHighlight;
            On.WearNTear.Destroy += OnDestroy;
        }

        private void OnDestroy(On.WearNTear.orig_Destroy orig, WearNTear wearNTear)
        {
            //Check if actually destoyed, not removed by middle clicking with Hammer
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
                    UnityEngine.GameObject gameObject = wearNTear.gameObject;
                    if (planTotem.InRange(gameObject))
                    {
                        planTotem.Replace(gameObject, planPrefab);
                        break;
                    }
                }
            }
            orig(wearNTear);
        }

        private void CreatePlanTable()
        {
            // Create plan piece table for the plan mode
            var categories = PieceManager.Instance.GetPieceCategories()
                .Where(x => x != BlueprintAssets.CategoryBlueprints && x != BlueprintAssets.CategoryTools);

            CustomPieceTable planPieceTable = new CustomPieceTable(
                PlanPiecePrefab.PieceTableName,
                new PieceTableConfig()
                {
                    CanRemovePieces = true,
                    UseCategories = true,
                    UseCustomCategories = true,
                    CustomCategories = categories.ToArray()
                }
            );
            PieceManager.Instance.AddPieceTable(planPieceTable);

            // Add empty lists up to the max categories count
            for (int i = planPieceTable.PieceTable.m_availablePieces.Count; i < (int)Piece.PieceCategory.All; i++)
            {
                planPieceTable.PieceTable.m_availablePieces.Add(new List<Piece>());
            }

            // Resize selectedPiece array
            Array.Resize(ref planPieceTable.PieceTable.m_selectedPiece, planPieceTable.PieceTable.m_availablePieces.Count);

            // Set table on the rune
            ItemDrop rune = ItemManager.Instance.GetItem(BlueprintAssets.BlueprintRuneName).ItemDrop;
            rune.m_itemData.m_shared.m_buildPieces = planPieceTable.PieceTable;

            // Needs to run only once
            PieceManager.OnPiecesRegistered -= CreatePlanTable;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scan piece tables after scene has loaded to catch non-Jötunn mods, too
            if (scene.name == "main")
            {
                PlanDB.Instance.ScanPieceTables();
            }
        }

        private void OnPlayerUpdateKnownRecipesList(On.Player.orig_UpdateKnownRecipesList orig, Player self)
        {
            // Prefix the recipe loading for the plans to avoid spamming unlock messages
            UpdateKnownRecipes();
            orig(self);
        }

        public void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            Logger.LogDebug("Updating known Recipes");
            foreach (PlanPiecePrefab planPiece in PlanDB.Instance.GetPlanPiecePrefabs())
            {
                if (!PlanConfig.ShowAllPieces.Value && !PlayerKnowsPiece(player, planPiece.OriginalPiece))
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

            PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PieceTableName)
                .UpdateAvailable(player.m_knownRecipes, player, true, false);
        }

        /// <summary>
        ///     Check if the player knows this piece
        ///     Has some additional handling for pieces with duplicate m_name
        /// </summary>
        /// <param name="player"></param>
        /// <param name="originalPiece"></param>
        /// <returns></returns>
        private bool PlayerKnowsPiece(Player player, Piece originalPiece)
        {
            if (PlanDB.Instance.FindByPieceName(originalPiece.m_name, out List<Piece> originalPieces))
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

        private bool OnHaveRequirements(On.Player.orig_HaveRequirements_Piece_RequirementMode orig, Player self, Piece piece, Player.RequirementMode mode)
        {
            try
            {
                if (piece && !PlanConfig.ShowAllPieces.Value && PlanDB.Instance.FindOriginalByPrefabName(piece.gameObject.name, out Piece originalPiece))
                {
                    return self.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                }
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"Error while executing Player.HaveRequirements({piece},{mode}): {e}");
            }
            return orig(self, piece, mode);
        }

        private void OnSetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
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
                    else if (PlanConfig.ConfigTransparentGhostPlacement.Value
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
                Jotunn.Logger.LogWarning($"Exception caught while executing Player.SetupPlacementGhost(): {ex}");
            }
            finally
            {
                PlanPiece.m_forceDisableInit = false;
            }
        }

        private void OnHighlight(On.WearNTear.orig_Highlight orig, WearNTear self)
        {
            if (!PlanCrystalPrefab.ShowRealTextures && self.TryGetComponent(out PlanPiece planPiece))
            {
                planPiece.Highlight();
                return;
            }
            orig(self);
        }

        public void UpdateAllPlanPieceTextures()
        {
            Player self = Player.m_localPlayer;
            if (self && self.m_placementGhost &&
                (self.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName) ||
                 self.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix)))
            {
                if (PlanCrystalPrefab.ShowRealTextures || !PlanConfig.ConfigTransparentGhostPlacement.Value)
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

        public void UpdateAllPlanTotems()
        {
            PlanTotemPrefab.UpdateGlowColor(PlanTotemPrefab.PlanTotemKitbash?.Prefab);
            foreach (PlanTotem planTotem in PlanTotem.m_allPlanTotems)
            {
                PlanTotemPrefab.UpdateGlowColor(planTotem.gameObject);
            }
        }
    }
}
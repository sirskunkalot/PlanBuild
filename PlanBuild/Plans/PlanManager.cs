using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints;
using PlanBuild.Utils;
using UnityEngine;
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

        public static ConfigEntry<bool> ShowAllPieces;
        public static readonly Dictionary<string, Piece> PlanToOriginalMap = new Dictionary<string, Piece>();
        public readonly Dictionary<string, PlanPiecePrefab> PlanPiecePrefabs = new Dictionary<string, PlanPiecePrefab>();

        internal void Init()
        {
            ShowAllPieces = PlanBuildPlugin.Instance.Config.Bind("General", "Plan unknown pieces", false, 
                new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            ShowAllPieces.SettingChanged += (_, _) => UpdateKnownRecipes();

            PieceManager.OnPiecesRegistered += CreatePlanTable;
            SceneManager.sceneLoaded += OnSceneLoaded;
            On.Player.UpdateKnownRecipesList += OnPlayerUpdateKnownRecipesList;
            On.Player.HaveRequirements_Piece_RequirementMode += OnHaveRequirements;
            On.Player.SetupPlacementGhost += OnSetupPlacementGhost;
            On.WearNTear.Highlight += OnHighlight;
        }
        
        public bool CanCreatePlan(Piece piece)
        {
            return piece.m_enabled
                   && piece.GetComponent<Plant>() == null
                   && piece.GetComponent<TerrainOp>() == null
                   && piece.GetComponent<TerrainModifier>() == null
                   && piece.GetComponent<Ship>() == null
                   && piece.GetComponent<PlanPiece>() == null
                   && !piece.name.Equals(PlanTotemPrefab.PlanTotemPieceName)
                   && !piece.name.Equals(BlueprintAssets.PieceCaptureName)
                   && !piece.name.Equals(BlueprintAssets.PieceDeletePlansName);
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
                ScanPieceTables();
            }
        }
        
        private void ScanPieceTables()
        {
            Logger.LogDebug("Scanning PieceTables for Pieces");
            PieceTable planPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PieceTableName);
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                PieceTable pieceTable = item.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_buildPieces;
                if (pieceTable == null ||
                    pieceTable.name.Equals(PlanPiecePrefab.PieceTableName) ||
                    pieceTable.name.Equals(BlueprintAssets.PieceTableName))
                {
                    continue;
                }
                foreach (GameObject piecePrefab in pieceTable.m_pieces)
                {
                    if (!piecePrefab)
                    {
                        Logger.LogWarning($"Invalid prefab in {item.name} PieceTable");
                        continue;
                    }
                    Piece piece = piecePrefab.GetComponent<Piece>();
                    if (!piece)
                    {
                        Logger.LogWarning($"Recipe in {item.name} has no Piece?! {piecePrefab.name}");
                        continue;
                    }
                    try
                    {
                        if (piece.name == "piece_repair")
                        {
                            continue;
                        }
                        if (PlanPiecePrefabs.ContainsKey(piece.name))
                        {
                            continue;
                        }
                        if (!CanCreatePlan(piece))
                        {
                            continue;
                        }
                        if (!EnsurePrefabRegistered(piece))
                        {
                            continue;
                        }

                        PlanPiecePrefab planPiece = new PlanPiecePrefab(piece);
                        PrefabManager.Instance.RegisterToZNetScene(planPiece.PiecePrefab);
                        PieceManager.Instance.AddPiece(planPiece);
                        PlanPiecePrefabs.Add(piece.name, planPiece);
                        if (!planPieceTable.m_pieces.Contains(planPiece.PiecePrefab))
                        {
                            planPieceTable.m_pieces.Add(planPiece.PiecePrefab);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Error while creating plan of {piece.name}: {e}");
                    }
                }
            }
        }

        private bool EnsurePrefabRegistered(Piece piece)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(piece.gameObject.name);
            if (prefab)
            {
                return true;
            }
            Logger.LogWarning("Piece " + piece.name + " in Hammer not fully registered? Could not find prefab " + piece.gameObject.name);
            if (!ZNetScene.instance.m_prefabs.Contains(piece.gameObject))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_prefabs! Adding now");
                ZNetScene.instance.m_prefabs.Add(piece.gameObject);
            }
            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(piece.gameObject.name.GetStableHashCode()))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_namedPrefabs! Adding now");
                ZNetScene.instance.m_namedPrefabs[piece.gameObject.name.GetStableHashCode()] = piece.gameObject;
            }
            //Prefab was added incorrectly, make sure the game doesn't delete it when logging out
            GameObject prefabParent = piece.gameObject.transform.parent?.gameObject;
            if (!prefabParent)
            {
                Logger.LogWarning(" Prefab has no parent?! Adding to Jotunn");
                PrefabManager.Instance.AddPrefab(piece.gameObject);
            }
            else if (prefabParent.scene.buildIndex != -1)
            {
                Logger.LogWarning(" Prefab container not marked as DontDestroyOnLoad! Marking now");
                Object.DontDestroyOnLoad(prefabParent);
            }
            return PrefabManager.Instance.GetPrefab(piece.gameObject.name) != null;
        }

        private void OnPlayerUpdateKnownRecipesList(On.Player.orig_UpdateKnownRecipesList orig, Player self)
        {
            // Prefix the recipe loading for the plans to avoid spamming unlock messages
            UpdateKnownRecipes();
            orig(self);
        }
        
        private void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            Logger.LogDebug("Updating known Recipes");
            foreach (PlanPiecePrefab planPiece in PlanPiecePrefabs.Values)
            {
                if (!ShowAllPieces.Value && !player.HaveRequirements(planPiece.OriginalPiece, Player.RequirementMode.IsKnown))
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

        private bool OnHaveRequirements(On.Player.orig_HaveRequirements_Piece_RequirementMode orig, Player self, Piece piece, Player.RequirementMode mode)
        {
            try
            {
                if (piece && !ShowAllPieces.Value && PlanToOriginalMap.TryGetValue(piece.gameObject.name, out Piece originalPiece))
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
            PlanPiece.m_forceDisableInit = true;
            orig(self);
            if (self.m_placementGhost)
            {
                if (PlanCrystalPrefab.ShowRealTextures)
                {
                    ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Skuld);
                }
                else if (PlanBuildPlugin.ConfigTransparentGhostPlacement.Value
                         && (self.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName)
                             || self.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix))
                )
                {
                    ShaderHelper.UpdateTextures(self.m_placementGhost, ShaderHelper.ShaderState.Supported);
                }
            }
            PlanPiece.m_forceDisableInit = false;
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
    }
}

using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.PlanBuild
{
    class PlanManager
    {
        private static PlanManager _instance;
        public static PlanManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PlanManager();
                return _instance;
            }
        } 

        public static ConfigEntry<bool> showAllPieces; 
        public readonly Dictionary<string, PlanPiecePrefab> planPiecePrefabs = new Dictionary<string, PlanPiecePrefab>();

        internal void Init()
        { 
            showAllPieces = PlanBuildPlugin.Instance.Config.Bind("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            showAllPieces.SettingChanged += UpdateKnownRecipes;

            On.Player.Awake += OnPlayerAwake;
        }
         
        private void UpdateKnownRecipes(object sender, EventArgs e)
        {
            UpdateKnownRecipes();
        }

        private void OnPlayerAwake(On.Player.orig_Awake orig, Player self)
        {
            orig(self);
            LateScanPieceTables();
        }

        public void TogglePlanBuildMode()
        {
            if (ScanPieceTables(lateAdd: true))
            {
                UpdateKnownRecipes();
            }
            if (Player.m_localPlayer.m_visEquipment.m_rightItem != BlueprintRunePrefab.BlueprintRuneName)
            {
                return;
            }
            ItemDrop.ItemData blueprintRune = Player.m_localPlayer.GetInventory().GetItem(BlueprintRunePrefab.BlueprintRuneItemName);
            if (blueprintRune == null)
            {
                return;
            }
            PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
            PieceTable bluePrintRunePieceTable = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
            if (blueprintRune.m_shared.m_buildPieces == planHammerPieceTable)
            {
                blueprintRune.m_shared.m_buildPieces = bluePrintRunePieceTable;
                if (blueprintRune.m_shared.m_buildPieces.m_selectedCategory == 0)
                {
                    blueprintRune.m_shared.m_buildPieces.m_selectedCategory = PieceManager.Instance.AddPieceCategory(BlueprintRunePrefab.PieceTableName, BlueprintRunePrefab.CategoryTools);
                }
            }
            else
            {
                blueprintRune.m_shared.m_buildPieces = planHammerPieceTable;
            }
            Player.m_localPlayer.UnequipItem(blueprintRune);
            Player.m_localPlayer.EquipItem(blueprintRune);

            Color color = blueprintRune.m_shared.m_buildPieces == planHammerPieceTable ? Color.red : Color.cyan;
            ShaderHelper.SetEmissionColor(Player.m_localPlayer.m_visEquipment.m_rightItemInstance, color);

            Player.m_localPlayer.UpdateAvailablePiecesList();
        }

        private void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (!showAllPieces.Value)
            {
                foreach (PlanPiecePrefab planPieceConfig in planPiecePrefabs.Values)
                {
                    if (!player.HaveRequirements(planPieceConfig.originalPiece, Player.RequirementMode.IsKnown))
                    {
#if DEBUG
                        Jotunn.Logger.LogInfo("Removing planned piece from m_knownRecipes: " + planPieceConfig.Piece.m_name);
#endif
                        player.m_knownRecipes.Remove(planPieceConfig.Piece.m_name);
                    }
#if DEBUG
                    else
                    {
                        Jotunn.Logger.LogDebug("Player knows about " + planPieceConfig.originalPiece.m_name);
                    }
#endif
                }
            }
            player.UpdateKnownRecipesList();
            PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName)
                .UpdateAvailable(player.m_knownRecipes, player, true, false);
        }


        internal bool addedRepair = false; 

        internal void InitialScanPieceTables()
        {
            try
            {
                this.ScanPieceTables(false);
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= InitialScanPieceTables;
            }
        }

        internal void LateScanPieceTables()
        {
            ScanPieceTables(true);
        }

        internal bool ScanPieceTables(bool lateAdd)
        {
            Jotunn.Logger.LogDebug("Scanning Hammer PieceTable for Pieces");
            bool addedPiece = false;
            PieceTable hammerPieceTable = PieceManager.Instance.GetPieceTable("Hammer");
            if (!hammerPieceTable) {
                return false;
            }
            string[] categoryNames = new string[Hud.instance.m_pieceCategoryTabs.Length];
            for (int i = 0; i < Hud.instance.m_pieceCategoryTabs.Length; i++)
            {
                categoryNames[i] = Hud.instance.m_pieceCategoryTabs[i].name;
            }
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                PieceTable pieceTable = item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
                if(pieceTable == null)
                {
                    continue;
                } 
                foreach (GameObject piecePrefab in pieceTable.m_pieces)
                {

                    if (!piecePrefab)
                    {
                        Jotunn.Logger.LogWarning("Invalid prefab in Hammer PieceTable");
                        continue;
                    }
                    Piece piece = piecePrefab.GetComponent<Piece>();
                    if (!piece)
                    {
                        Jotunn.Logger.LogWarning("Recipe in Hammer has no Piece?! " + piecePrefab.name);
                        continue;
                    }
                    try
                    {
                        if (piece.name == "piece_repair")
                        {
                            if (!addedRepair)
                            {
                                PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
                                if (planHammerPieceTable != null)
                                {
                                    planHammerPieceTable.m_pieces.Add(piecePrefab);
                                    addedRepair = true;
                                }
                            }
                            continue;
                        }
                        if (planPiecePrefabs.ContainsKey(piece.name))
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
                        if(planPiece.Piece.m_category > Piece.PieceCategory.Max)
                        {
                            planPiece.Piece.m_category = BlueprintRunePrefab.OtherCategory;
                        }
                        PieceManager.Instance.AddPiece(planPiece);
                        planPiecePrefabs.Add(piece.name, planPiece);
                        PrefabManager.Instance.RegisterToZNetScene(planPiece.PiecePrefab);
                        if (lateAdd)
                        {
                            PieceTable planPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
                            if (!planPieceTable.m_pieces.Contains(planPiece.PiecePrefab))
                            {
                                planPieceTable.m_pieces.Add(planPiece.PiecePrefab);
                                addedPiece = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Jotunn.Logger.LogWarning("Error while creating plan of " + piece.name + ": " + e);
                    }
                }
            }
            return addedPiece;
        }

        public static bool CanCreatePlan(Piece piece)
        {
            return piece.m_enabled
                && piece.GetComponent<Ship>() == null
                && piece.GetComponent<Plant>() == null
                && piece.GetComponent<TerrainModifier>() == null
                && piece.GetComponent<TerrainOp>() == null
                && piece.m_resources.Length != 0;
        }

        private bool EnsurePrefabRegistered(Piece piece)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(piece.gameObject.name);
            if (prefab)
            {
                return true;
            }
            Jotunn.Logger.LogWarning("Piece " + piece.name + " in Hammer not fully registered? Could not find prefab " + piece.gameObject.name);
            if (!ZNetScene.instance.m_prefabs.Contains(piece.gameObject))
            {
                Jotunn.Logger.LogWarning(" Not registered in ZNetScene.m_prefabs! Adding now");
                ZNetScene.instance.m_prefabs.Add(piece.gameObject);
            }
            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(piece.gameObject.name.GetStableHashCode()))
            {
                Jotunn.Logger.LogWarning(" Not registered in ZNetScene.m_namedPrefabs! Adding now");
                ZNetScene.instance.m_namedPrefabs[piece.gameObject.name.GetStableHashCode()] = piece.gameObject;
            }
            //Prefab was added incorrectly, make sure the game doesn't delete it when logging out
            GameObject prefabParent = piece.gameObject.transform.parent?.gameObject;
            if (!prefabParent)
            {
                Jotunn.Logger.LogWarning(" Prefab has no parent?! Adding to Jotunn");
                PrefabManager.Instance.AddPrefab(piece.gameObject);
            }
            else if (prefabParent.scene.buildIndex != -1)
            {
                Jotunn.Logger.LogWarning(" Prefab container not marked as DontDestroyOnLoad! Marking now");
                Object.DontDestroyOnLoad(prefabParent);
            }
            return PrefabManager.Instance.GetPrefab(piece.gameObject.name) != null;
        }

    }
}

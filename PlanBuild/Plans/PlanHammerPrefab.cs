using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace PlanBuild.Plans
{
    internal class PlanHammerPrefab
    {
        public const string PlanHammerName = "PlanHammer";
        public const string PlanHammerItemName = "$item_plan_hammer";
        public const string PieceTableName = "_PlanHammerPieceTable";

        public const string PieceDeletePlansName = "piece_plan_delete";

        private static Sprite HammerIcon;
        private static GameObject PieceDeletePlansPrefab;
        private static CustomItem PlanHammerItem;

        public static void Create(AssetBundle planbuildBundle)
        {
            HammerIcon = planbuildBundle.LoadAsset<Sprite>("plan_hammer");
            PieceDeletePlansPrefab = planbuildBundle.LoadAsset<GameObject>(PieceDeletePlansName);
            PrefabManager.OnVanillaPrefabsAvailable += CreatePlanHammerItem;
            PieceManager.OnPiecesRegistered += CreatePlanTable;
            GUIManager.OnCustomGUIAvailable += CreateCustomKeyHints;
        }

        private static void CreatePlanHammerItem()
        {
            try
            {
                Logger.LogDebug("Creating PlanHammer item");

                PlanHammerItem = new CustomItem(PlanHammerName, "Hammer", new ItemConfig
                {
                    Name = PlanHammerItemName,
                    Description = $"{PlanHammerItemName}_description",
                    Icons = new []
                    {
                        HammerIcon
                    },
                    Requirements = new []
                    {
                        new RequirementConfig
                        {
                            Item = "Wood",
                            Amount = 1
                        }
                    }
                });
                ItemManager.Instance.AddItem(PlanHammerItem);

                ItemDrop.ItemData.SharedData sharedData = PlanHammerItem.ItemDrop.m_itemData.m_shared;
                sharedData.m_useDurability = false;
                sharedData.m_maxQuality = 1;
                sharedData.m_weight = 0;
                sharedData.m_buildPieces = null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error caught while creating the PlanHammer item: {ex}");
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= CreatePlanHammerItem;
            }
        }

        private static void CreatePlanTable()
        {
            try
            {
                Logger.LogDebug("Creating PlanHammer piece table");

                // Create plan piece table for the plan mode
                var categories = PieceManager.Instance.GetPieceCategories()
                    .Where(x => x != BlueprintAssets.CategoryBlueprints && x != BlueprintAssets.CategoryTools);

                CustomPieceTable planPieceTable = new CustomPieceTable(
                    PieceTableName,
                    new PieceTableConfig
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
                Array.Resize(ref planPieceTable.PieceTable.m_selectedPiece,
                    planPieceTable.PieceTable.m_availablePieces.Count);

                // Set table on the hammer
                PlanHammerItem.ItemDrop.m_itemData.m_shared.m_buildPieces = planPieceTable.PieceTable;

                // Create delete tool
                PieceDeletePlansPrefab.AddComponent<DeletePlansComponent>();
                CustomPiece pieceDelete = new CustomPiece(PieceDeletePlansPrefab, PieceTableName, false);
                PieceManager.Instance.AddPiece(pieceDelete);
                PieceManager.Instance.RegisterPieceInPieceTable(PieceDeletePlansPrefab, PieceTableName, "All");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error caught while creating the PlanHammer table: {ex}");
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= CreatePlanTable;
            }
        }

        private static void CreateCustomKeyHints()
        {
            // Remove

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = PlanHammerName,
                Piece = PieceDeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_plandelete" }
                }
            });

            GUIManager.OnCustomGUIAvailable += CreateCustomKeyHints;
        }
        
        private class DeletePlansComponent : MonoBehaviour
        {
            private Piece LastHoveredPiece;
            
            private void Start()
            {
                On.Player.PieceRayTest += Player_PieceRayTest;
                On.Player.PlacePiece += Player_PlacePiece;
                Logger.LogDebug($"{gameObject.name} started");
            }
            
            private void OnDestroy()
            {
                On.Player.PlacePiece -= Player_PlacePiece;
                On.Player.PieceRayTest -= Player_PieceRayTest;
                Logger.LogDebug($"{gameObject.name} destroyed");
            }
            
            private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
            {
                bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
                LastHoveredPiece = piece;
                return result;
            }

            private bool Player_PlacePiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
            {
                if (LastHoveredPiece && LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                }
                return false;
            }
        }
    }
}

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
        public const string PieceTableName = "_PlanHammerPieceTable";
        public const string PlanHammerName = "PlanHammer";
        public const string PlanHammerItemName = "$item_plan_hammer";

        private static Sprite HammerIcon;
        private static CustomItem PlanHammerItem;

        public static void Create(AssetBundle planbuildBundle)
        {
            HammerIcon = planbuildBundle.LoadAsset<Sprite>("plan_hammer");
            PrefabManager.OnVanillaPrefabsAvailable += CreatePlanHammerItem;
            PieceManager.OnPiecesRegistered += CreatePlanTable;
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
                    Icons = new Sprite[]
                    {
                        HammerIcon
                    },
                    Requirements = new RequirementConfig[]
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
    }
}

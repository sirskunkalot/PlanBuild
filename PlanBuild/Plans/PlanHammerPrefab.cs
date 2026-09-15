using HarmonyLib;
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
            Patches.Harmony.PatchAll(typeof(PlanHammerPrefab));
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
                var categories = PieceManager.Instance.GetPieceCategoriesMap().Where(x =>
                    x.Value != BlueprintAssets.CategoryBlueprints &&
                    x.Value != BlueprintAssets.CategoryClipboard &&
                    x.Value != BlueprintAssets.CategoryTools).Select(x => x.Value).ToList();

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
                for (int i = planPieceTable.PieceTable.m_availablePiecesByCategory.Count; i < (int)Piece.PieceCategory.All; i++)
                {
                    planPieceTable.PieceTable.m_availablePiecesByCategory.Add(new List<Piece>());
                }

                // Resize selectedPiece array
                Array.Resize(ref planPieceTable.PieceTable.m_selectedPiece,
                    planPieceTable.PieceTable.m_availablePiecesByCategory.Count);

                // Set table on the hammer
                PlanHammerItem.ItemDrop.m_itemData.m_shared.m_buildPieces = planPieceTable.PieceTable;

                // Create delete tool
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

            GUIManager.OnCustomGUIAvailable -= CreateCustomKeyHints;
        }
        
        private static Piece LastHoveredPiece;

        /// <summary>
        ///     Whether the delete-plans piece is the player's currently selected/placed ghost.
        ///     Mirrors the ghost-name check PatcherGizmo.cs already uses for the same piece.
        /// </summary>
        private static bool IsDeletePlansActive(Player player)
        {
            return player.m_placementGhost
                && player.m_placementGhost.name.StartsWith(PieceDeletePlansName, StringComparison.Ordinal);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
        [HarmonyPostfix]
        private static void Player_PieceRayTest_Postfix(Player __instance, Piece piece)
        {
            if (IsDeletePlansActive(__instance))
            {
                LastHoveredPiece = piece;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        [HarmonyPrefix]
        private static bool Player_TryPlacePiece_Prefix(Player __instance, ref bool __result)
        {
            if (!IsDeletePlansActive(__instance))
            {
                return true;
            }

            if (LastHoveredPiece && LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
            {
                planPiece.m_wearNTear.Remove();
            }

            __result = false;
            return false;
        }
    }
}

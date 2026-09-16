using HarmonyLib;
using Jotunn.Managers;
using PlanBuild.Plans;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace PlanBuild.Blueprints
{
    internal static class BlueprintManager
    {
        public static BlueprintDictionary LocalBlueprints;
        public static BlueprintDictionary TemporaryBlueprints;
        public static BlueprintDictionary ServerBlueprints;

        public const float HighlightTimeout = 0.5f;
        public const float GhostTimeout = 10f;

        public static Piece LastHoveredPiece;

        private static float LastHighlightTime;
        private static float OriginalPlaceDistance;

        /// <summary>
        ///     Categories shown first in the Blueprint Rune, in this order.
        ///     Everything else follows alphabetically.
        /// </summary>
        private static readonly string[] FixedCategoryOrder =
        {
            BlueprintAssets.CategoryTools,
            BlueprintAssets.CategoryClipboard,
            BlueprintAssets.CategoryBlueprints
        };

        public static void Init()
        {
            Logger.LogInfo("Initializing BlueprintManager");

            try
            {
                // Init stuff
                LocalBlueprints = new BlueprintDictionary();
                TemporaryBlueprints = new BlueprintDictionary();
                ServerBlueprints = new BlueprintDictionary();
                Selection.Init();
                SelectionCommands.Init();
                BlueprintSync.Init();
                BlueprintCommands.Init();
                UndoManager.Instance.CreateQueue(Config.BlueprintUndoQueueNameConfig.Value);

                // Harmony patches (see patch methods below)
                Patches.Harmony.PatchAll(typeof(BlueprintManager));
                Patches.Harmony.PatchAll(typeof(Components.ToolComponentBase));

                // Ghost watchdog
                IEnumerator watchdog()
                {
                    while (true)
                    {
                        foreach (var bp in LocalBlueprints.Values.Where(x => x.GhostActiveTime > 0f))
                        {
                            if (Time.time - bp.GhostActiveTime > GhostTimeout)
                            {
                                bp.DestroyGhost();
                            }
                        }

                        yield return new WaitForSeconds(GhostTimeout);
                    }
                }

                PlanBuildPlugin.Instance.StartCoroutine(watchdog());
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error caught while initializing: {ex}");
            }
        }

        /// <summary>
        ///     Determine if a piece can be captured in a blueprint
        /// </summary>
        /// <param name="piece">Piece instance to be tested</param>
        /// <param name="onlyPlanned">When true, only pieces with the PlanPiece component return true</param>
        /// <returns></returns>
        public static bool CanCapture(Piece piece, bool onlyPlanned = false)
        {
            if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName) || piece.name.StartsWith(BlueprintAssets.PieceCenterPointName))
            {
                return true;
            }

            if (piece.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                return false;
            }

            if (!SynchronizationManager.Instance.PlayerIsAdmin && PlanBlacklist.Contains(piece))
            {
                return false;
            }

            return piece.GetComponent<PlanPiece>() != null || (!onlyPlanned && PlanDB.Instance.CanCreatePlan(piece));
        }

        /// <summary>
        ///     Get all pieces on a given position in a given radius, optionally only planned ones
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="onlyPlanned"></param>
        /// <returns></returns>
        public static List<Piece> GetPiecesInRadius(Vector3 position, float radius, bool onlyPlanned = false)
        {
            List<Piece> result = new List<Piece>();
            foreach (var piece in Piece.s_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(piecePos.x, piecePos.z)) <= radius
                    && CanCapture(piece, onlyPlanned))
                {
                    result.Add(piece);
                }
            }
            return result;
        }

        /// <summary>
        ///     "Highlights" pieces in a given radius with a given color.
        /// </summary>
        public static void HighlightPiecesInRadius(Vector3 startPosition, float radius, Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHighlightTime + HighlightTimeout)
            {
                return;
            }

            foreach (var piece in GetPiecesInRadius(startPosition, radius, onlyPlanned))
            {
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(color, HighlightTimeout + 0.1f);
                }
            }
            LastHighlightTime = Time.time;
        }

        /// <summary>
        ///     "Highlights" the last hovered piece with a given color.
        /// </summary>
        public static void HighlightHoveredPiece(Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHighlightTime + HighlightTimeout)
            {
                return;
            }

            if (LastHoveredPiece)
            {
                if (onlyPlanned && !LastHoveredPiece.GetComponent<PlanPiece>())
                {
                    return;
                }
                if (LastHoveredPiece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(color, HighlightTimeout + 0.1f);
                }
            }
            LastHighlightTime = Time.time;
        }

        /// <summary>
        ///     Get the GameObject from a ZDOID via ZNetScene or force creation of one via ZDO
        /// </summary>
        public static GameObject GetGameObject(ZDOID zdoid, bool required = false)
        {
            GameObject go = ZNetScene.instance.FindInstance(zdoid);
            if (go)
            {
                return go;
            }
            return required ? ZNetScene.instance.CreateObject(ZDOMan.instance.GetZDO(zdoid)) : null;
        }

        public static bool ClearClipboard()
        {
            if (TemporaryBlueprints.Count == 0)
            {
                return false;
            }

            foreach (var tmp in TemporaryBlueprints)
            {
                tmp.Value.DestroyBlueprint();
            }
            TemporaryBlueprints.Clear();
            Player.m_localPlayer.UpdateKnownRecipesList();
            Player.m_localPlayer.UpdateAvailablePiecesList();
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Temporary);

            return true;
        }

        /// <summary>
        ///     Create pieces for all known local Blueprints
        /// </summary>
        public static void RegisterKnownBlueprints()
        {
            if (Player.m_localPlayer)
            {
                Logger.LogInfo("Registering known blueprints");

                foreach (var bp in LocalBlueprints.Values)
                {
                    bp.CreatePiece();
                }
                Player.m_localPlayer.UpdateKnownRecipesList();
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }

        /// <summary>
        ///     Create blueprint pieces on player spawn
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        private static void Player_OnSpawned_Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                RegisterKnownBlueprints();

                if (!Config.AllowBlueprintRune.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    Player.m_localPlayer.SetBuildCategory(0);
                    Player.m_localPlayer.SetSelectedPiece(new Vector2Int(9, 9));
                }
            }
        }

        /// <summary>
        ///     Order the Blueprint Rune's piece table: Tools, Clipboard, Blueprints, then the
        ///     custom categories alphabetically, blueprints inside a category by name.
        ///     The new build UI takes both the tag order and the piece order from m_availablePieces,
        ///     which vanilla fills from m_pieces - reordering m_availablePiecesByCategory afterwards
        ///     has no effect on it.
        /// </summary>
        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
        [HarmonyPrefix]
        private static void PieceTable_UpdateAvailable_Prefix(PieceTable __instance)
        {
            if (!__instance.name.Equals(BlueprintAssets.PieceTableName))
            {
                return;
            }

            var categoryNames = PieceManager.Instance.GetPieceCategoriesMap();
            var sorted = __instance.m_pieces
                .OrderBy(prefab => CategorySortKey(prefab, categoryNames))
                // Only the blueprints sort by name, the tools keep their registration order
                .ThenBy(prefab => IsBlueprintPiece(prefab) && prefab.TryGetComponent(out Piece piece)
                    ? piece.m_name
                    : string.Empty)
                .ToList();

            // Keep the list instance itself, Jotunn registers new pieces into it
            __instance.m_pieces.Clear();
            __instance.m_pieces.AddRange(sorted);
        }

        /// <summary>
        ///     Sort key of a piece's category: the three own categories in fixed order,
        ///     everything else alphabetically behind them.
        /// </summary>
        private static (int, string) CategorySortKey(GameObject prefab, Dictionary<Piece.PieceCategory, string> categoryNames)
        {
            if (!prefab || !prefab.TryGetComponent(out Piece piece) ||
                !categoryNames.TryGetValue(piece.m_category, out string name))
            {
                return (FixedCategoryOrder.Length + 1, string.Empty);
            }

            int rank = Array.IndexOf(FixedCategoryOrder, name);
            return rank >= 0 ? (rank, string.Empty) : (FixedCategoryOrder.Length, name);
        }

        private static bool IsBlueprintPiece(GameObject prefab)
        {
            return prefab && prefab.name.StartsWith($"{Blueprint.PieceBlueprintName}:");
        }

        /// <summary>
        ///     Lazy ghost instantiation
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
        [HarmonyPrefix]
        private static void Player_SetupPlacementGhost_Prefix(Player __instance)
        {
            if (__instance.m_buildPieces == null)
            {
                return;
            }

            GameObject prefab = __instance.m_buildPieces.GetSelectedPrefab();
            if (!prefab || !prefab.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                return;
            }

            string bpname = prefab.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            if (LocalBlueprints.TryGetValue(bpname, out var bp))
            {
                bp.InstantiateGhost();
            }
        }

        /// <summary>
        ///     Timed ghost destruction
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        [HarmonyPrefix]
        private static void Player_UpdatePlacementGhost_Prefix(Player __instance)
        {
            if (__instance.m_buildPieces == null)
            {
                return;
            }

            GameObject prefab = __instance.m_buildPieces.GetSelectedPrefab();
            if (!prefab || !prefab.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                return;
            }

            string bpname = prefab.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            if (LocalBlueprints.TryGetValue(bpname, out var bp))
            {
                bp.GhostActiveTime = Time.time;
            }
        }

        /// <summary>
        ///     Save the reference to the last hovered piece
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
        [HarmonyPostfix]
        private static void Player_PieceRayTest_Postfix(Piece piece)
        {
            LastHoveredPiece = piece;
        }

        /// <summary>
        ///     BlueprintRune equip
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPostfix]
        private static void Humanoid_EquipItem_Postfix(bool __result)
        {
            if (__result && Player.m_localPlayer?.m_rightItem?.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName)
            {
                OriginalPlaceDistance = Math.Max(Player.m_localPlayer.m_maxPlaceDistance, 8f);
                Player.m_localPlayer.m_maxPlaceDistance = Config.RayDistanceConfig.Value;

                var desc = Hud.instance.m_buildHud.transform.Find("SelectedInfo/selected_piece/piece_description");
                if (desc is RectTransform rect)
                {
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -30f);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 110f);
                }

                if (!Config.AllowBlueprintRune.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
                {
                    if (Hud.IsPieceSelectionVisible())
                    {
                        Hud.HidePieceSelection();
                    }
                    Player.m_localPlayer.SetBuildCategory(0);
                    Player.m_localPlayer.SetSelectedPiece(new Vector2Int(9, 9));
                }
            }
        }

        /// <summary>
        ///     BlueprintRune uneqip
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        [HarmonyPostfix]
        private static void Humanoid_UnequipItem_Postfix(ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer &&
                item != null && item.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName)
            {
                Player.m_localPlayer.m_maxPlaceDistance = OriginalPlaceDistance;

                var desc = Hud.instance.m_buildHud.transform.Find("SelectedInfo/selected_piece/piece_description");
                if (desc is RectTransform rect)
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 36.5f);
                }
            }
        }

        /// <summary>
        ///     Prevent opening the build menu when the rune is selected and globally disabled
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.TogglePieceSelection))]
        [HarmonyPrefix]
        private static bool Hud_TogglePieceSelection_Prefix()
        {
            if (Player.m_localPlayer.m_rightItem?.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName &&
                !Hud.IsPieceSelectionVisible() &&
                !Config.AllowBlueprintRune.Value &&
                !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_blueprintrune_disabled");
                Player.m_localPlayer.SetBuildCategory(0);
                Player.m_localPlayer.SetSelectedPiece(new Vector2Int(9, 9));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
        [HarmonyPostfix]
        private static void Piece_Awake_Postfix(Piece __instance)
        {
            Selection.Instance.OnPieceAwake(__instance);
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy))]
        [HarmonyPostfix]
        private static void Piece_OnDestroy_Postfix(Piece __instance)
        {
            Selection.Instance.OnPieceUnload(__instance);
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Shutdown))]
        [HarmonyPostfix]
        private static void ZNetScene_Shutdown_Postfix()
        {
            // Clearing the dictionary on its own leaves every clipboard piece registered in the
            // rune's piece table, and nothing can clean them up afterwards because ClearClipboard()
            // iterates this very dictionary. Destroy them first.
            foreach (var tmp in TemporaryBlueprints)
            {
                tmp.Value.DestroyBlueprint();
            }
            TemporaryBlueprints.Clear();
            Selection.Instance.Clear();
        }

        /// <summary>
        ///     Get the blueprint behind a blueprint piece, local or from the clipboard
        /// </summary>
        public static bool TryGetBlueprint(string prefabName, out Blueprint blueprint)
        {
            blueprint = null;
            if (string.IsNullOrEmpty(prefabName) || !prefabName.StartsWith(Blueprint.PieceBlueprintName))
            {
                return false;
            }

            string id = prefabName.Substring(Blueprint.PieceBlueprintName.Length + 1);
            var blueprints = id.StartsWith("__") ? TemporaryBlueprints : LocalBlueprints;
            return blueprints.TryGetValue(id, out blueprint);
        }

        /// <summary>
        ///     Display the blueprint tooltip panel when a blueprint building item is hovered.
        ///     Hooked on UpdateBuild instead of SetupPieceInfo because that one is skipped
        ///     entirely when the player leaves the place mode, leaving the panel on screen.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild))]
        [HarmonyPostfix]
        private static void Hud_UpdateBuild_Postfix(Hud __instance)
        {
            var piece = __instance.m_hoveredPiece;
            if (!Config.TooltipEnabledConfig.Value || !Hud.IsPieceSelectionVisible() || !piece
                || !TryGetBlueprint(piece.name, out var bp) || bp.Thumbnail == null)
            {
                BlueprintTooltipGUI.Hide();
                return;
            }

            BlueprintTooltipGUI.Show(bp, piece.m_icon, __instance.m_buildUi.m_currentHoveredPieceButton);
        }
    }
}
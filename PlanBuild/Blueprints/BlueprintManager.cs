using Jotunn.Managers;
using PlanBuild.Blueprints.Marketplace;
using PlanBuild.Plans;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace PlanBuild.Blueprints
{
    internal class BlueprintManager
    {
        private static BlueprintManager _instance;

        public static BlueprintManager Instance => _instance ??= new BlueprintManager();

        public static BlueprintDictionary LocalBlueprints;
        public static BlueprintDictionary ServerBlueprints;
        public static Stack<BlueprintInstance> BlueprintInstances;
        
        public const float HighlightTimeout = 0.5f;
        public const float GhostTimeout = 10f;

        public Piece LastHoveredPiece;

        private float LastHightlightTime;
        private float OriginalPlaceDistance;
        private GameObject OriginalTooltip;

        internal void Init()
        {
            Logger.LogInfo("Initializing BlueprintManager");

            try
            {
                // Init lists
                LocalBlueprints = new BlueprintDictionary();
                ServerBlueprints = new BlueprintDictionary();
                BlueprintInstances = new Stack<BlueprintInstance>();

                Selection.GrowMask = LayerMask.GetMask("Default", "piece", "piece_nonsolid");

                // Init sync
                BlueprintSync.Init();

                // Init Commands
                BlueprintCommands.Init();

                // Init GUI
                BlueprintGUI.Init();

                // Create blueprint prefabs when all pieces were registered
                // Some may still fail, these will be retried every time the blueprint rune is opened
                PieceManager.OnPiecesRegistered += RegisterKnownBlueprints;

                // Hooks
                On.ZNetScene.Shutdown += (orig, self) => BlueprintInstances.Clear();
                On.ZNetScene.Shutdown += (orig, self) => Selection.Instance.Clear();
                On.Player.SetupPlacementGhost += Player_SetupPlacementGhost;
                On.Player.UpdatePlacementGhost += Player_UpdatePlacementGhost;
                On.Player.PieceRayTest += Player_PieceRayTest;
                On.Humanoid.EquipItem += Humanoid_EquipItem;
                On.Humanoid.UnequipItem += Humanoid_UnequipItem;
                On.Piece.Awake += Piece_Awake;
                On.Piece.OnDestroy += Piece_OnDestroy;
                //On.WearNTear.Destroy += WearNTear_Destroy;

                GUIManager.OnCustomGUIAvailable += GUIManager_OnCustomGUIAvailable;
                On.UITooltip.OnHoverStart += UITooltip_OnHoverStart;

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
        public bool CanCapture(Piece piece, bool onlyPlanned = false)
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
        public List<Piece> GetPiecesInRadius(Vector3 position, float radius, bool onlyPlanned = false)
        {
            List<Piece> result = new List<Piece>();
            foreach (var piece in Piece.m_allPieces)
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
        public void HighlightPiecesInRadius(Vector3 startPosition, float radius, Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHightlightTime + HighlightTimeout)
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
            LastHightlightTime = Time.time;
        }

        /// <summary>
        ///     "Highlights" the last hovered piece with a given color.
        /// </summary>
        public void HighlightHoveredPiece(Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHightlightTime + HighlightTimeout)
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
            LastHightlightTime = Time.time;
        }

        /// <summary>
        ///     "Highlights" all pieces belonging to the current hovered Blueprint with a given color.
        /// </summary>
        /*public void HighlightHoveredBlueprint(Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHightlightTime + HighlightTimeout)
            {
                return;
            }
            
            if (LastHoveredPiece && BlueprintInstance.TryGetInstance(LastHoveredPiece, out var blueprintInstance))
            {
                foreach (Piece blueprintPiece in blueprintInstance.GetPieceInstances())
                {
                    if (onlyPlanned && !blueprintPiece.GetComponent<PlanPiece>())
                    {
                        continue;
                    }

                    if (blueprintPiece.TryGetComponent(out WearNTear wearNTear))
                    {
                        wearNTear.Highlight(color, HighlightTimeout + 0.1f);
                    }
                }
            }
            LastHightlightTime = Time.time;
        }*/
        
        /// <summary>
        ///     Remove a <see cref="Piece"/> instance ZDO from its Blueprint <see cref="ZDOIDSet"/>
        /// </summary>
        // public void RemoveFromBlueprint(Piece piece)
        // {
        //     if (BlueprintInstance.TryGetInstance(piece, out var blueprintInstance))
        //     {
        //         blueprintInstance.RemovePiece(piece);
        //     }
        // }

        /// <summary>
        ///     Get the GameObject from a ZDOID via ZNetScene or force creation of one via ZDO
        /// </summary>
        public GameObject GetGameObject(ZDOID zdoid, bool required = false)
        {
            GameObject go = ZNetScene.instance.FindInstance(zdoid);
            if (go)
            {
                return go;
            }
            return required ? ZNetScene.instance.CreateObject(ZDOMan.instance.GetZDO(zdoid)) : null;
        }
        
        public bool SelectLastBlueprint()
        {
            if (BlueprintInstances.Count == 0)
            {
                return false;
            }

            var instance = BlueprintInstances.Peek();
            Selection.Instance.Clear();
            Selection.Instance.AddBlueprint(instance);
            
            return true;
        }

        public bool UndoLastBlueprint()
        {
            if (BlueprintInstances.Count == 0)
            {
                return false;
            }

            var instance = BlueprintInstances.Pop();
            Selection.Instance.Clear();
            foreach (var zdoid in instance.ZDOIDs)
            {
                var go = ZNetScene.instance.FindInstance(zdoid);
                if (go)
                {
                    ZNetScene.instance.Destroy(go);
                }
            }

            return true;
        }

        /// <summary>
        ///     Create pieces for all known local Blueprints
        /// </summary>
        public void RegisterKnownBlueprints()
        {
            // Client only
            if (ZNet.instance != null && !ZNet.instance.IsDedicated())
            {
                Logger.LogInfo("Registering known blueprints");

                // Create prefabs for all known blueprints
                foreach (var bp in LocalBlueprints.Values)
                {
                    bp.CreatePiece();
                }
                Player.m_localPlayer?.UpdateKnownRecipesList();
            }
        }

        /// <summary>
        ///     Lazy ghost instantiation
        /// </summary>
        private void Player_SetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
        {
            if (self.m_buildPieces == null)
            {
                orig(self);
                return;
            }

            GameObject prefab = self.m_buildPieces.GetSelectedPrefab();
            if (!prefab || !prefab.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                orig(self);
                return;
            }

            string bpname = prefab.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            if (LocalBlueprints.TryGetValue(bpname, out var bp))
            {
                bp.InstantiateGhost();
            }

            orig(self);
        }

        /// <summary>
        ///     Timed ghost destruction
        /// </summary>
        private void Player_UpdatePlacementGhost(On.Player.orig_UpdatePlacementGhost orig, Player self, bool flashGuardStone)
        {
            if (self.m_buildPieces == null)
            {
                orig(self, flashGuardStone);
                return;
            }

            GameObject prefab = self.m_buildPieces.GetSelectedPrefab();
            if (!prefab || !prefab.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                orig(self, flashGuardStone);
                return;
            }

            string bpname = prefab.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            if (LocalBlueprints.TryGetValue(bpname, out var bp))
            {
                bp.GhostActiveTime = Time.time;
            }

            orig(self, flashGuardStone);
        }

        /// <summary>
        ///     Save the reference to the last hovered piece
        /// </summary>
        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            LastHoveredPiece = piece;
            return result;
        }

        /// <summary>
        ///     BlueprintRune equip
        /// </summary>
        private bool Humanoid_EquipItem(On.Humanoid.orig_EquipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            bool result = orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer && result &&
                item != null && item.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName)
            {
                RegisterKnownBlueprints();

                OriginalPlaceDistance = Math.Max(Player.m_localPlayer.m_maxPlaceDistance, 8f);
                Player.m_localPlayer.m_maxPlaceDistance = Config.RayDistanceConfig.Value;
                
                var desc = Hud.instance.m_buildHud.transform.Find("SelectedInfo/selected_piece/piece_description");
                if (desc is RectTransform rect)
                {
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -30f);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 110f);
                }
            }
            return result;
        }

        /// <summary>
        ///     BlueprintRune uneqip
        /// </summary>
        private void Humanoid_UnequipItem(On.Humanoid.orig_UnequipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            orig(self, item, triggerEquipEffects);
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
        
        private void Piece_Awake(On.Piece.orig_Awake orig, Piece self)
        {
            orig(self);
            Selection.Instance.OnPieceAwake(self);
        }

        private void Piece_OnDestroy(On.Piece.orig_OnDestroy orig, Piece self)
        {
            orig(self);
            Selection.Instance.OnPieceUnload(self);
        }
        
        // private void WearNTear_Destroy(On.WearNTear.orig_Destroy orig, WearNTear self)
        // {
        //     if (self.m_piece)
        //     {
        //         RemoveFromBlueprint(self.m_piece);
        //     }
        //     orig(self);
        // }

        // Get all prefabs for this GUI session
        private void GUIManager_OnCustomGUIAvailable()
        {
            OriginalTooltip = PrefabManager.Instance.GetPrefab("Tooltip");
        }

        /// <summary>
        ///     Display the blueprint tooltip panel when a blueprint building item is hovered
        /// </summary>
        private void UITooltip_OnHoverStart(On.UITooltip.orig_OnHoverStart orig, UITooltip self, GameObject go)
        {
            if (BlueprintAssets.BlueprintTooltip && Hud.IsPieceSelectionVisible())
            {
                var piece = Hud.instance.m_hoveredPiece;
                if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
                {
                    piece = Player.m_localPlayer.GetSelectedPiece();
                }
                if (Config.TooltipEnabledConfig.Value && piece &&
                    piece.name.StartsWith(Blueprint.PieceBlueprintName) &&
                    LocalBlueprints.TryGetValue(piece.name.Substring(Blueprint.PieceBlueprintName.Length + 1), out var bp) &&
                    bp.Thumbnail != null)
                {
                    self.m_tooltipPrefab = BlueprintAssets.BlueprintTooltip;
                    orig(self, go);
                    global::Utils.FindChild(UITooltip.m_tooltip.transform, "Background")
                        .GetComponent<Image>().color = Config.TooltipBackgroundConfig.Value;
                    global::Utils.FindChild(UITooltip.m_tooltip.transform, "Image")
                        .GetComponent<Image>().sprite = Sprite.Create(bp.Thumbnail, new Rect(0, 0, bp.Thumbnail.width, bp.Thumbnail.height), Vector2.zero);
                }
                else
                {
                    self.m_tooltipPrefab = OriginalTooltip;
                    orig(self, go);
                }

                return;
            }

            orig(self, go);
        }
    }
}
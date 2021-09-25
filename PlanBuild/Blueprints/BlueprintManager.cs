using Jotunn.Managers;
using PlanBuild.Blueprints.Marketplace;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintManager
    {
        private static BlueprintManager _instance;

        public static BlueprintManager Instance
        {
            get
            {
                if (_instance == null) _instance = new BlueprintManager();
                return _instance;
            }
        }

        internal static BlueprintDictionary LocalBlueprints;
        internal static BlueprintDictionary ServerBlueprints;

        private float OriginalPlaceDistance;

        internal const float HighlightTimeout = 0.5f;
        private float LastHightlightTime = 0f;

        internal Piece LastHoveredPiece; 

        internal void Init()
        {
            Jotunn.Logger.LogInfo("Initializing BlueprintManager");

            try
            {
                // Init lists
                LocalBlueprints = new BlueprintDictionary();
                ServerBlueprints = new BlueprintDictionary();

                // Init config
                BlueprintConfig.Init();

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
                On.Player.SetupPlacementGhost += Player_SetupPlacementGhost;
                On.Player.PieceRayTest += Player_PieceRayTest;
                On.Humanoid.EquipItem += Humanoid_EquipItem;
                On.Humanoid.UnequipItem += Humanoid_UnequipItem;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Error caught while initializing: {ex}");
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
        /// <param name="startPosition"></param>
        /// <param name="radius"></param>
        /// <param name="color"></param>
        /// <param name="onlyPlanned"></param>
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
                    wearNTear.Highlight(color, BlueprintManager.HighlightTimeout + 0.1f);
                }
            }
            LastHightlightTime = Time.time;
        }

        /// <summary>
        ///     "Highlights" the last hovered planned piece with a given color.
        /// </summary>
        /// <param name="color"></param>
        public void HighlightHoveredPiece(Color color)
        {
            if (Time.time > LastHightlightTime + HighlightTimeout)
            {
                if (LastHoveredPiece != null && LastHoveredPiece.TryGetComponent(out PlanPiece hoveredPlanPiece))
                {
                    hoveredPlanPiece.m_wearNTear.Highlight(color, BlueprintManager.HighlightTimeout + 0.1f);
                }
                LastHightlightTime = Time.time;
            }
        }

        /// <summary>
        ///     "Highlights" all pieces belonging to the last hovered Blueprint with a given color.
        /// </summary>
        /// <param name="color"></param>
        public void HighlightHoveredBlueprint(Color color)
        {
            if (Time.time > LastHightlightTime + HighlightTimeout)
            {
                if (LastHoveredPiece != null && LastHoveredPiece.TryGetComponent(out PlanPiece hoveredPlanPiece))
                {
                    ZDOID blueprintID = hoveredPlanPiece.GetBlueprintID();
                    if (blueprintID != ZDOID.None)
                    {
                        foreach (PlanPiece planPiece in GetPlanPiecesInBlueprint(blueprintID))
                        {
                            planPiece.m_wearNTear.Highlight(color, BlueprintManager.HighlightTimeout + 0.1f);
                        }
                    }
                }
                LastHightlightTime = Time.time;
            }
        }

        /// <summary>
        ///     Get all pieces belonging to a given Blueprint identified by its <see cref="ZDOID"/>
        /// </summary>
        /// <param name="blueprintID"></param>
        /// <returns></returns>
        public List<PlanPiece> GetPlanPiecesInBlueprint(ZDOID blueprintID)
        {
            List<PlanPiece> result = new List<PlanPiece>();
            ZDO blueprintZDO = ZDOMan.instance.GetZDO(blueprintID);
            if (blueprintZDO == null)
            {
                return result;
            }
            ZDOIDSet planPieces = GetPlanPieces(blueprintZDO);
            foreach (ZDOID pieceZDOID in planPieces)
            {
                GameObject pieceObject = ZNetScene.instance.FindInstance(pieceZDOID);
                if (pieceObject && pieceObject.TryGetComponent(out PlanPiece planPiece))
                {
                    result.Add(planPiece);
                }
            }
            return result;
        }

        /// <summary>
        ///     Get a specific <see cref="Piece"/> from a Blueprint identified by its <see cref="ZDO"/>
        /// </summary>
        /// <param name="blueprintZDO"></param>
        /// <returns></returns>
        public ZDOIDSet GetPlanPieces(ZDO blueprintZDO)
        {
            byte[] data = blueprintZDO.GetByteArray(PlanPiece.zdoBlueprintPiece);
            if (data == null)
            {
                return null;
            }
            return ZDOIDSet.From(new ZPackage(data));
        }

        /// <summary>
        ///     Remove a <see cref="Piece"/> instances ZDO from its Blueprint <see cref="ZDOIDSet"/>
        /// </summary>
        /// <param name="planPiece"></param>
        public void PlanPieceRemovedFromBlueprint(PlanPiece planPiece)
        {
            ZDOID blueprintID = planPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None)
            {
                return;
            }

            ZDO blueprintZDO = ZDOMan.instance.GetZDO(blueprintID);
            if (blueprintZDO == null)
            {
                return;
            }
            ZDOIDSet planPieces = GetPlanPieces(blueprintZDO);
            planPieces?.Remove(planPiece.GetPlanPieceID());
            if (planPieces == null || !planPieces.Any())
            {
                GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
                if (blueprintObject)
                {
                    ZNetScene.instance.Destroy(blueprintObject);
                }
            }
            else
            {
                blueprintZDO.Set(PlanPiece.zdoBlueprintPiece, planPieces.ToZPackage().GetArray());
            }
        }

        /// <summary>
        ///     Create pieces for all known local Blueprints
        /// </summary>
        public void RegisterKnownBlueprints()
        {
            // Client only
            if (ZNet.instance != null && !ZNet.instance.IsDedicated())
            {
                Jotunn.Logger.LogInfo("Registering known blueprints");

                // Create prefabs for all known blueprints
                foreach (var bp in LocalBlueprints.Values)
                {
                    bp.CreatePiece();
                }
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
        ///     Save the reference to the last hovered piece
        /// </summary>
        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            LastHoveredPiece = piece;
            return result;
        }

        /// <summary>
        ///     Register blueprints and apply the config place distance
        /// </summary>
        private bool Humanoid_EquipItem(On.Humanoid.orig_EquipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            bool result = orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer && result &&
                item != null && item.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName)
            {
                RegisterKnownBlueprints();
                OriginalPlaceDistance = Math.Max(Player.m_localPlayer.m_maxPlaceDistance, 8f);
                Player.m_localPlayer.m_maxPlaceDistance = BlueprintConfig.RayDistanceConfig.Value;

                On.Player.CheckCanRemovePiece += Player_CheckCanRemovePiece;
            }
            return result;
        }

        /// <summary>
        ///     Restore the original place distance
        /// </summary>
        private void Humanoid_UnequipItem(On.Humanoid.orig_UnequipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer &&
                item != null && item.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName)
            {
                Player.m_localPlayer.m_maxPlaceDistance = OriginalPlaceDistance;

                On.Player.CheckCanRemovePiece -= Player_CheckCanRemovePiece;
            }
        }

        private bool Player_CheckCanRemovePiece(On.Player.orig_CheckCanRemovePiece orig, Player self, Piece piece)
        {
            return CanCapture(piece, true);
        }
    }
}
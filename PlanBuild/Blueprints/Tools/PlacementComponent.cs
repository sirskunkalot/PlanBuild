using Jotunn.Managers;
using PlanBuild.Plans;
using System;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class PlacementComponent : ToolComponentBase
    {
        public override void OnStart()
        {
            SuppressPieceHighlight = false;
            ResetPlacementOffset = false;
        }
        
        public override void OnUpdatePlacement(Player self)
        {
            DisableSelectionProjector();

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                bool radiusModifier = ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name);
                bool deleteModifier = ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name);
                if (radiusModifier && deleteModifier)
                {
                    PlacementOffset.y += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (deleteModifier)
                {
                    PlacementOffset.x += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (radiusModifier)
                {
                    PlacementOffset.z += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (ZInput.GetButton(BlueprintConfig.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            if (self.m_placementStatus == Player.PlacementStatus.Valid)
            {
                try
                {
                    PlaceBlueprint(self, piece);
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"Exception caught while placing {piece.gameObject.name}: {ex}\n{ex.StackTrace}");
                }
            }

            // Dont set the blueprint piece and clutter the world with it
            return false;
        }

        private void PlaceBlueprint(Player player, Piece piece)
        {
            string id = piece.gameObject.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            Blueprint bp = BlueprintManager.LocalBlueprints[id];
            var transform = player.m_placementGhost.transform;
            var position = transform.position;
            var rotation = transform.rotation;

            bool placeDirect = BlueprintConfig.DirectBuildDefault;
            placeDirect ^= ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name);
            if (placeDirect
                && !BlueprintConfig.AllowDirectBuildConfig.Value
                && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_direct_build_disabled");
                return;
            }

            uint cntEffects = 0u;
            uint maxEffects = 10u;

            ZDOIDSet createdPlans = new ZDOIDSet();
            ZDO blueprintZDO = null;
            if (!placeDirect)
            {
                GameObject blueprintPrefab = PrefabManager.Instance.GetPrefab(Blueprint.PieceBlueprintName);
                GameObject blueprintObject = Instantiate(blueprintPrefab, position, rotation);
                blueprintZDO = blueprintObject.GetComponent<ZNetView>().GetZDO();
                blueprintZDO.Set(Blueprint.ZDOBlueprintName, bp.Name);
            }

            for (int i = 0; i < bp.PieceEntries.Length; i++)
            {
                PieceEntry entry = bp.PieceEntries[i];

                // Final position
                Vector3 entryPosition = transform.TransformPoint(entry.GetPosition());

                // Final rotation
                Quaternion entryQuat = transform.rotation * entry.GetRotation();
                
                // Dont place an erroneously captured piece_blueprint
                if (entry.name == Blueprint.PieceBlueprintName)
                {
                    continue;
                }

                // Dont place blacklisted pieces
                if (!SynchronizationManager.Instance.PlayerIsAdmin && PlanBlacklist.Contains(entry.name))
                {
                    Jotunn.Logger.LogWarning($"{entry.name} is blacklisted, not placing @{entryPosition}");
                    continue;
                }

                // Get the prefab of the piece or the plan piece
                string prefabName = entry.name;
                if (!placeDirect)
                {
                    prefabName += PlanPiecePrefab.PlannedSuffix;
                }

                GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                if (!prefab)
                {
                    Jotunn.Logger.LogWarning($"{entry.name} not found, you are probably missing a dependency for blueprint {bp.Name}, not placing @{entryPosition}");
                    continue;
                }

                if (!(SynchronizationManager.Instance.PlayerIsAdmin || BlueprintConfig.AllowTerrainmodConfig.Value)
                    && (prefab.GetComponent<TerrainModifier>() || prefab.GetComponent<TerrainOp>()))
                {
                    Jotunn.Logger.LogWarning("Flatten not allowed, not placing terrain modifiers");
                    continue;
                }

                // Instantiate a new object with the new prefab
                GameObject gameObject = Instantiate(prefab, entryPosition, entryQuat);
                if(!gameObject)
                {
                    Jotunn.Logger.LogWarning($"Invalid PieceEntry: {entry.name}");
                    continue;
                }
                OnPiecePlaced(gameObject);

                ZNetView zNetView = gameObject.GetComponent<ZNetView>();
                if (!zNetView)
                {
                    Jotunn.Logger.LogWarning($"No ZNetView for {gameObject}!!??");
                }
                else if (!placeDirect && gameObject.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.PartOfBlueprint(blueprintZDO.m_uid, entry);
                    createdPlans.Add(planPiece.GetPlanPieceID());
                }

                // Register special effects
                CraftingStation craftingStation = gameObject.GetComponentInChildren<CraftingStation>();
                if (craftingStation)
                {
                    player.AddKnownStation(craftingStation);
                }
                Piece newpiece = gameObject.GetComponent<Piece>();
                if (newpiece)
                {
                    newpiece.SetCreator(player.GetPlayerID());
                }
                PrivateArea privateArea = gameObject.GetComponent<PrivateArea>();
                if (privateArea)
                {
                    privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
                }
                WearNTear wearntear = gameObject.GetComponent<WearNTear>();
                if (wearntear)
                {
                    wearntear.OnPlaced();
                }
                TextReceiver textReceiver = gameObject.GetComponent<TextReceiver>();
                if (textReceiver != null)
                {
                    textReceiver.SetText(entry.additionalInfo);
                }

                // Limited build effects and none for planned pieces
                if (placeDirect && newpiece && cntEffects < maxEffects)
                {
                    newpiece.m_placeEffect.Create(gameObject.transform.position, rotation, gameObject.transform, 1f);
                    player.AddNoise(50f);
                    cntEffects++;
                }

                // Count up player builds
                Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
            }

            if(!placeDirect)
            {
                blueprintZDO.Set(PlanPiece.zdoBlueprintPiece, createdPlans.ToZPackage().GetArray());
            }
        }

        /// <summary>
        ///     Hook for patching
        /// </summary>
        /// <param name="newpiece"></param>
        internal virtual void OnPiecePlaced(GameObject placedPiece)
        {
        }
    }
}
using Jotunn.Managers;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class PlacementComponent : ToolComponentBase
    {
        public override void OnStart()
        {
            SuppressGizmo = false;
            SuppressPieceHighlight = false;
            ResetPlacementOffset = false;
        }

        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            DisableSelectionProjector();

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                bool radiusModifier = ZInput.GetButton(Config.CtrlModifierButton.Name);
                bool deleteModifier = ZInput.GetButton(Config.AltModifierButton.Name);
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
                else if (ZInput.GetButton(Config.ShiftModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
            }

            if (ZInput.GetButton(Config.ToggleButton.Name))
            {
                PlacementOffset = Vector3.zero;
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            if (self.m_placementStatus != Player.PlacementStatus.Valid)
            {
                return;
            }

            try
            {
                PlaceBlueprint(self, piece);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Exception caught while placing {piece.gameObject.name}: {ex}\n{ex.StackTrace}");
            }
        }

        private void PlaceBlueprint(Player player, Piece piece)
        {
            string id = piece.gameObject.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            Blueprint bp;
            if (id.StartsWith("__"))
            {
                bp = BlueprintManager.TemporaryBlueprints[id];
            }
            else
            {
                bp = BlueprintManager.LocalBlueprints[id];
            }
            var transform = player.m_placementGhost.transform;
            var position = transform.position;
            var rotation = transform.rotation;

            bool placeDirect = Config.DirectBuildDefault;
            placeDirect ^= ZInput.GetButton(Config.CtrlModifierButton.Name);
            if (placeDirect
                && !Config.AllowDirectBuildConfig.Value
                && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_direct_build_disabled");
                return;
            }

            for (int i = 0; i < bp.TerrainMods.Length; i++)
            {
                TerrainModEntry entry = bp.TerrainMods[i];
                
                // Final position
                Vector3 entryPosition = transform.TransformPoint(entry.GetPosition());
                
                // Final rotation
                Quaternion entryQuat = transform.rotation; // * entry.GetRotation();

                Dictionary<TerrainComp, Indices> indices = null;
                if (entry.shape.Equals("circle", StringComparison.OrdinalIgnoreCase))
                {
                    indices = TerrainTools.GetCompilerIndicesWithCircle(entryPosition, entry.radius * 2,
                        BlockCheck.Off);
                }
                if (entry.shape.Equals("square", StringComparison.OrdinalIgnoreCase))
                {
                    indices = TerrainTools.GetCompilerIndicesWithRect(entryPosition, entry.radius * 2, entry.radius * 2,
                        entryQuat.eulerAngles.x * Mathf.PI / 180f, BlockCheck.Off);
                }
                TerrainTools.LevelTerrain(indices, entryPosition, entry.radius, entry.smooth, entryPosition.y);
                if (!string.IsNullOrEmpty(entry.paint))
                {
                    TerrainTools.PaintTerrain(indices, entryPosition, entry.radius,
                        (TerrainModifier.PaintType) Enum.Parse(typeof(TerrainModifier.PaintType), entry.paint));
                }
            }
            
            uint cntEffects = 0u;
            uint maxEffects = 10u;

            List<ZDO> ZDOs = new List<ZDO>();

            for (int i = 0; i < bp.PieceEntries.Length; i++)
            {
                PieceEntry entry = bp.PieceEntries[i];

                // Dont place an erroneously captured piece_blueprint
                if (entry.name == Blueprint.PieceBlueprintName)
                {
                    continue;
                }

                // Final position
                Vector3 entryPosition = transform.TransformPoint(entry.GetPosition());

                // Final rotation
                Quaternion entryQuat = transform.rotation * entry.GetRotation();

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
                    Jotunn.Logger.LogWarning($"{prefabName} not found, you are probably missing a dependency, not placing @{entryPosition}");
                    continue;
                }

                // No Terrain stuff unless allowed
                // if (!(SynchronizationManager.Instance.PlayerIsAdmin || Config.AllowTerrainmodConfig.Value)
                //     && (prefab.GetComponent<TerrainModifier>() || prefab.GetComponent<TerrainOp>()))
                // {
                //     Jotunn.Logger.LogWarning("Flatten not allowed, not placing terrain modifiers");
                //     continue;
                // }

                // Instantiate a new object with the prefab
                GameObject gameObject = Instantiate(prefab, entryPosition, entryQuat);
                if (!gameObject)
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
                else
                {
                    ZDOs.Add(zNetView.m_zdo);
                    zNetView.SetLocalScale(entry.GetScale());
                }

                // Register special effects
                Piece newpiece = gameObject.GetComponent<Piece>();
                if (newpiece)
                {
                    newpiece.SetCreator(player.GetPlayerID());

                    if (placeDirect && cntEffects < maxEffects)
                    {
                        newpiece.m_placeEffect.Create(gameObject.transform.position, rotation, gameObject.transform);
                        player.AddNoise(50f);
                        cntEffects++;
                    }

                    if (placeDirect)
                    {
                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                    }
                }
                CraftingStation craftingStation = gameObject.GetComponentInChildren<CraftingStation>();
                if (craftingStation)
                {
                    player.AddKnownStation(craftingStation);
                }
                PrivateArea privateArea = gameObject.GetComponent<PrivateArea>();
                if (privateArea)
                {
                    privateArea.Setup(Game.instance.GetPlayerProfile().GetName());

                    if (placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        zNetView.m_zdo.Set("enabled", bool.Parse(entry.additionalInfo));
                    }
                }
                WearNTear wearntear = gameObject.GetComponent<WearNTear>();
                if (wearntear)
                {
                    wearntear.OnPlaced();
                }
                TextReceiver textReceiver = gameObject.GetComponent<TextReceiver>();
                if (textReceiver != null)
                {
                    if (!placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        zNetView.m_zdo.Set(Blueprint.AdditionalInfo, entry.additionalInfo);
                    }
                    textReceiver.SetText(string.IsNullOrEmpty(entry.additionalInfo) ? string.Empty : entry.additionalInfo);
                }
                ItemStand itemStand = gameObject.GetComponent<ItemStand>();
                if (itemStand != null)
                {
                    if (placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        var fields = entry.additionalInfo.Split(':');
                        if (fields.Length < 2)
                        {
                            Jotunn.Logger.LogWarning($"ItemStand items not found, not adding items @{entryPosition}");
                            continue;
                        }
                        var item = fields[0];
                        var variant = int.Parse(fields[1]);
                        zNetView.m_zdo.Set("item", item);
                        zNetView.m_zdo.Set("variant", variant);
                        itemStand.SetVisualItem(item, variant);
                    }
                }
                ArmorStand armorStand = gameObject.GetComponent<ArmorStand>();
                if (armorStand != null)
                {
                    if (placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        var fields = entry.additionalInfo.Split(':');
                        if (fields.Length < 2)
                        {
                            Jotunn.Logger.LogWarning($"ArmorStand items not found, not adding items @{entryPosition}");
                            continue;
                        }
                        var pose = int.Parse(fields[0]);
                        zNetView.m_zdo.Set("pose", pose);
                        armorStand.SetPose(pose, false);
                        var cnt = int.Parse(fields[1]);
                        for (int j = 0; j < cnt; j++)
                        {
                            var item = fields[j * 2 + 2];
                            var variant = int.Parse(fields[j * 2 + 3]);
                            zNetView.m_zdo.Set($"{j}_item", item);
                            zNetView.m_zdo.Set($"{j}_variant", variant);
                            armorStand.SetVisualItem(j, item, variant);
                        }
                    }
                }
                Door door = gameObject.GetComponent<Door>();
                if (door != null)
                {
                    if (placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        zNetView.m_zdo.Set("state", int.Parse(entry.additionalInfo));
                    }
                }
                Container container = gameObject.GetComponent<Container>();
                if (container != null)
                {
                    if (placeDirect && zNetView && !string.IsNullOrEmpty(entry.additionalInfo))
                    {
                        zNetView.m_zdo.Set("items", entry.additionalInfo);
                    }
                }
                if (placeDirect && zNetView && Config.UnlimitedHealthConfig.Value)
                {
                    if (zNetView.GetComponent<WearNTear>() || zNetView.GetComponent<TreeBase>() ||
                        zNetView.GetComponent<TreeLog>() || zNetView.GetComponent<Destructible>())
                    {
                        zNetView.m_zdo.Set("health", float.MaxValue);
                    }
                }
            }

            // Create undo action
            if (ZDOs.Any())
            {
                var action = new UndoCreate(ZDOs);
                UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
            }

            // Reset offset
            PlacementOffset = Vector3.zero;
        }

        /// <summary>
        ///     Hook for patching
        /// </summary>
        internal virtual void OnPiecePlaced(GameObject placedPiece)
        {
        }
    }
}
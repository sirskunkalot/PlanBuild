using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Plans;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints.Tools
{
    internal class DeleteObjectsComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            EnableSelectionProjector(self, true);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (ZInput.GetButton(Config.ShiftModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            if (!Config.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return;
            }

            int delcnt;
            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                // Remove Pieces
                delcnt = RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius,
                    new Type[] { typeof(Piece) },
                    new Type[] { typeof(PlanPiece) });
            }
            else if (ZInput.GetButton(Config.AltModifierButton.Name))
            {
                // Remove All
                delcnt = RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(ZSFX) });
            }
            else
            {
                // Remove Vegetation
                delcnt = RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(ZSFX), typeof(Piece), typeof(ItemDrop)});
            }
            
            if (delcnt > 0)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                    Localization.instance.Localize("$msg_removed_objects", delcnt.ToString()));
            }
        }

        private int RemoveObjects(Transform transform, float radius, Type[] includeTypes, Type[] excludeTypes)
        {
            Logger.LogDebug($"Entered RemoveVegetation {transform.position} / {radius}");

            int delcnt = 0;
            ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                if (Location.IsInsideNoBuildLocation(startPosition))
                {
                    return delcnt;
                }

                IEnumerable<GameObject> prefabs = FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() &&
                                  //obj.GetComponents<Component>().Select(x => x.GetType()) is Type[] comp &&
                                  (includeTypes == null || includeTypes.All(x => obj.GetComponent(x) != null)) &&
                                  (excludeTypes == null || excludeTypes.All(x => obj.GetComponent(x) == null)));

                var ZDOs = new List<ZDO>();

                foreach (GameObject prefab in prefabs)
                {
                    if (!prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        continue;
                    }

                    ZDOs.Add(zNetView.m_zdo);
                    zNetView.ClaimOwnership();
                    zNetScene.Destroy(prefab);
                    ++delcnt;
                }

                var action = new UndoActions.UndoRemove(ZDOs);
                UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);

                Logger.LogDebug($"Removed {delcnt} objects");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing objects: {ex}");
            }

            return delcnt;
        }
    }
}
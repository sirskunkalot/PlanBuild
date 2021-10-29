using System;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class DeleteObjectsComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            EnableSelectionProjector(self, true);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (ZInput.GetButton(BlueprintConfig.CameraModifierButton.Name))
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

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            if (!BlueprintConfig.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
            {
                TerrainTools.RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(ZSFX) });
            }
            else
            {
                TerrainTools.RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(Piece), typeof(ItemDrop), typeof(ZSFX) });
            }
            return false;
        }
    }
}
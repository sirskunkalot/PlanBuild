using System;
using Jotunn.Managers;
using PlanBuild.Plans;
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
            if (!Config.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return;
            }

            int delcnt;
            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                // Remove Pieces
                delcnt = TerrainTools.RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius,
                    new Type[] { typeof(Piece) },
                    new Type[] { typeof(PlanPiece) });
            }
            else if (ZInput.GetButton(Config.AltModifierButton.Name))
            {
                // Remove All
                delcnt = TerrainTools.RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(ZSFX) });
            }
            else
            {
                // Remove Vegetation
                delcnt = TerrainTools.RemoveObjects(
                    self.m_placementGhost.transform, SelectionRadius, null, new Type[]
                    { typeof(Character), typeof(TerrainModifier), typeof(ZSFX), typeof(Piece), typeof(ItemDrop)});
            }
            
            if (delcnt > 0)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                    Localization.instance.Localize("$msg_removed_objects", delcnt.ToString()));
            }
        }
    }
}
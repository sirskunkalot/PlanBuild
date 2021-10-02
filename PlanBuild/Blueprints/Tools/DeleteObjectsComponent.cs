using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class DeleteObjectsComponent : ToolComponentBase
    {
        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            EnableSelectionProjector(self, true);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (!BlueprintConfig.Allowed(BlueprintConfig.AllowTerrainmodConfig.Value))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
            {
                TerrainTools.RemoveObjects(self.m_placementGhost.transform, SelectionRadius);
            }
            else
            {
                TerrainTools.RemoveVegetation(self.m_placementGhost.transform, SelectionRadius);
            }
            return false;
        }
    }
}

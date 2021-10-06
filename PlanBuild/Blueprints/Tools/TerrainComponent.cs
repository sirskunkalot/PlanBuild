using Jotunn.Managers;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class TerrainComponent : ToolComponentBase
    {
        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            EnableSelectionProjector(self);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                bool radiusModifier = ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name);
                bool deleteModifier = ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name);
                if (deleteModifier && radiusModifier)
                {
                    PlacementOffset.y += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (ZInput.GetButton(BlueprintConfig.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else
                {
                    UpdateSelectionRadius(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
            }
            if (ZInput.GetButtonDown(BlueprintConfig.MarkerSwitchButton.Name))
            {
                SelectionProjector.SwitchShape();
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (!BlueprintConfig.AllowTerrainmodConfig.Value)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
            {
                TerrainTools.RemoveTerrain(self.m_placementGhost.transform,
                    SelectionProjector.GetRadius(), SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Square);
            }
            else
            {
                TerrainTools.Flatten(self.m_placementGhost.transform,
                    SelectionProjector.GetRadius(), SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Square);
            }
            PlacementOffset = Vector3.zero;
            return false;
        }
    }
}

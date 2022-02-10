using Jotunn.Managers;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class TerrainComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            EnableSelectionProjector(self);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                bool radiusModifier = ZInput.GetButton(Config.RadiusModifierButton.Name);
                bool deleteModifier = ZInput.GetButton(Config.DeleteModifierButton.Name);
                if (deleteModifier && radiusModifier)
                {
                    PlacementOffset.y += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (ZInput.GetButton(Config.CameraModifierButton.Name))
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
            if (ZInput.GetButtonDown(Config.ToggleButton.Name))
            {
                SelectionProjector.SwitchShape();
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            if (!Config.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (ZInput.GetButton(Config.DeleteModifierButton.Name))
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
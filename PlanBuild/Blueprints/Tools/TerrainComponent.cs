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
                if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt)) ||
                    (Input.GetKey(KeyCode.RightControl) && Input.GetKey(KeyCode.RightAlt)))
                {
                    PlacementOffset.y += GetPlacementOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    UpdateCameraOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    // Nothing, just update the rotation ;)
                }
                else
                {
                    UpdateSelectionRadius(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SelectionProjector.SwitchShape();
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (!(BlueprintConfig.allowFlattenConfig.Value || SynchronizationManager.Instance.PlayerIsAdmin))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                TerrainTools.Paint(self.m_placementGhost.transform, SelectionRadius, TerrainModifier.PaintType.Reset);
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
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

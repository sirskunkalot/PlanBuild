using Jotunn.Managers;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class PaintComponent : ToolComponentBase
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
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SelectionProjector.SwitchMask();
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (!(BlueprintConfig.AllowTerrainmodConfig.Value || SynchronizationManager.Instance.PlayerIsAdmin))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                
            }
            else
            {
                TerrainTools.Paint(self.m_placementGhost.transform, SelectionRadius, TerrainModifier.PaintType.Reset);
            }
            PlacementOffset = Vector3.zero;
            return false;
        }
    }
}

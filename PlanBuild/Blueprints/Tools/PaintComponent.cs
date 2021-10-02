using System.Collections;
using Jotunn.Managers;
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

            EnableSelectionProjector(self, true);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (Input.GetKey(BlueprintConfig.CameraModifierButton.Key))
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
        }
          
        public override bool PlacePiece(Player self, Piece piece)
        { 
            if (!BlueprintConfig.AllowTerrainmodConfig.Value)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return false;
            }

            StopAllCoroutines();
            StartCoroutine(ConstantDraw(self.m_placementGhost.transform));

            return false;
        }

        private IEnumerator ConstantDraw(Transform ghost)
        {
            Vector3 lastPos = Vector3.zero;
            while (ghost != null && ZInput.GetButton("Attack"))
            {
                TerrainModifier.PaintType type = TerrainModifier.PaintType.Reset;

                if (Input.GetKey(BlueprintConfig.RadiusModifierButton.Key))
                {
                    type = TerrainModifier.PaintType.Dirt;
                }
                else if (Input.GetKey(BlueprintConfig.DeleteModifierButton.Key))
                {
                    type = TerrainModifier.PaintType.Paved;
                }

                if (ghost.position != lastPos)
                {
                    TerrainTools.Paint(ghost, SelectionRadius, type);
                    lastPos = ghost.position;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}

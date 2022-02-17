using Jotunn.Managers;
using System.Collections;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class PaintComponent : ToolComponentBase
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
                if (ZInput.GetButton(Config.CameraModifierButton.Name))
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

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!Config.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return;
            }

            StopAllCoroutines();
            StartCoroutine(ConstantDraw(self.m_placementGhost.transform));
        }

        private IEnumerator ConstantDraw(Transform ghost)
        {
            Vector3 lastPos = Vector3.zero;
            while (ghost != null && ZInput.GetButton("Attack"))
            {
                TerrainModifier.PaintType type = TerrainModifier.PaintType.Reset;

                if (ZInput.GetButton(Config.RadiusModifierButton.Name))
                {
                    type = TerrainModifier.PaintType.Dirt;
                }
                else if (ZInput.GetButton(Config.DeleteModifierButton.Name))
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
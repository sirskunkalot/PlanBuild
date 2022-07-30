using System.Collections;
using System.Collections.Generic;
using Jotunn.Managers;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class PaintComponent : ToolComponentBase
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
                    UndoRotation(self, scrollWheel);
                }
                else if (ZInput.GetButton(Config.CtrlModifierButton.Name))
                {
                    UpdateSelectionRotation(scrollWheel);
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

            StopAllCoroutines();
            StartCoroutine(ConstantDraw());
        }

        private IEnumerator ConstantDraw()
        {
            var lastPos = Vector3.zero;
            var ghost = SelectionProjector.transform;
            while (ghost != null && ZInput.GetButton("Attack"))
            {
                var type = TerrainModifier.PaintType.Reset;

                if (ZInput.GetButton(Config.CtrlModifierButton.Name))
                {
                    type = TerrainModifier.PaintType.Dirt;
                }
                else if (ZInput.GetButton(Config.AltModifierButton.Name))
                {
                    type = TerrainModifier.PaintType.Paved;
                }

                if (ghost.position != lastPos)
                {
                    Dictionary<TerrainComp, Indices> indices = null;
                    var pos = SelectionProjector.GetPosition();
                    var rad = SelectionProjector.GetRadius();
                    var rot = SelectionProjector.GetRotation();

                    if (SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Circle)
                    {
                        indices = TerrainTools.GetCompilerIndicesWithCircle(pos, rad * 2, BlockCheck.Off);
                    }
                    if (SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Square)
                    {
                        indices = TerrainTools.GetCompilerIndicesWithRect(pos, rad * 2, rad * 2, rot * Mathf.PI / 180f, BlockCheck.Off);
                    }

                    TerrainTools.PaintTerrain(indices, pos, rad, type);
                    lastPos = ghost.position;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
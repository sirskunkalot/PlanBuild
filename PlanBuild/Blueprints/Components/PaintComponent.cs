using System.Collections;
using Jotunn.Managers;
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
                bool ctrlModifier = ZInput.GetButton(Config.CtrlModifierButton.Name);
                bool altModifier = ZInput.GetButton(Config.AltModifierButton.Name);
                bool shiftModifier = ZInput.GetButton(Config.ShiftModifierButton.Name);
                if (shiftModifier && ctrlModifier)
                {
                    UpdateSelectionWidth(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (shiftModifier && altModifier)
                {
                    UpdateSelectionDepth(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (shiftModifier)
                {
                    UpdateCameraOffset(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (ctrlModifier)
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
                    var indices = GetSelectionIndices();
                    TerrainTools.PaintTerrain(indices, SelectionProjector.GetPosition(),
                        SelectionProjector.GetOuterRadius(), type);
                    lastPos = ghost.position;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
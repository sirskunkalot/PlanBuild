using Jotunn.Managers;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class TerrainComponent : ToolComponentBase
    {
        public override void OnStart()
        {
            ResetMarkerOffset = false;
        }

        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            EnableSelectionProjector(self);
            // Only this tool tilts the shared projector
            SelectionProjector.SetSlope(SelectionSlope);

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
                else if (ctrlModifier && altModifier)
                {
                    UpdateSelectionSlope(scrollWheel);
                    UndoRotation(self, scrollWheel);
                }
                else if (altModifier)
                {
                    MarkerOffset.y += GetPlacementOffset(scrollWheel);
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

            var indices = GetSelectionIndices();
            var pos = SelectionProjector.GetPosition();
            var rad = SelectionProjector.GetOuterRadius();

            var smooth = ZInput.GetButton(Config.CtrlModifierButton.Name)
                ? Mathf.Clamp01(Config.TerrainSmoothConfig.Value) : 0f;

            if (ZInput.GetButton(Config.AltModifierButton.Name))
            {
                TerrainTools.ResetTerrain(indices, pos, rad);
            }
            else if (SelectionSlope != 0)
            {
                // The slope rises towards the projector's rotation. Rectangle indices are already relative
                // to it, circle indices are in world axes and need the rotation as the slope's direction.
                bool isSquare = SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Square;
                var angle = isSquare ? 0f : SelectionProjector.GetRotation() * Mathf.Deg2Rad;
                var length = 2f * (isSquare ? SelectionProjector.GetDepthRadius() : SelectionProjector.GetRadius());
                var amount = length * Mathf.Tan(SelectionSlope * Mathf.Deg2Rad);
                TerrainTools.SlopeTerrain(indices, pos, rad, angle, smooth, pos.y, amount);
            }
            else
            {
                TerrainTools.LevelTerrain(indices, pos, rad, smooth, pos.y);
            }
            MarkerOffset = Vector3.zero;
        }

        public override void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text +=
                $"\n{LocalizationManager.Instance.TryTranslate("$hud_bpterrain_slope")}: {SelectionSlope}°";
        }
    }
}
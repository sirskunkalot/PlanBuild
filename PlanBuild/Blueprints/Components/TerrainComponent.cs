using Jotunn.Managers;
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

            if (ZInput.GetButton(Config.AltModifierButton.Name))
            {
                TerrainTools.ResetTerrain(indices, pos, rad);
            }
            else if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                TerrainTools.LevelTerrain(indices, pos, rad, Mathf.Clamp01(Config.TerrainSmoothConfig.Value), pos.y);
            }
            else
            {
                TerrainTools.LevelTerrain(indices, pos, rad, 0f, pos.y);
            }
            MarkerOffset = Vector3.zero;
        }
    }
}
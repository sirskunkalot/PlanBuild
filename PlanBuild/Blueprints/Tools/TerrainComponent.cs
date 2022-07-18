using Jotunn.Managers;
using PlanBuild.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class TerrainComponent : ToolComponentBase
    {
        public override void OnStart()
        {
            ResetMarkerOffset = false;
        }

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
                bool ctrlModifier = ZInput.GetButton(Config.CtrlModifierButton.Name);
                bool altModifier = ZInput.GetButton(Config.AltModifierButton.Name);
                bool shiftModifier = ZInput.GetButton(Config.ShiftModifierButton.Name);
                if (altModifier)
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
            if (!Config.AllowTerrainmodConfig.Value && !SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_terrain_disabled");
                return;
            }

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

            if (ZInput.GetButton(Config.AltModifierButton.Name))
            {
                TerrainTools.ResetTerrain(indices, pos, rad);
            }
            else if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                TerrainTools.LevelTerrain(indices, pos, rad, 0.33f, pos.y);
            }
            else
            {
                TerrainTools.LevelTerrain(indices, pos, rad, 0f, pos.y);
            }
            MarkerOffset = Vector3.zero;
        }
    }
}
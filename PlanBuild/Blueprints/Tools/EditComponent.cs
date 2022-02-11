using System;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class EditComponent : ToolComponentBase
    {
        private ZDOID CurrentHoveredBlueprintID = ZDOID.None;
        private ZDO CurrentHoveredBlueprint;
        
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            DisableSelectionProjector();
            
            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0)
            {
                if (ZInput.GetButton(Config.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            BlueprintManager.Instance.HighlightHoveredBlueprint(Color.grey);
        }

        public override void OnPieceHovered(Piece hoveredPiece)
        {
            if (!hoveredPiece)
            {
                CurrentHoveredBlueprintID = ZDOID.None;
                CurrentHoveredBlueprint = null;
                UpdateDescription();
                return;
            }

            ZDOID blueprintID = hoveredPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None || blueprintID == CurrentHoveredBlueprintID)
            {
                return;
            }

            CurrentHoveredBlueprintID = blueprintID;
            CurrentHoveredBlueprint = ZDOMan.instance.GetZDO(blueprintID);
            UpdateDescription();
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            // Set current blueprint and add all pieces to selection
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                ZDOID blueprintID = BlueprintManager.Instance.LastHoveredPiece.GetBlueprintID();
                if (blueprintID != ZDOID.None)
                {
                    Selection.Instance.Clear();
                    Selection.Instance.AddBlueprint(blueprintID);
                    return false;
                }
            }

            Selection.Instance.Clear();
            return false;
        }

        public override void UpdateDescription()
        {
            if (CurrentHoveredBlueprintID == ZDOID.None)
            {
                return;
            }

            var text = string.Empty;

            var bpname = CurrentHoveredBlueprint.GetString(BlueprintManager.zdoBlueprintName);
            if (!string.IsNullOrEmpty(bpname))
            {
                text += Localization.instance.Localize("$piece_blueprint_select_bp", bpname);
                text += Environment.NewLine;
            }

            Hud.instance.m_pieceDescription.text = text;
        }
    }
}
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class EditComponent : ToolComponentBase
    {
        private ZDOID CurrentHoveredBlueprintID = ZDOID.None;

        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            DisableSelectionProjector();

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (ZInput.GetButton(Config.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
            }
        }

        public override void OnPieceHovered(Piece hoveredPiece)
        {
            if (!hoveredPiece)
            {
                CurrentHoveredBlueprintID = ZDOID.None;
                UpdateDescription();
                return;
            }

            ZDOID blueprintID = hoveredPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None || blueprintID == CurrentHoveredBlueprintID)
            {
                return;
            }

            CurrentHoveredBlueprintID = blueprintID;
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
            if (Selection.Instance.Any())
            {
                return;
            }

            if (CurrentHoveredBlueprintID == ZDOID.None)
            {
                return;
            }

            var blueprintZDO = ZDOMan.instance.GetZDO(CurrentHoveredBlueprintID);
            if (blueprintZDO == null)
            {
                return;
            }

            var text = string.Empty;

            var bpname = blueprintZDO.GetString(BlueprintManager.zdoBlueprintName);
            if (!string.IsNullOrEmpty(bpname))
            {
                text = Localization.instance.Localize("$piece_blueprint_select_bp", bpname);
            }

            Hud.instance.m_pieceDescription.text = text;
        }
    }
}
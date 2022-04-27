using System;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class EditComponent : ToolComponentBase
    {
        private BlueprintInstance CurrentBlueprintInstance;
        
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
        }

        // public override void OnPieceHovered(Piece hoveredPiece)
        // {
        //     BlueprintInstance.TryGetInstance(hoveredPiece, out var hovered);
        //     if (hovered && hovered != CurrentBlueprintInstance)
        //     {
        //         CurrentBlueprintInstance = hovered;
        //         BlueprintManager.Instance.HighlightHoveredBlueprint(Color.white);
        //     }
        // }

        /*public override void OnPlacePiece(Player self, Piece piece)
        {
            // Set current blueprint and add all pieces to selection
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                ZDOID blueprintZDOID = BlueprintManager.Instance.LastHoveredPiece.GetBlueprintZDOID();
                if (blueprintZDOID != ZDOID.None)
                {
                    Selection.Instance.Clear();
                    Selection.Instance.AddBlueprint(blueprintZDOID);
                    return;
                }
            }

            Selection.Instance.Clear();
        }*/

        public override void OnPlacePiece(Player self, Piece piece)
        {
            // Get last blueprint and add all pieces to selection
            if (BlueprintInstance.Instances.Count > 0)
            {
                Selection.Instance.Clear();
                Selection.Instance.AddBlueprint(BlueprintInstance.Instances.Last());
                return;
            }

            Selection.Instance.Clear();
        }

        public override void UpdateDescription()
        {
            if (CurrentBlueprintInstance == null)
            {
                return;
            }

            var text = string.Empty;

            var bpid = CurrentBlueprintInstance.ID;
            if (!string.IsNullOrEmpty(bpid))
            {
                text += Localization.instance.Localize("$piece_blueprint_select_bp", bpid);
                text += Environment.NewLine;
            }

            Hud.instance.m_pieceDescription.text = text;
        }
    }
}
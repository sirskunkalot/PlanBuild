using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        private Piece StartPiece;

        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            bool cameraModifier = ZInput.GetButton(Config.ShiftModifierButton.Name);
            bool radiusModifier = ZInput.GetButton(Config.CtrlModifierButton.Name);
            bool connectedModifier = ZInput.GetButton(Config.AltModifierButton.Name);

            if (radiusModifier && !connectedModifier)
            {
                EnableSelectionProjector(self);
            }
            else
            {
                DisableSelectionProjector();
            }

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0)
            {
                if (cameraModifier)
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else if (radiusModifier && !connectedModifier)
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            if (ZInput.GetButtonDown(Config.ToggleButton.Name))
            {
                Player.m_localPlayer.m_buildPieces.RightPiece();
                Player.m_localPlayer.SetupPlacementGhost();
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            bool cameraModifier = ZInput.GetButton(Config.ShiftModifierButton.Name);
            bool radiusModifier = ZInput.GetButton(Config.CtrlModifierButton.Name);
            bool connectedModifier = ZInput.GetButton(Config.AltModifierButton.Name);

            if (radiusModifier)
            {
                Selection.Instance.AddPiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.LastHoveredPiece &&
                     BlueprintManager.CanCapture(BlueprintManager.LastHoveredPiece))
            {
                if (cameraModifier)
                {
                    if (StartPiece == null)
                    {
                        Selection.Instance.AddPiece(BlueprintManager.LastHoveredPiece);
                        StartPiece = BlueprintManager.LastHoveredPiece;
                    }
                    else
                    {
                        Selection.Instance.AddPiecesBetween(StartPiece, BlueprintManager.LastHoveredPiece);
                        StartPiece = null;
                    }
                }
                else if (connectedModifier)
                {
                    Selection.Instance.AddGrowFromPiece(BlueprintManager.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.AddPiece(BlueprintManager.LastHoveredPiece);
                }
            }
            UpdateDescription();
        }
    }
}
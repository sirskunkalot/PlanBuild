using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }
            
            bool cameraModifier = ZInput.GetButton(Config.CameraModifierButton.Name);
            bool radiusModifier = ZInput.GetButton(Config.RadiusModifierButton.Name);
            bool connectedModifier = ZInput.GetButton(Config.DeleteModifierButton.Name);

            if (radiusModifier && !connectedModifier)
            {
                EnableSelectionProjector(self);
                //BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.green);
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
            bool radiusModifier = ZInput.GetButton(Config.RadiusModifierButton.Name);
            bool connectedModifier = ZInput.GetButton(Config.DeleteModifierButton.Name);

            if (radiusModifier && connectedModifier)
            {
                Selection.Instance.Clear();
            }
            else if (radiusModifier)
            {
                Selection.Instance.AddPiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece &&
                     BlueprintManager.Instance.CanCapture(BlueprintManager.Instance.LastHoveredPiece))
            {
                if (connectedModifier)
                {
                    Selection.Instance.AddGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.AddPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
            }
            UpdateDescription();
        }
    }
}
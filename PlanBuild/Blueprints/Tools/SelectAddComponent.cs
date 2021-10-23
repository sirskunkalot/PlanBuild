using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
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
                if (ZInput.GetButton(BlueprintConfig.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
            {
                Selection.Instance.AddPiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece && 
                     BlueprintManager.Instance.CanCapture(BlueprintManager.Instance.LastHoveredPiece))
            {
                if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
                {
                    Selection.Instance.AddGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.AddPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
            }
            UpdateDescription();
            return false;
        }
    }
}
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectRemoveComponent : SelectionToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (ZInput.GetButton(Config.RadiusModifierButton.Name))
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
                if (ZInput.GetButton(Config.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else if (ZInput.GetButton(Config.RadiusModifierButton.Name))
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            bool radiusModifier = ZInput.GetButton(Config.RadiusModifierButton.Name);
            bool connectedModifier = ZInput.GetButton(Config.DeleteModifierButton.Name);
            if (radiusModifier && connectedModifier)
            {
                Selection.Instance.Clear();
            }
            else if (radiusModifier)
            {
                Selection.Instance.RemovePiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece &&
                     BlueprintManager.Instance.CanCapture(BlueprintManager.Instance.LastHoveredPiece))
            {
                if (connectedModifier)
                {
                    Selection.Instance.RemoveGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.RemovePiece(BlueprintManager.Instance.LastHoveredPiece);
                }
            }
            UpdateDescription();
            return false;
        }
    }
}
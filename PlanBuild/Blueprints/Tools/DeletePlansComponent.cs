using PlanBuild.Plans;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class DeletePlansComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
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
                if (ZInput.GetButton(Config.ShiftModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else if (ZInput.GetButton(Config.CtrlModifierButton.Name))
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.red, onlyPlanned: true);
            }
            // else if (ZInput.GetButton(Config.DeleteModifierButton.Name))
            // {
            //     BlueprintManager.Instance.HighlightHoveredBlueprint(Color.red, true);
            // }
            else
            {
                BlueprintManager.Instance.HighlightHoveredPiece(Color.red, true);
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                DeletePlans(self);
                return;
            }

            // if (ZInput.GetButton(Config.DeleteModifierButton.Name))
            // {
            //     UndoBlueprint();
            //     return;
            // }

            UndoPiece();
        }

        private void UndoPiece()
        {
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (BlueprintManager.Instance.LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                }
            }
        }

        /*private void UndoBlueprint()
        {
            if (!BlueprintManager.Instance.LastHoveredPiece)
            {
                return;
            }

            if (!BlueprintManager.Instance.LastHoveredPiece.TryGetComponent(out PlanPiece _))
            {
                return;
            }

            if (!BlueprintInstance.TryGetInstance(BlueprintManager.Instance.LastHoveredPiece,
                    out var instance))
            {
                return;
            }
            
            int removedPieces = 0;
            foreach (Piece pieceToRemove in instance.GetPieceInstances())
            {
                if (pieceToRemove.TryGetComponent<PlanPiece>(out var planPieceToRemove))
                {
                    planPieceToRemove.Remove();
                    removedPieces++;
                }
            }
            
            ZNetScene.instance.Destroy(instance.gameObject);

            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));
        }*/

        private void DeletePlans(Player self)
        {
            Vector3 deletePosition = self.m_placementMarkerInstance.transform.position;
            int removedPieces = 0;
            foreach (Piece pieceToRemove in BlueprintManager.Instance.GetPiecesInRadius(deletePosition, SelectionRadius, true))
            {
                pieceToRemove.GetComponent<PlanPiece>().m_wearNTear.Remove();
                removedPieces++;
            }
            self.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));
        }
    }
}
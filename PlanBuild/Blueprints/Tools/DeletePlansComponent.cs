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

            if (ZInput.GetButton(Config.RadiusModifierButton.Name))
            {
                BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.red, onlyPlanned: true);
            }
            else if (ZInput.GetButton(Config.DeleteModifierButton.Name))
            {
                BlueprintManager.Instance.HighlightHoveredBlueprint(Color.red, true);
            }
            else
            {
                BlueprintManager.Instance.HighlightHoveredPiece(Color.red, true);
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            if (ZInput.GetButton(Config.RadiusModifierButton.Name))
            {
                return DeletePlans(self);
            }

            if (ZInput.GetButton(Config.DeleteModifierButton.Name))
            {
                return UndoBlueprint();
            }

            return UndoPiece();
        }

        private bool UndoPiece()
        {
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (BlueprintManager.Instance.LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                }
            }

            return false;
        }

        private bool UndoBlueprint()
        {
            if (!BlueprintManager.Instance.LastHoveredPiece)
            {
                return false;
            }

            if (!BlueprintManager.Instance.LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
            {
                return false;
            }

            ZDOID blueprintID = BlueprintManager.Instance.LastHoveredPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None)
            {
                return false;
            }

            int removedPieces = 0;
            foreach (Piece pieceToRemove in BlueprintManager.Instance.GetPiecesInBlueprint(blueprintID))
            {
                if (pieceToRemove.TryGetComponent<PlanPiece>(out var planPieceToRemove))
                {
                    planPieceToRemove.Remove();
                    removedPieces++;
                }
            }

            GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
            if (blueprintObject)
            {
                ZNetScene.instance.Destroy(blueprintObject);
            }

            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));

            return false;
        }

        private bool DeletePlans(Player self)
        {
            Vector3 deletePosition = self.m_placementMarkerInstance.transform.position;
            int removedPieces = 0;
            foreach (Piece pieceToRemove in BlueprintManager.Instance.GetPiecesInRadius(deletePosition, SelectionRadius, true))
            {
                pieceToRemove.GetComponent<PlanPiece>().m_wearNTear.Remove();
                removedPieces++;
            }
            self.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));

            return false;
        }
    }
}
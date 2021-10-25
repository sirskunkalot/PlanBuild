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

            if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
            {
                BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.red, onlyPlanned: true);
            }
            else if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
            {
                BlueprintManager.Instance.HighlightHoveredBlueprint(Color.red);
            }
            else
            {
                BlueprintManager.Instance.HighlightHoveredPiece(Color.red);
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
            {
                return DeletePlans(self);
            }

            if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
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
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (BlueprintManager.Instance.LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    ZDOID blueprintID = planPiece.GetBlueprintID();
                    if (blueprintID != ZDOID.None)
                    {
                        int removedPieces = 0;
                        foreach (PlanPiece pieceToRemove in BlueprintManager.Instance.GetPlanPiecesInBlueprint(blueprintID))
                        {
                            pieceToRemove.Remove();
                            removedPieces++;
                        }

                        GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
                        if (blueprintObject)
                        {
                            ZNetScene.instance.Destroy(blueprintObject);
                        }

                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));
                    }
                }
            }

            return false;
        }

        private bool DeletePlans(Player self)
        {
            Vector3 deletePosition = self.m_placementMarkerInstance.transform.position;
            int removedPieces = 0;
            foreach (Piece pieceToRemove in BlueprintManager.Instance.GetPiecesInRadius(deletePosition, SelectionRadius))
            {
                if (pieceToRemove.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                    removedPieces++;
                }
            }
            self.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));

            return false;
        }
    }
}
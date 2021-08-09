using PlanBuild.Plans;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class DeletePlansComponent : ToolComponentBase
    {
        public override void Init()
        {
            On.Player.UpdateWearNTearHover += Player_UpdateWearNTearHover;
        }

        public override void Remove()
        {
            On.Player.UpdateWearNTearHover -= Player_UpdateWearNTearHover;
        }

        /// <summary>
        ///     Dont highlight pieces while deleting
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void Player_UpdateWearNTearHover(On.Player.orig_UpdateWearNTearHover orig, Player self)
        {
            return;
        }

        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
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
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.red, onlyPlanned: true);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                BlueprintManager.Instance.HighlightHoveredBlueprint(Color.red);
            }
            else
            {
                BlueprintManager.Instance.HighlightHoveredPiece(Color.red);
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return DeletePlans(self);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                return UndoBlueprint();
            }
            else
            {
                return UndoPiece();
            }
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

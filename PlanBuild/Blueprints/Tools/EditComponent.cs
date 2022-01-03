using PlanBuild.Plans;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class EditComponent : ToolComponentBase
    {
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
            if (!hoveredPiece.TryGetComponent<PlanPiece>(out var planPiece))
            {
                return;
            }

            ZDOID blueprintID = planPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None)
            {
                return;
            }

            GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
            if (blueprintObject && blueprintObject.TryGetComponent<ZNetView>(out var znet))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, znet.GetZDO().GetString(Blueprint.ZDOBlueprintName));
            }
        }
        
        public override bool OnPlacePiece(Player self, Piece piece)
        {
            // Add all blueprint pieces when hovered
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (BlueprintManager.Instance.LastHoveredPiece.TryGetComponent<PlanPiece>(out var planPiece) && planPiece.GetBlueprintID() != ZDOID.None)
                {
                    Selection.Instance.AddBlueprint(planPiece.GetBlueprintID());
                }
            }
            // Remove selection when not hovered
            else
            {
                Selection.Instance.Clear();
            }
            
            return false;
        }
    }
}
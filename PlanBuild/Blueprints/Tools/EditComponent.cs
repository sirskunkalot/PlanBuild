using PlanBuild.Plans;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class EditComponent : SelectionToolComponentBase
    {
        private ZDOID LastHoveredBlueprintID = ZDOID.None;

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
            ZDOID blueprintID = hoveredPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None || blueprintID == LastHoveredBlueprintID)
            {
                return;
            }

            GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
            if (blueprintObject && blueprintObject.TryGetComponent<ZNetView>(out var znet))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, znet.GetZDO().GetString(Blueprint.ZDOBlueprintName));
            }
            
            LastHoveredBlueprintID = blueprintID;
        }
        
        public override bool OnPlacePiece(Player self, Piece piece)
        {
            // Set current blueprint and add all pieces to selection
            if (BlueprintManager.Instance.LastHoveredPiece)
            {
                ZDOID blueprintID = BlueprintManager.Instance.LastHoveredPiece.GetBlueprintID();
                if (blueprintID != ZDOID.None)
                {
                    Selection.Instance.Clear();
                    Selection.Instance.AddBlueprint(blueprintID);
                    return false;
                }
            }

            Selection.Instance.Clear();
            return false;
        }
    }
}
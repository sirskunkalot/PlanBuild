using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectEditComponent : SelectionToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            DisableSelectionProjector();

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0)
            {
                if (ZInput.GetButton(Config.ShiftModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!Selection.Instance.Any())
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_blueprint_select_empty"));
                return;
            }

            SelectionGUI.ShowGUI();
        }
    }
}
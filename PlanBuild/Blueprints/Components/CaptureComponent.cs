using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class CaptureComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            EnableSelectionProjector(self);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (ZInput.GetButton(Config.ShiftModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            if (ZInput.GetButton(Config.CtrlModifierButton.Name))
            {
                BlueprintManager.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.green);
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            MakeBlueprint(self);
        }

        private void MakeBlueprint(Player self)
        {
            Vector3 capturePosition = self.m_placementMarkerInstance.transform.position;
            Selection selection = new Selection();
            selection.AddPiecesInRadius(capturePosition, SelectionRadius);
            if (selection.Any())
            {
                var saveCurrentSnapPoints = ZInput.GetButton(Config.AltModifierButton.Name);
                SelectionTools.SaveWithGUI(selection, saveCurrentSnapPoints, false);
            }
        }
    }
}
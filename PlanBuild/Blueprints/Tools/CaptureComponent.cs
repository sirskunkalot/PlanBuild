using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class CaptureComponent : ToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
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
            MakeBlueprint(self);
        }

        private void MakeBlueprint(Player self)
        {
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";
            Jotunn.Logger.LogInfo($"Capturing blueprint {bpname}");

            var bp = new Blueprint();
            Vector3 capturePosition = self.m_placementMarkerInstance.transform.position;
            Selection selection = new Selection();
            selection.AddPiecesInRadius(capturePosition, SelectionRadius);
            if (bp.Capture(selection))
            {
                TextInput.instance.m_queuedSign = new SelectionTools.BlueprintSaveGUI(bp);
                TextInput.instance.Show(Localization.instance.Localize("$msg_bpcapture_save", bp.GetPieceCount().ToString()), bpname, 50);
            }
            else
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
            }
            selection.Clear();
        }
    }
}
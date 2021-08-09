using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class CaptureComponent : ToolComponentBase
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
        ///     Dont highlight pieces while capturing
        /// </summary>
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

            EnableSelectionProjector(self);

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0f)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                else
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                BlueprintManager.Instance.HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, SelectionRadius, Color.green);
            }
        }

        public override bool PlacePiece(Player self, Piece piece)
        {
            return MakeBlueprint(self);
        }

        private bool MakeBlueprint(Player self)
        {
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count() + 1:000}";
            Jotunn.Logger.LogInfo($"Capturing blueprint {bpname}");

            var bp = new Blueprint();
            Vector3 capturePosition = self.m_placementMarkerInstance.transform.position;
            if (bp.Capture(capturePosition, SelectionRadius))
            {
                TextInput.instance.m_queuedSign = new Blueprint.BlueprintSaveGUI(bp);
                TextInput.instance.Show($"Save Blueprint ({bp.GetPieceCount()} pieces captured)", bpname, 50);
            }
            else
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
            }

            // Don't place the piece and clutter the world with it
            return false;
        }
    }
}

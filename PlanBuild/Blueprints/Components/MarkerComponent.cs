using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class MarkerComponent : SelectionToolComponentBase
    {
        public string PieceInstanceName;

        public override void OnStart()
        {
            base.OnStart();
            SuppressGizmo = false;
        }

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

            if (ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove"))
            {
                var hover = BlueprintManager.LastHoveredPiece;
                if (hover && (hover.name.StartsWith(BlueprintAssets.PieceSnapPointName, StringComparison.Ordinal) ||
                              hover.name.StartsWith(BlueprintAssets.PieceCenterPointName, StringComparison.Ordinal)))
                {
                    hover.GetComponent<WearNTear>().Destroy();
                }
            }
        }

        public override void OnPlacePiece(Player self, Piece piece)
        {
            if (!self.m_placementMarkerInstance || !self.m_placementMarkerInstance.activeSelf)
            {
                return;
            }

            if (self.m_placementStatus != Player.PlacementStatus.Valid)
            {
                return;
            }

            var fab = PrefabManager.Instance.GetPrefab(PieceInstanceName);
            var tf = self.m_placementGhost.transform;
            var pos = tf.position;
            var rot = tf.rotation;
            var obj = Instantiate(fab, pos, rot);
            var newPiece = obj.GetComponent<Piece>();

            if (Selection.Instance.Any())
            {
                Selection.Instance.AddPiece(newPiece);
            }
            
            // Create undo action
            var action = new UndoCreate(new [] { newPiece.m_nview.m_zdo });
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
        }

        public override void UpdateDescription()
        {
            // oh boy, overriding the text override to show the marker description...
        }
    }
}
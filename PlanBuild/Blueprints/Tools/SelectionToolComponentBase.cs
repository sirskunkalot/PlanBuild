using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void Init()
        {
            UpdateDescription();
            On.Hud.SetupPieceInfo += OnSetupPieceInfo;
            Selection.Instance.Highlight();
        }

        private void OnSetupPieceInfo(On.Hud.orig_SetupPieceInfo orig, Hud self, Piece piece)
        {
            orig(self, piece);
            UpdateDescription();
        }

        public override void Remove()
        {
            Selection.Instance.Unhighlight();
            On.Hud.SetupPieceInfo -= OnSetupPieceInfo;
        }

        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                EnableSelectionProjector(self);
                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (scrollWheel != 0f)
                {
                    UpdateSelectionRadius(scrollWheel);
                }
                else
                {
                    UndoRotation(self, scrollWheel);
                }
            }
            else
            {
                DisableSelectionProjector();
            }
        }

        internal void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text = Localization.instance.Localize("$piece_blueprint_select_desc", Selection.Instance.Count().ToString());
        }
    }
}
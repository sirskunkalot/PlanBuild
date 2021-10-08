using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void Init()
        {
            UpdateDescription();
            On.Hud.SetupPieceInfo += OnSetupPieceInfo;
        }
        
        private void OnSetupPieceInfo(On.Hud.orig_SetupPieceInfo orig, Hud self, Piece piece)
        {
            orig(self, piece);
            UpdateDescription();
        }

        private void Start()
        {
            Selection.Instance.Highlight();
        }

        private void OnDisable()
        {
            Selection.Instance.Unhighlight();
        }

        public override void Remove()
        {
            On.Hud.SetupPieceInfo -= OnSetupPieceInfo;
        }

        public override void UpdatePlacement(Player self)
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
        }

        internal void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text = Selection.Instance.ToString();
        }
    }
}
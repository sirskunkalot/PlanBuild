namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void OnStart()
        {
            UpdateDescription();
            On.Hud.SetupPieceInfo += OnSetupPieceInfo;
        }

        public void Start()
        {
            Selection.Instance.StartHighlightSelection();
        }

        private void OnSetupPieceInfo(On.Hud.orig_SetupPieceInfo orig, Hud self, Piece piece)
        {
            orig(self, piece);
            UpdateDescription();
        }

        public override void OnOnDestroy()
        {
            On.Hud.SetupPieceInfo -= OnSetupPieceInfo;
            Selection.Instance.StopHighlightSelection();
        }

        internal void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text = Selection.Instance.ToString();
        }
    }
}
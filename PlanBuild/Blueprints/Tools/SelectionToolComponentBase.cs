namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void OnStart()
        {
            UpdateDescription();
            On.Hud.SetupPieceInfo += OnSetupPieceInfo;
            Selection.Instance.StartHighlightSelection();
        }

        public override void OnOnDestroy()
        {
            On.Hud.SetupPieceInfo -= OnSetupPieceInfo;
            Selection.Instance.StopHighlightSelection();
        }

        private void OnSetupPieceInfo(On.Hud.orig_SetupPieceInfo orig, Hud self, Piece piece)
        {
            orig(self, piece);
            UpdateDescription();
        }

        public virtual void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text = Selection.Instance.ToString();
        }
    }
}
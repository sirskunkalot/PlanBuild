namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void OnStart()
        {
            UpdateDescription();
            Selection.Instance.StartHighlightSelection();
        }

        public override void OnOnDestroy()
        {
            Selection.Instance.StopHighlightSelection();
        }

        public override void UpdateDescription()
        {
            Hud.instance.m_pieceDescription.text = Selection.Instance.ToString();
        }
    }
}
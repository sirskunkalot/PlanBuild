using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectionToolComponentBase : ToolComponentBase
    {
        public override void OnStart()
        {
            PlacementOffset = Vector3.zero;
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
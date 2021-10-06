using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            if (ZInput.GetButton(BlueprintConfig.RadiusModifierButton.Name))
            {
                Selection.Instance.AddPiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (ZInput.GetButton(BlueprintConfig.DeleteModifierButton.Name))
                {
                    Selection.Instance.AddGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.AddPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
            }
            UpdateDescription();
            return false;
        }
    }
}
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            if (Input.GetKey(BlueprintConfig.RadiusModifierButton.Key))
            {
                Selection.Instance.AddPiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (Input.GetKey(BlueprintConfig.DeleteModifierButton.Key))
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
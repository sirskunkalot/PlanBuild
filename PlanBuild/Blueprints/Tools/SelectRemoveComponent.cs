using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectRemoveComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                Selection.Instance.RemovePiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    Selection.Instance.RemoveGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
                }
                else
                {
                    Selection.Instance.RemovePiece(BlueprintManager.Instance.LastHoveredPiece);
                }
            }
            UpdateDescription();
            return false;
        } 
    }
}

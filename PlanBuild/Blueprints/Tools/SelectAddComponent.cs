using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectAddComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                BlueprintManager.Instance.activeSelection.AddPiecesInRadius(transform.position, this.SelectionRadius); 
            } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                BlueprintManager.Instance.activeSelection.AddGrowFromPiece(BlueprintManager.Instance.LastHoveredPiece);
            } else
            {
                BlueprintManager.Instance.activeSelection.AddPiece(BlueprintManager.Instance.LastHoveredPiece);
            }
            return false;
        } 
    }
}

using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectRemoveComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            BlueprintManager.Instance.activeSelection.RemovePiecesInRadius(transform.position, this.SelectionRadius); 
            return false;
        } 
    }
}

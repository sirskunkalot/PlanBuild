using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectRemoveComponent : SelectionToolComponentBase
    {
        public override bool PlacePiece(Player self, Piece piece)
        {
            bool radiusModifier = Input.GetKey(BlueprintConfig.RadiusModifierButton.Key);
            bool connectedModifier = Input.GetKey(BlueprintConfig.DeleteModifierButton.Key);
            if(radiusModifier && connectedModifier)
            {
                Selection.Instance.Clear();
            }else if (radiusModifier)
            { 
                Selection.Instance.RemovePiecesInRadius(transform.position, SelectionRadius);
            }
            else if (BlueprintManager.Instance.LastHoveredPiece)
            {
                if (connectedModifier)
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

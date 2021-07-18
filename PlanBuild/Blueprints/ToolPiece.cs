using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class ToolPiece : MonoBehaviour
    {
        private void Awake()
        {
            Jotunn.Logger.LogMessage($"{gameObject} awoken");
            //On.Player.UpdatePlacementGhost += OnUpdatePlacementGhost;
        }

        private void OnDestroy()
        {
            Jotunn.Logger.LogMessage($"{gameObject} destroyed");
            //On.Player.UpdatePlacementGhost -= OnUpdatePlacementGhost;
        }

        private void OnUpdatePlacementGhost(On.Player.orig_UpdatePlacementGhost orig, Player self, bool flashGuardStone)
        {
            UpdatePlacementGhost(self);
        }

        internal virtual void UpdatePlacementGhost(Player self)
        {

        }
    }
}

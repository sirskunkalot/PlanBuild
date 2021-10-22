using System.Collections.Generic;

namespace PlanBuild.Plans
{
    internal class PlanBlacklist : List<int>
    {
        public PlanBlacklist()
        {
            Reload();
        }

        public void Reload()
        {
            Clear();
            foreach (var prefabName in PlanConfig.PlanBlacklistConfig.Value.Split(','))
            {
                int hash = prefabName.Trim().GetStableHashCode();
                if (Contains(hash))
                {
                    continue;
                }
                
                Jotunn.Logger.LogMessage($"Adding {prefabName} to plan blacklist");
                Add(hash);
            }
        }
        
        public bool Contains(PlanPiecePrefab planPiecePrefab)
        {
            if (!planPiecePrefab.OriginalPiece)
            {
                return false;
            }

            int hash = planPiecePrefab.OriginalPiece.name.Split('(')[0].Trim().GetStableHashCode();
            return Contains(hash);
        }
        
        public bool Contains(Piece piece)
        {
            if (!piece)
            {
                return false;
            }

            int hash = piece.name.Split('(')[0].Trim().GetStableHashCode();
            return Contains(hash);
        }
    }
}

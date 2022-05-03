using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintInstance
    {
        public string ID;
        public ZDOIDSet ZDOIDs = new ZDOIDSet();

        public BlueprintInstance(string ID)
        {
            this.ID = ID;
        }

        public bool AddZDOID(ZDOID? zdoid)
        {
            return zdoid.HasValue && ZDOIDs.Add(zdoid.Value);
        }

        public bool RemoveZDOID(ZDOID? zdoid)
        {
            return zdoid.HasValue && ZDOIDs.Remove(zdoid.Value);
        }

        /// <summary>
        ///     Get all piece instances of this blueprint instance
        /// </summary>
        public List<Piece> GetPieceInstances()
        {
            List<Piece> result = new List<Piece>();
            foreach (ZDOID pieceZDOID in ZDOIDs)
            {
                GameObject pieceObject = ZNetScene.instance.FindInstance(pieceZDOID);
                if (pieceObject && pieceObject.TryGetComponent(out Piece blueprintPiece))
                {
                    result.Add(blueprintPiece);
                }
            }
            return result;
        }
    }
}

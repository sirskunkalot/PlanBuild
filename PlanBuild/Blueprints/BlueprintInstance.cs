using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintInstance : IEnumerable<ZDOID>
    {
        public static List<BlueprintInstance> Instances = new List<BlueprintInstance>();

        public string ID;
        private ZDOIDSet ZDOIDs = new ZDOIDSet();
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ZDOID> GetEnumerator()
        {
            foreach (ZDOID zdoid in new List<ZDOID>(ZDOIDs))
            {
                yield return zdoid;
            }
        }

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

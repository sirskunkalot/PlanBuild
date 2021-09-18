using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    class Selection
    {

        private readonly ZDOIDSet zDOIDs = new ZDOIDSet();
        private readonly Dictionary<ZDOID, GameObject> selectedObjectsCache = new Dictionary<ZDOID, GameObject>();

        public void Clear()
        {
            zDOIDs.Clear();
            selectedObjectsCache.Clear();
        }

        public void AddPiecesInRadius(Vector3 worldPos, float radius)
        {

        }

    }
}

using System;
using UnityEngine;

namespace PlanBuild.KitBash
{
    class KitBashObject
    {
        internal Action KitBashApplied;

        public GameObject Prefab { get; internal set; }
        public KitBashConfig Config { get; internal set; }

        public bool ApplyKitBash()
        {
            Jotunn.Logger.LogDebug("Applying KitBash for " + Prefab);
            foreach (KitBashSourceConfig config in Config.KitBashSources)
            {
                if(!KitBashManager.Instance.KitBash(Prefab, config))
                {
                    return false;
                }
            }
            KitBashApplied?.Invoke();
            return true;
            //    foreach(string colliderPath in kitBashConfig.boxColliderPaths)
            //    {
            //        CreateBoxColliderFromMesh(kitbashedPrefab, colliderPath);
            //    }
        }
    }
}

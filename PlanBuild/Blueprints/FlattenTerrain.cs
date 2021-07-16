using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class FlattenTerrain
    {
        public static void FlattenForBlueprint(Transform transform, Bounds bounds)
        {
            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                try
                {
                    Quaternion groundOrientation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    for (float localZ = bounds.min.z - 0.5f; localZ < bounds.max.z + 0.5f; localZ++)
                    {
                        for (float localX = bounds.min.x - 0.5f; localX < bounds.max.x + 0.5f; localX++)
                        {
                            Vector3 groundTargetLocation = transform.TransformPoint(new Vector3(localZ, bounds.min.y - 0.5f, localX));
                            Object.Instantiate(groundPrefab, groundTargetLocation, groundOrientation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening for blueprint: {ex}");
                }
            }
        }
    }
}
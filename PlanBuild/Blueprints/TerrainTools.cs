using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class TerrainTools
    {
        public static void Flatten(Transform transform, float radius)
        {
            Logger.LogDebug($"Entered Flatten {transform} / {radius}");

            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                var startPosition = transform.position;  // + Vector3.down * 0.5f;

                //TerrainModifier.SetTriggerOnPlaced(true);
                try
                {
                    var forward = -radius;
                    while (forward < radius)
                    {
                        var right = -radius;
                        while (right < radius)
                        {
                            var newPos = startPosition + transform.forward * forward + transform.right * right;
                            var newRot = Quaternion.identity;
                            Object.Instantiate(groundPrefab, newPos, newRot);
                            right++;
                        }
                        forward++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening: {ex}");
                }
                /*finally
                {
                    TerrainModifier.SetTriggerOnPlaced(false);
                }*/
            }
        }

        public static void FlattenForBlueprint(Transform transform, Bounds bounds, PieceEntry[] pieces)
        {
            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                //TerrainModifier.SetTriggerOnPlaced(true);

                try
                {
                    var forward = bounds.min.z - 1f;
                    while (forward < bounds.max.z + 1f)
                    {
                        var right = bounds.min.x - 1f;
                        while (right < bounds.max.x + 1f)
                        {
                            if (pieces.Any(x => Vector2.Distance(new Vector2(x.posX, x.posZ), new Vector2(right, forward)) <= 1f))
                            {
                                Object.Instantiate(groundPrefab,
                                    transform.position + transform.forward * forward + transform.right * right + new Vector3(0, -0.5f, 0), transform.rotation);
                            }
                            right++;
                        }
                        forward++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening for blueprint: {ex}");
                }
                /*finally
                {
                    TerrainModifier.SetTriggerOnPlaced(false);
                }*/
            }
        }
    }
}
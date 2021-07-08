using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class FlattenTerrain
    {
        public static void Flatten(Transform transform, Vector2 floorSize, List<PieceEntry> pieces)
        {
            Logger.LogDebug($"Entered FlattenTerrain {transform} / {floorSize} with {pieces.Count}");

            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                var lowestY = pieces.Min(x => x.posY);
                var startPosition = transform.position + Vector3.down * 0.5f;
                var rotation = transform.rotation;

                //TerrainModifier.SetTriggerOnPlaced(true);
                try
                {
                    var forward = 0f;
                    while (forward < floorSize.y)
                    {
                        var right = 0f;
                        while (right < floorSize.x)
                        {
                            var lowestAtPosition = pieces.OrderBy(x => x.posY)
                                .FirstOrDefault(x => Math.Abs(x.posX - forward) < 4f && Math.Abs(x.posZ - right) < 4f);
                            if (lowestAtPosition != null)
                            {
                                Logger.LogDebug("Lowest: " + lowestAtPosition.posY);

                                Object.Instantiate(groundPrefab,
                                    startPosition + transform.forward * forward + transform.right * right + new Vector3(0, lowestAtPosition.posY, 0), rotation);
                            }
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
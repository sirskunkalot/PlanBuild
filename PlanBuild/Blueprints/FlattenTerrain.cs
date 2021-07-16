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
        public static void FlattenForBlueprint(Transform transform, Bounds bounds, List<GameObject> pieces)
        {
            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            { 
                try
                {
                    Quaternion groundOrientation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    Vector3 boxColliderHalfExtents = new Vector3(0.5f, bounds.extents.y, 0.5f);
                    int layerMask = LayerMask.GetMask("piece", "piece_nonsolid");
                    //TerrainModifier.SetTriggerOnPlaced(true);
                    for (float localZ = bounds.min.z; localZ < bounds.max.z; localZ++)
                    { 
                        for(float localX = bounds.min.x; localX < bounds.max.x; localX++)
                        {
                            Vector3 groundTargetLocation = transform.TransformPoint(new Vector3(localZ, bounds.min.y, localX));
                            Collider[] colliders = Physics.OverlapBox(transform.TransformPoint(new Vector3(localZ, bounds.center.y, localX)), boxColliderHalfExtents, groundOrientation, layerMask);
                            if(colliders == null || colliders.Length == 0)
                            {
                                continue;
                            }
                            float lowestColliderHeight = float.MaxValue;
                            foreach(Collider collider in colliders)
                            {
                                Piece piece = collider.GetComponentInParent<Piece>();
                                if(!piece || !pieces.Contains(piece.gameObject))
                                {
                                    continue;
                                }
                                lowestColliderHeight = Mathf.Min(lowestColliderHeight, collider.bounds.min.y);
                            }
                            if(lowestColliderHeight == float.MaxValue)
                            {
                                continue;
                            }
                            groundTargetLocation.y = lowestColliderHeight - 0.5f;
                            Object.Instantiate(groundPrefab, groundTargetLocation, groundOrientation); 
                        } 
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening for blueprint: {ex}");
                } 
               // finally
               // {
               //     TerrainModifier.SetTriggerOnPlaced(false);
               // }
            }
        }
    }
}
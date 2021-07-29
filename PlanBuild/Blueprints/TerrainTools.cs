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
            Logger.LogDebug($"Entered Flatten {transform.position} / {radius}");

            GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                try
                {
                    Vector3 pos = transform.position + Vector3.down * 0.5f;
                    Quaternion rot = transform.rotation;
                    var raise = Object.Instantiate(groundPrefab, pos, rot).GetComponent<TerrainModifier>();
                    raise.m_useTerrainCompiler = true;
                    raise.m_level = true;
                    raise.m_levelRadius = radius;
                    raise.m_smooth = true;
                    raise.m_smoothRadius = radius;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening: {ex}");
                }
            }

            /*GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                Vector3 startPosition = transform.position + Vector3.down * 0.5f;
                try
                {
                    float forward = -radius;
                    while (forward < radius)
                    {
                        float right = -radius;
                        while (right < radius)
                        {
                            Vector3 newPos = startPosition + transform.forward * forward + transform.right * right;
                            Quaternion newRot = Quaternion.identity;
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
            }*/
        }

        public static void Paint(Transform transform, float radius, TerrainModifier.PaintType type)
        {
            Logger.LogDebug($"Entered Paint {transform.position} / {radius}");

            GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                try
                {
                    Vector3 pos = transform.position;
                    Quaternion rot = transform.rotation;
                    var raise = Object.Instantiate(groundPrefab, pos, rot).GetComponent<TerrainModifier>();
                    raise.m_useTerrainCompiler = true;
                    raise.m_level = false;
                    raise.m_paintType = type;
                    raise.m_paintRadius = radius;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while paiting: {ex}");
                }
            }

            /*GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                Vector3 startPosition = transform.position + Vector3.down * 0.5f;
                try
                {
                    float forward = -radius;
                    while (forward < radius)
                    {
                        float right = -radius;
                        while (right < radius)
                        {
                            Vector3 newPos = startPosition + transform.forward * forward + transform.right * right;
                            Quaternion newRot = Quaternion.identity;
                            var raise = Object.Instantiate(groundPrefab, newPos, newRot).GetComponent<TerrainModifier>();
                            raise.m_level = false;
                            raise.m_paintType = type;
                            right++;
                        }
                        forward++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while flattening: {ex}");
                }
            }*/
        }

        public static void FlattenForBlueprint(Transform transform, Bounds bounds)
        {
            GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
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

        public static void RemoveVegetation(Transform transform, float radius)
        {
            Logger.LogDebug($"Entered Remove {transform.position} / {radius}");
            
            ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                IEnumerable<GameObject> prefabs = Object.FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() && !obj.GetComponent<Piece>() && 
                                  !obj.GetComponent<ItemDrop>() && !obj.GetComponent<Character>());

                foreach (GameObject prefab in prefabs)
                {
                    if (!prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        continue;
                    }

                    zNetView.ClaimOwnership();
                    zNetScene.Destroy(prefab);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing objects: {ex}");
            }
        }

        public static void RemoveTerrain(Transform transform, float radius)
        {
            Logger.LogDebug($"Entered Remove {transform.position} / {radius}");

            ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                IEnumerable<GameObject> prefabs = Object.FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() && 
                                  (obj.GetComponent<TerrainModifier>() || obj.GetComponent<TerrainOp>()));

                foreach (GameObject prefab in prefabs)
                {
                    if (!prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        continue;
                    }

                    zNetView.ClaimOwnership();
                    zNetScene.Destroy(prefab);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing terrain: {ex}");
            }
        }
    }
}
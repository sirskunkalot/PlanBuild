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
        private static List<TerrainComp> GetTerrainComp(Vector3 position, float radius)
        {
            List<TerrainComp> ret = new List<TerrainComp>();
            List<Heightmap> maps = new List<Heightmap>();
            Heightmap.FindHeightmap(position, radius, maps);
            foreach (Heightmap map in maps)
            {
                TerrainComp comp = map.GetAndCreateTerrainCompiler();
                if (comp != null)
                {
                    ret.Add(comp);
                    if (!comp.IsOwner())
                    {
                        comp.m_nview.ClaimOwnership();
                    }
                }
            }
            return ret;
        }

        public static void Flatten(Transform transform, float radius, bool square = true)
        {
            Logger.LogDebug($"Entered Flatten {transform.position} / {radius}");
                
            try
            {
                TerrainOp.Settings settings = new TerrainOp.Settings();
                settings.m_level = true;
                settings.m_levelRadius = radius;
                settings.m_square = square;
                settings.m_paintRadius = radius;
                settings.m_paintType = TerrainModifier.PaintType.Dirt;

                foreach (TerrainComp comp in GetTerrainComp(transform.position, radius))
                {
                    comp.DoOperation(transform.position, settings);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while flattening: {ex}");
            }

            /*GameObject groundPrefab = ZNetScene.instance.GetPrefab("raise");
            TerrainModifier raise = groundPrefab.GetComponent<TerrainModifier>();
            raise.m_useTerrainCompiler = true;

            if (groundPrefab)
            {
                Vector3 startPosition = transform.position + Vector3.down * 0.5f;
                Vector2 startPoint = new Vector2(startPosition.x, startPosition.z);
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

                            if (!square && Vector2.Distance(startPoint, new Vector2(newPos.x, newPos.z)) > radius) {
                                continue;
                            }

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

            try
            {
                TerrainOp.Settings settings = new TerrainOp.Settings();
                settings.m_paintRadius = radius;
                settings.m_paintType = type;

                foreach (TerrainComp comp in GetTerrainComp(transform.position, radius))
                {
                    comp.DoOperation(transform.position, settings);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while painting: {ex}");
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

                List<TerrainModifier> modifiers = new List<TerrainModifier>();
                TerrainModifier.GetModifiers(startPosition, radius + 0.1f, modifiers);
                int tmodcnt = 0;
                foreach (TerrainModifier modifier in modifiers)
                {
                    if (modifier.m_nview == null)
                    {
                        continue;
                    }
                    modifier.m_nview.ClaimOwnership();
                    zNetScene.Destroy(modifier.gameObject);
                    tmodcnt++;
                }

                int tcompcnt = 0;
                foreach (TerrainComp comp in GetTerrainComp(transform.position, radius))
                {
                    Heightmap map = comp.m_hmap;
                    zNetScene.Destroy(comp.gameObject);
                    map.Regenerate();
                    tcompcnt++;
                }

                Logger.LogDebug($"Removed {tmodcnt} TerrainMod(s) and {tcompcnt} TerrainComp(s)");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing terrain: {ex}");
            }

            /*ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                IEnumerable<GameObject> prefabs = Object.FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() && 
                                  (obj.GetComponent<TerrainModifier>() || obj.GetComponent<TerrainComp>()));

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
            }*/
        }
    }
}
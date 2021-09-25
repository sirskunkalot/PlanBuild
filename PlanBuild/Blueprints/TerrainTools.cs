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
            Logger.LogDebug($"Found {maps.Count} Heightmaps in radius {radius}");
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

        public static void Flatten(Transform transform, float radius, bool square = false)
        {
            Logger.LogDebug($"Entered Flatten {transform.position} / {radius}");
                
            try
            {
                TerrainOp.Settings settings = new TerrainOp.Settings();
                settings.m_raise = true;
                settings.m_raiseRadius = radius;
                settings.m_raisePower = 3f;
                settings.m_square = square;
                settings.m_paintRadius = radius * 1.1f;
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
        }

        public static void RemoveTerrain(Transform transform, float radius, bool square = false)
        {
            Logger.LogDebug($"Entered RemoveTerrain {transform.position} / {radius}");

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
                    ++tmodcnt;
                }

                int tcompcnt = 0;
                foreach (TerrainComp comp in GetTerrainComp(startPosition, radius + 0.1f))
                {
                    Heightmap hmap = comp.m_hmap;
                    hmap.WorldToVertex(startPosition, out int x, out int y);
                    float scaledRadius = radius / hmap.m_scale;
                    int maxRadius = Mathf.CeilToInt(scaledRadius);
                    int maxWidth = comp.m_width + 1;
                    Vector2 a = new Vector2(x, y);
                    bool modified = false;
                    for (int i = y - maxRadius; i <= y + maxRadius; i++)
                    {
                        for (int j = x - maxRadius; j <= x + maxRadius; j++)
                        {
                            if ((square || !(Vector2.Distance(a, new Vector2(j, i)) > scaledRadius)) && j >= 0 && i >= 0 && j < maxWidth && i < maxWidth)
                            {
                                int index = i * maxWidth + j;
                                modified |= comp.m_modifiedHeight[index];

                                comp.m_smoothDelta[index] = 0f;
                                comp.m_levelDelta[index] = 0f;
                                comp.m_modifiedHeight[index] = false;
                            }
                        }
                    }
                    if (modified)
                    {
                        comp.PaintCleared(startPosition, radius, TerrainModifier.PaintType.Reset, false, true);
                        comp.Save();
                        hmap.Poke(false);
                        if (ClutterSystem.instance)
                        {
                            ClutterSystem.instance.ResetGrass(hmap.transform.position, hmap.m_width * hmap.m_scale / 2f);
                        }
                        ++tcompcnt;
                    }
                }
                Logger.LogDebug($"Removed {tmodcnt} TerrainMod(s) and {tcompcnt} TerrainComp(s)");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing terrain: {ex}");
            }
        }

        public static void RemoveVegetation(Transform transform, float radius)
        {
            Logger.LogDebug($"Entered RemoveVegetation {transform.position} / {radius}");
            
            ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                if (Location.IsInsideNoBuildLocation(startPosition))
                {
                    return;
                }

                IEnumerable<GameObject> prefabs = Object.FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() && !obj.GetComponent<Character>() &&
                                  !obj.GetComponent<TerrainModifier>() && !obj.GetComponent<Piece>() &&
                                  !obj.GetComponent<ItemDrop>() && !obj.GetComponent<ZSFX>());

                int delcnt = 0;
                foreach (GameObject prefab in prefabs)
                {
                    if (!prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        continue;
                    }

                    zNetView.ClaimOwnership();
                    zNetScene.Destroy(prefab);
                    ++delcnt;
                }
                Logger.LogDebug($"Removed {delcnt} objects");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing objects: {ex}");
            }
        }

        public static void RemoveObjects(Transform transform, float radius)
        {
            Logger.LogDebug($"Entered RemoveObjects {transform.position} / {radius}");

            ZNetScene zNetScene = ZNetScene.instance;
            try
            {
                Vector3 startPosition = transform.position;

                if (Location.IsInsideNoBuildLocation(startPosition))
                {
                    return;
                }

                IEnumerable<GameObject> prefabs = Object.FindObjectsOfType<GameObject>()
                    .Where(obj => Vector3.Distance(startPosition, obj.transform.position) <= radius &&
                                  obj.GetComponent<ZNetView>() && !obj.GetComponent<Character>() &&
                                  !obj.GetComponent<TerrainModifier>() && !obj.GetComponent<ZSFX>());

                int delcnt = 0;
                foreach (GameObject prefab in prefabs)
                {
                    if (!prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        continue;
                    }

                    zNetView.ClaimOwnership();
                    zNetScene.Destroy(prefab);
                    ++delcnt;
                }
                Logger.LogDebug($"Removed {delcnt} objects");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error while removing objects: {ex}");
            }
        }
    }
}
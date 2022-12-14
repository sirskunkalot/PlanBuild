using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class HeightIndex
    {
        public int Index;
        public Vector3 Position;
        public float DistanceWidth;
        public float DistanceDepth;
        public float Distance;
    }

    public class PaintIndex
    {
        public int Index;
        public Vector3 Position;
    }

    public class Indices
    {
        public HeightIndex[] HeightIndices = new HeightIndex[0];
        public PaintIndex[] PaintIndices = new PaintIndex[0];
    }

    public enum BlockCheck
    {
        Off,
        On,
        Inverse
    }

    public static class TerrainTools
    {
        public static void Save(TerrainComp compiler)
        {
            compiler.GetComponent<ZNetView>()?.ClaimOwnership();
            compiler.m_operations++;
            // These are only used to remove grass which isn't really needed.
            compiler.m_lastOpPoint = Vector3.zero;
            compiler.m_lastOpRadius = 0f;
            compiler.Save();
            compiler.m_hmap.Poke(false);
        }

        private static Indices FilterByBlockCheck(Indices indices, BlockCheck blockCheck)
        {
            indices.HeightIndices = indices.HeightIndices.Where(index => CheckBlocking(index.Position, blockCheck))
                .ToArray();
            indices.PaintIndices =
                indices.PaintIndices.Where(index => CheckBlocking(index.Position, blockCheck)).ToArray();
            return indices;
        }

        private static IEnumerable<TerrainComp> GetTerrainCompilersWithCircle(Vector3 position, float radius)
        {
            List<Heightmap> heightMaps = new List<Heightmap>();
            Heightmap.FindHeightmap(position, radius + 1f, heightMaps);
            var pos = position;
            var zs = ZoneSystem.instance;
            var ns = ZNetScene.instance;
            return heightMaps.Where(hmap => ns.InActiveArea(zs.GetZone(hmap.transform.position), pos))
                .Select(hmap => hmap.GetAndCreateTerrainCompiler());
        }

        private static IEnumerable<TerrainComp> GetTerrainCompilersWithRect(Vector3 position, float width, float depth,
            float angle)
        {
            List<Heightmap> heightMaps = new List<Heightmap>();
            // Turn the rectable to a square for an upper bound.
            var maxDimension = Mathf.Max(width, depth);
            // Rotating increases the square dimensions.
            var dimensionMultiplier = Mathf.Abs(Mathf.Sin(angle)) + Mathf.Abs(Mathf.Cos(angle));
            var size = maxDimension * dimensionMultiplier / 2f;
            Heightmap.FindHeightmap(position, size + 1f, heightMaps);
            var pos = position;
            var zs = ZoneSystem.instance;
            var ns = ZNetScene.instance;
            return heightMaps.Where(hmap => ns.InActiveArea(zs.GetZone(hmap.transform.position), pos))
                .Select(hmap => hmap.GetAndCreateTerrainCompiler());
        }

        public static Dictionary<TerrainComp, Indices> GetCompilerIndicesWithCircle(Vector3 centerPos, float diameter,
            BlockCheck blockCheck)
        {
            return GetTerrainCompilersWithCircle(centerPos, diameter / 2f).ToDictionary(comp => comp, comp =>
                {
                    return new Indices
                    {
                        HeightIndices = GetHeightIndicesWithCircle(comp, centerPos, diameter)
                            .Where(index => CheckBlocking(index.Position, blockCheck)).ToArray(),
                        PaintIndices = GetPaintIndicesWithCircle(comp, centerPos, diameter)
                            .Where(index => CheckBlocking(index.Position, blockCheck)).ToArray()
                    };
                }).Where(kvp => kvp.Value.HeightIndices.Count() + kvp.Value.PaintIndices.Count() > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Dictionary<TerrainComp, Indices> GetCompilerIndicesWithRect(Vector3 centerPos, float width, float depth,
            float angle, BlockCheck blockCheck)
        {
            return GetTerrainCompilersWithRect(centerPos, width, depth, angle).ToDictionary(comp => comp, comp =>
                {
                    return new Indices
                    {
                        HeightIndices = GetHeightIndicesWithRect(comp, centerPos, width, depth, angle)
                            .Where(index => CheckBlocking(index.Position, blockCheck)).ToArray(),
                        PaintIndices = GetPaintIndicesWithRect(comp, centerPos, width, depth, angle)
                            .Where(index => CheckBlocking(index.Position, blockCheck)).ToArray()
                    };
                }).Where(kvp => kvp.Value.HeightIndices.Count() + kvp.Value.PaintIndices.Count() > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static void SetTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, float smooth,
            float delta)
        {
            Action<TerrainComp, HeightIndex> action = (compiler, heightIndex) =>
            {
                var multiplier = CalculateSmooth(smooth, heightIndex.Distance);
                var index = heightIndex.Index;
                compiler.m_levelDelta[index] = delta * multiplier;
                compiler.m_smoothDelta[index] = 0f;
                compiler.m_modifiedHeight[index] = compiler.m_levelDelta[index] != 0f;
            };
            DoHeightOperation(compilerIndices, pos, radius, action);
        }

        private static float CalculateSmooth(float smooth, float distance) =>
            (1f - distance) >= smooth ? 1f : (1f - distance) / smooth;

        private static float CalculateSlope(float angle, float distanceWidth, float distanceDepth) =>
            Mathf.Sin(angle) * distanceWidth + Mathf.Cos(angle) * distanceDepth;

        public static void RaiseTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, float smooth,
            float amount)
        {
            Action<TerrainComp, HeightIndex> action = (compiler, heightIndex) =>
            {
                var multiplier = CalculateSmooth(smooth, heightIndex.Distance);
                var index = heightIndex.Index;
                compiler.m_levelDelta[index] += multiplier * amount + compiler.m_smoothDelta[index];
                compiler.m_smoothDelta[index] = 0f;
                compiler.m_modifiedHeight[index] = compiler.m_levelDelta[index] != 0f;
            };
            DoHeightOperation(compilerIndices, pos, radius, action);
        }

        public static void LevelTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, float smooth,
            float altitude)
        {
            Action<TerrainComp, HeightIndex> action = (compiler, heightIndex) =>
            {
                var multiplier = CalculateSmooth(smooth, heightIndex.Distance);
                var index = heightIndex.Index;
                compiler.m_levelDelta[index] += multiplier * (altitude - compiler.m_hmap.m_heights[index]);
                compiler.m_smoothDelta[index] = 0f;
                compiler.m_modifiedHeight[index] = compiler.m_levelDelta[index] != 0f;
            };
            DoHeightOperation(compilerIndices, pos, radius, action);
        }

        public static void SlopeTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, float angle,
            float smooth, float altitude, float amount)
        {
            Action<TerrainComp, HeightIndex> action = (compiler, heightIndex) =>
            {
                var multiplier = CalculateSlope(angle, heightIndex.DistanceWidth, heightIndex.DistanceDepth) *
                                 CalculateSmooth(smooth, heightIndex.Distance);
                var index = heightIndex.Index;
                compiler.m_levelDelta[index] +=
                    (altitude - compiler.m_hmap.m_heights[index]) + multiplier * amount / 2f;
                compiler.m_smoothDelta[index] = 0f;
                compiler.m_modifiedHeight[index] = compiler.m_levelDelta[index] != 0f;
            };
            DoHeightOperation(compilerIndices, pos, radius, action);
        }

        public static void ResetTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius)
        {
            List<TerrainModifier> modifiers = new List<TerrainModifier>();
            TerrainModifier.GetModifiers(pos, radius + 1f, modifiers);
            foreach (TerrainModifier modifier in modifiers)
            {
                if (modifier.m_nview == null)
                {
                    continue;
                }
                modifier.m_nview.ClaimOwnership();
                ZNetScene.instance.Destroy(modifier.gameObject);
            }

            Action<TerrainComp, HeightIndex> action = (compiler, heightIndex) =>
            {
                var index = heightIndex.Index;
                compiler.m_levelDelta[index] = 0f;
                compiler.m_smoothDelta[index] = 0f;
                compiler.m_modifiedHeight[index] = false;
            };
            DoHeightOperation(compilerIndices, pos, radius, action);
            PaintTerrain(compilerIndices, pos, radius, Color.black);
        }
        
        public static void PaintTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, TerrainModifier.PaintType type)
        {
            Color color = type switch
            {
                TerrainModifier.PaintType.Dirt => Color.red,
                TerrainModifier.PaintType.Cultivate => Color.green,
                TerrainModifier.PaintType.Paved => Color.blue,
                TerrainModifier.PaintType.Reset => Color.black,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "PaintType not supported")
            };
            PaintTerrain(compilerIndices, pos, radius, color);
        }

        public static void PaintTerrain(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius, Color color)
        {
            Action<TerrainComp, int> action = (compiler, index) =>
            {
                compiler.m_paintMask[index] = color;
                compiler.m_modifiedPaint[index] = true;
            };
            DoPaintOperation(compilerIndices, pos, radius, action);
        }

        ///<summary>Returns terrain data of given indices</summary>
        public static Dictionary<Vector3, UndoActions.TerrainUndoData> GetData(Dictionary<TerrainComp, Indices> compilerIndices)
        {
            return compilerIndices.ToDictionary(kvp => kvp.Key.transform.position, kvp =>
            {
                return new UndoActions.TerrainUndoData
                {
                    Heights = kvp.Value.HeightIndices.Select(heightIndex => new UndoActions.HeightUndoData
                    {
                        Index = heightIndex.Index,
                        HeightModified = kvp.Key.m_modifiedHeight[heightIndex.Index],
                        Level = kvp.Key.m_levelDelta[heightIndex.Index],
                        Smooth = kvp.Key.m_smoothDelta[heightIndex.Index]
                    }).ToArray(),
                    Paints = kvp.Value.PaintIndices.Select(paintIndex => new UndoActions.PaintUndoData
                    {
                        Index = paintIndex.Index,
                        PaintModified = kvp.Key.m_modifiedPaint[paintIndex.Index],
                        Paint = kvp.Key.m_paintMask[paintIndex.Index],
                    }).ToArray(),
                };
            });
        }
        
        private static Vector3 VertexToWorld(Heightmap hmap, int x, int y)
        {
            var vector = hmap.transform.position;
            vector.x += (x - hmap.m_width / 2 + 0.5f) * hmap.m_scale;
            vector.z += (y - hmap.m_width / 2 + 0.5f) * hmap.m_scale;
            return vector;
        }

        private static void DoHeightOperation(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius,
            Action<TerrainComp, HeightIndex> action)
        {
            var before = GetData(compilerIndices);
            foreach (var kvp in compilerIndices)
            {
                var compiler = kvp.Key;
                var indices = kvp.Value.HeightIndices;
                foreach (var heightIndex in indices) action(compiler, heightIndex);
                Save(compiler);
            }
            ClutterSystem.instance?.ResetGrass(pos, radius);
            var after = GetData(compilerIndices);
            var undo = new UndoActions.UndoTerrain(before, after, pos, radius);
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, undo);
        }

        private static void DoPaintOperation(Dictionary<TerrainComp, Indices> compilerIndices, Vector3 pos, float radius,
            Action<TerrainComp, int> action)
        {
            var before = GetData(compilerIndices);
            foreach (var kvp in compilerIndices)
            {
                var compiler = kvp.Key;
                var indices = kvp.Value.PaintIndices;
                foreach (var index in indices) action(compiler, index.Index);
                Save(compiler);
            }
            ClutterSystem.instance?.ResetGrass(pos, radius);
            var after = GetData(compilerIndices);
            var undo = new UndoActions.UndoTerrain(before, after, pos, radius);
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, undo);
        }

        private static bool CheckBlocking(Vector3 position, BlockCheck blockCheck)
        {
            if (blockCheck == BlockCheck.Off) return true;
            var blocked = ZoneSystem.instance.IsBlocked(position);
            if (blocked && blockCheck == BlockCheck.On) return false;
            if (!blocked && blockCheck == BlockCheck.Inverse) return false;
            return true;
        }

        private static IEnumerable<HeightIndex> GetHeightIndicesWithCircle(TerrainComp compiler, Vector3 centerPos,
            float diameter)
        {
            List<HeightIndex> indices = new List<HeightIndex>();
            compiler.m_hmap.WorldToVertex(centerPos, out var cx, out var cy);
            var maxDistance = diameter / 2f / compiler.m_hmap.m_scale;
            var max = compiler.m_width + 1;
            Vector2 center = new Vector2((float)cx, (float)cy);
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    var distance = Vector2.Distance(center, new Vector2((float)j, (float)i));
                    if (distance > maxDistance) continue;
                    var distanceX = j - cx;
                    var distanceY = i - cy;
                    indices.Add(new HeightIndex()
                    {
                        Index = i * max + j,
                        Position = VertexToWorld(compiler.m_hmap, j, i),
                        DistanceWidth = distanceX / maxDistance,
                        DistanceDepth = distanceY / maxDistance,
                        Distance = distance / maxDistance
                    });
                }
            }

            return indices;
        }

        private static float GetX(int x, int y, float angle) => Mathf.Cos(angle) * x - Mathf.Sin(angle) * y;
        private static float GetY(int x, int y, float angle) => Mathf.Sin(angle) * x + Mathf.Cos(angle) * y;

        private static IEnumerable<HeightIndex> GetHeightIndicesWithRect(TerrainComp compiler, Vector3 centerPos,
            float width, float depth, float angle)
        {
            List<HeightIndex> indices = new List<HeightIndex>();
            compiler.m_hmap.WorldToVertex(centerPos, out var cx, out var cy);
            var maxWidth = width / 2f / compiler.m_hmap.m_scale;
            var maxDepth = depth / 2f / compiler.m_hmap.m_scale;
            var max = compiler.m_width + 1;
            for (int x = 0; x < max; x++)
            {
                for (int y = 0; y < max; y++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var distanceX = GetX(dx, dy, angle);
                    var distanceY = GetY(dx, dy, angle);
                    if (Mathf.Abs(distanceX) > maxWidth) continue;
                    if (Mathf.Abs(distanceY) > maxDepth) continue;
                    var distanceWidth = distanceX / maxWidth;
                    var distanceDepth = distanceY / maxDepth;
                    indices.Add(new HeightIndex()
                    {
                        Index = y * max + x,
                        Position = VertexToWorld(compiler.m_hmap, x, y),
                        DistanceWidth = distanceWidth,
                        DistanceDepth = distanceDepth,
                        Distance = Mathf.Max(Mathf.Abs(distanceWidth), Mathf.Abs(distanceDepth))
                    });
                }
            }

            return indices;
        }

        private static IEnumerable<PaintIndex> GetPaintIndicesWithRect(TerrainComp compiler, Vector3 centerPos,
            float width, float depth, float angle)
        {
            centerPos = new Vector3(centerPos.x - 0.5f, centerPos.y, centerPos.z - 0.5f);
            List<PaintIndex> indices = new List<PaintIndex>();
            compiler.m_hmap.WorldToVertex(centerPos, out var cx, out var cy);
            var maxWidth = width / 2f / compiler.m_hmap.m_scale;
            var maxDepth = depth / 2f / compiler.m_hmap.m_scale;
            var max = compiler.m_width;
            for (int x = 0; x < max; x++)
            {
                for (int y = 0; y < max; y++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var distanceX = GetX(dx, dy, angle);
                    var distanceY = GetY(dx, dy, angle);
                    if (Mathf.Abs(distanceX) > maxWidth) continue;
                    if (Mathf.Abs(distanceY) > maxDepth) continue;
                    indices.Add(new PaintIndex()
                    {
                        Index = y * max + x,
                        Position = VertexToWorld(compiler.m_hmap, x, y)
                    });
                }
            }

            return indices;
        }

        private static IEnumerable<PaintIndex> GetPaintIndicesWithCircle(TerrainComp compiler, Vector3 centerPos,
            float diameter)
        {
            centerPos = new Vector3(centerPos.x - 0.5f, centerPos.y, centerPos.z - 0.5f);
            List<PaintIndex> indices = new List<PaintIndex>();
            compiler.m_hmap.WorldToVertex(centerPos, out var cx, out var cy);
            var maxDistance = diameter / 2f / compiler.m_hmap.m_scale;
            var max = compiler.m_width;
            Vector2 center = new Vector2(cx, cy);
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    var distance = Vector2.Distance(center, new Vector2(j, i));
                    if (distance > maxDistance) continue;
                    indices.Add(new PaintIndex()
                    {
                        Index = i * max + j,
                        Position = VertexToWorld(compiler.m_hmap, j, i)
                    });
                }
            }

            return indices;
        }
    }
}
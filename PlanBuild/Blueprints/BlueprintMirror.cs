using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    /// <summary>
    ///     Mirroring of blueprint content across its local YZ plane. Always the same axis, so a mirrored
    ///     blueprint does not depend on the direction it was mirrored from.
    /// </summary>
    public static class BlueprintMirror
    {
        /// <summary>
        ///     Name suffix of mirrored copies, fixed English so it is recognized in any language
        /// </summary>
        public const string NameSuffix = " (mirrored)";

        /// <summary>
        ///     Turns around a piece's own Y axis tried to match its mirror image, no turn first
        /// </summary>
        private static readonly int[] CorrectionCandidates = { 0, 180, 90, 270 };

        private const float SnapPointTolerance = 0.02f;

        private static readonly Dictionary<string, int> Corrections = new Dictionary<string, int>();
        private static Dictionary<string, int> Overrides;

        public static Vector3 MirrorPosition(Vector3 position)
        {
            return new Vector3(-position.x, position.y, position.z);
        }

        /// <summary>
        ///     Rotation of a piece mirrored along with its position. A rotation cannot mirror the mesh,
        ///     so this alone is exact only for pieces symmetric from left to right (walls, floors, stairs, roofs).
        /// </summary>
        public static Quaternion MirrorRotation(Quaternion rotation)
        {
            return new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        /// <summary>
        ///     Mirrored rotation turned by the piece's correction around its own Y axis, which makes
        ///     pieces like angled beams or roof corners match their mirror image
        /// </summary>
        public static Quaternion MirrorRotation(Quaternion rotation, int correction)
        {
            return MirrorRotation(rotation) * YawRotation(correction);
        }

        /// <summary>
        ///     A quarter turn correction swaps the piece's own X and Z axes
        /// </summary>
        public static Vector3 MirrorScale(Vector3 scale, int correction)
        {
            return correction % 180 == 0 ? scale : new Vector3(scale.z, scale.y, scale.x);
        }

        /// <summary>
        ///     Mirror a rotation around Y given in degrees
        /// </summary>
        public static int MirrorYaw(int degrees)
        {
            return -degrees;
        }

        /// <summary>
        ///     Name of a mirrored copy: mirroring a mirrored copy again removes the suffix
        /// </summary>
        public static string MirroredName(string name)
        {
            return name.EndsWith(NameSuffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - NameSuffix.Length)
                : name + NameSuffix;
        }

        /// <summary>
        ///     Rotation around Y, built by hand since Quaternion.Euler is native code
        /// </summary>
        public static Quaternion YawRotation(int degrees)
        {
            float half = degrees * Mathf.Deg2Rad / 2f;
            return new Quaternion(0f, Mathf.Sin(half), 0f, Mathf.Cos(half));
        }

        /// <summary>
        ///     Turn around Y in degrees that makes a piece with these snap points look like its mirror
        ///     image, 0 if none does (no snap points, or pieces like spiral stairs)
        /// </summary>
        public static int FindCorrection(IList<Vector3> snapPoints)
        {
            if (snapPoints.Count == 0)
            {
                return 0;
            }
            foreach (int degrees in CorrectionCandidates)
            {
                Quaternion turn = YawRotation(degrees);
                if (snapPoints.All(p => ContainsPoint(snapPoints, MirrorPosition(turn * p))))
                {
                    return degrees;
                }
            }
            return 0;
        }

        private static bool ContainsPoint(IList<Vector3> points, Vector3 point)
        {
            return points.Any(p => (p - point).sqrMagnitude < SnapPointTolerance * SnapPointTolerance);
        }

        /// <summary>
        ///     Correction of a piece prefab, from the config overrides or its snap points, cached
        /// </summary>
        internal static int GetCorrection(string prefabName)
        {
            if (Corrections.TryGetValue(prefabName, out int degrees))
            {
                return degrees;
            }

            Overrides ??= ParseOverrides(Config.MirrorOverridesConfig.Value);
            if (!Overrides.TryGetValue(prefabName, out degrees))
            {
                degrees = FindCorrection(GetSnapPoints(prefabName));
            }
            Corrections[prefabName] = degrees;
            return degrees;
        }

        private static List<Vector3> GetSnapPoints(string prefabName)
        {
            var points = new List<Vector3>();
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
            if (prefab && prefab.TryGetComponent(out Piece piece))
            {
                var snapPoints = new List<Transform>();
                piece.GetSnapPoints(snapPoints);
                points.AddRange(snapPoints.Select(x => x.localPosition));
            }
            return points;
        }

        private static Dictionary<string, int> ParseOverrides(string value)
        {
            var overrides = new Dictionary<string, int>();
            foreach (string pair in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int degrees) && degrees % 90 == 0)
                {
                    overrides[parts[0].Trim()] = (degrees % 360 + 360) % 360;
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Invalid mirror rotation override '{pair.Trim()}', expected prefab:degrees with 0, 90, 180 or 270");
                }
            }
            return overrides;
        }

        /// <summary>
        ///     Forget all corrections, after the overrides changed
        /// </summary>
        internal static void ClearCorrections()
        {
            Corrections.Clear();
            Overrides = null;
        }

        /// <summary>
        ///     Mirror the children of a placement ghost in place, applying it twice restores them
        ///     as long as the corrections did not change in between
        /// </summary>
        internal static void MirrorChildren(Transform root)
        {
            foreach (Transform child in root)
            {
                // The stub's own marker sits on the center and has no mirror image
                if (child.name == "_GhostOnly")
                {
                    continue;
                }

                // The debug grid keeps its points in local space
                if (child.TryGetComponent(out LineRenderer line))
                {
                    var points = new Vector3[line.positionCount];
                    line.GetPositions(points);
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = MirrorPosition(points[i]);
                    }
                    line.SetPositions(points);
                    continue;
                }

                // Pieces are clones named after their prefab, snap points and the place collider are no pieces
                int correction = child.CompareTag("snappoint") || child.name == Blueprint.PlaceColliderName
                    ? 0
                    : GetCorrection(child.name.Split('(')[0]);
                child.localPosition = MirrorPosition(child.localPosition);
                child.localRotation = MirrorRotation(child.localRotation, correction);
                child.localScale = MirrorScale(child.localScale, correction);
            }
        }
    }
}

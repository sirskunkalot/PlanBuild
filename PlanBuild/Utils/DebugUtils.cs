using System.Linq;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class DebugUtils
    {
        public static void InitLaserGrid(GameObject gameObject, Bounds bounds)
        {
            Transform parent = gameObject.transform;
            Material defaultLine = Resources.FindObjectsOfTypeAll<Material>().First((k) => k.name == "Default-Line");
            int i = 0;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Color color = x == 0 && y == 0 ? Color.red : Color.gray;
                    CreateLaser(parent, i++, bounds, new Vector3(-1, x, y), new Vector3(1, x, y), defaultLine, color);
                    CreateLaser(parent, i++, bounds, new Vector3(x, -1, y), new Vector3(x, 1, y), defaultLine, color);
                    CreateLaser(parent, i++, bounds, new Vector3(x, y, -1), new Vector3(x, y, 1), defaultLine, color);
                }
            }
        }

        private static void CreateLaser(Transform parent, int i, Bounds bounds, Vector3 first, Vector3 second, Material material, Color color)
        {
            GameObject gameObject = new GameObject("laser_" + i, typeof(LineRenderer));
            gameObject.transform.SetParent(parent);
            LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = material;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            Vector3 extents = bounds.extents;
            lineRenderer.SetPositions(new Vector3[] { bounds.center + Vector3.Scale(first, extents), bounds.center + Vector3.Scale(second, extents) });
        }
    }
}
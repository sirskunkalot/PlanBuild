using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Utils
{
    /// <summary>
    ///     Circle outline like vanilla's CircleProjector, which ignores rotation and so can't show a slope
    /// </summary>
    internal class RingProjector : MonoBehaviour
    {
        private const float Speed = 0.1f;

        public GameObject prefab;
        public float radius = 5f;
        public int rotation;
        // Degrees the ring is tilted up towards its rotation
        public float slope;
        public LayerMask mask = 0;

        private readonly List<GameObject> segments = new List<GameObject>();

        private int SegmentCount => Mathf.Max((int)radius * 4, 3);

        private void Update()
        {
            if (segments.Count != SegmentCount)
            {
                CreateSegments();
            }

            Vector3 forward = Quaternion.Euler(0f, rotation, 0f) * Vector3.forward;
            float gradient = Mathf.Tan(slope * Mathf.Deg2Rad);
            float step = Mathf.PI * 2f / segments.Count;
            float offset = Time.time * Speed;

            for (int i = 0; i < segments.Count; i++)
            {
                float angle = i * step + offset;
                Vector3 local = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                Vector3 pos = transform.position + local;
                if (slope != 0f)
                {
                    pos.y += Vector3.Dot(local, forward) * gradient;
                }
                else if (Physics.Raycast(pos + Vector3.up * 500f, Vector3.down, out RaycastHit hitInfo, 1000f, mask.value))
                {
                    pos.y = hitInfo.point.y;
                }
                segments[i].transform.position = pos;
            }

            // Point each segment from its previous to its next neighbour, which follows the tilt
            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 previous = segments[i == 0 ? segments.Count - 1 : i - 1].transform.position;
                Vector3 next = segments[i == segments.Count - 1 ? 0 : i + 1].transform.position;
                segments[i].transform.rotation = Quaternion.LookRotation((next - previous).normalized, Vector3.up);
            }
        }

        private void CreateSegments()
        {
            DestroySegments();
            for (int i = 0; i < SegmentCount; i++)
            {
                segments.Add(Instantiate(prefab, transform.position, Quaternion.identity, transform));
            }
        }

        private void DestroySegments()
        {
            foreach (GameObject segment in segments)
            {
                Destroy(segment);
            }
            segments.Clear();
        }

        private void OnDestroy()
        {
            DestroySegments();
        }
    }
}

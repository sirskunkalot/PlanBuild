using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareProjector : CircleProjector
{
    public int resolution;
    public float step;
    public float offset;
    public Bounds bounds = new Bounds();
    private List<GameObject> m_segments = new List<GameObject>();

    private void Start()
    {
        CreateSegments();
    }

    private void Update()
    {
        bounds = new Bounds(transform.position, new Vector3(m_radius * 2, 0f, -m_radius * 2));
        int floor = Mathf.FloorToInt(m_radius);
        m_nrOfSegments = floor * 4 - floor * 4 % 4;
        resolution = m_nrOfSegments / 4;
        m_nrOfSegments += 8;
        CreateSegments();
        step = m_radius / resolution * 2f;
        offset = Mathf.Repeat(Time.time * 0.5f, step);
        for (int i = 0; i <= resolution + 1; i++)
        {
            float delta = i * step + offset - step;
            int chunk = i * 4;
            m_segments[chunk].transform.position = transform.TransformPoint(new Vector3(-m_radius + delta, -m_radius, 0f));
            m_segments[chunk].transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            m_segments[chunk + 1].transform.position = transform.TransformPoint(new Vector3(m_radius - delta, m_radius, 0f));
            m_segments[chunk + 1].transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        }
        for (int i = 0; i <= resolution + 1; i++)
        {
            float delta = i * step + offset - step;
            int chunk = i * 4;
            m_segments[chunk + 2].transform.position = transform.TransformPoint(new Vector3(-m_radius, m_radius - delta, 0f));
            m_segments[chunk + 2].transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            m_segments[chunk + 3].transform.position = transform.TransformPoint(new Vector3(m_radius, -m_radius + delta, 0f));
            m_segments[chunk + 3].transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }
    }
    private void CreateSegments()
    {
        if (m_segments.Count == m_nrOfSegments)
        {
            return;
        }

        foreach (GameObject segment in m_segments)
        {
            UnityEngine.Object.Destroy(segment);
        }

        m_segments.Clear();
        for (int i = 0; i < m_nrOfSegments; i++)
        {
            GameObject item = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.identity, base.transform);
            m_segments.Add(item);
        }
    }
    private void OnDrawGizmos()
    {
        if (m_segments.Count == 0)
        {
            bounds = new Bounds(transform.position, new Vector3(m_radius * 2, 0f, -m_radius * 2));
            ShowBounds();

            int floor = Mathf.FloorToInt(m_radius);
            m_nrOfSegments = floor * 4 - floor * 4 % 4;
            resolution = m_nrOfSegments / 4;
            m_nrOfSegments += 8;
            step = m_radius / resolution * 2f;
            offset = Mathf.Repeat(offset, step);
            for (int i = 0; i <= resolution + 1; i++)
            {
                float delta = i * step + offset - step;
                ShowPoint(-m_radius + delta, m_radius);  // up
                ShowPoint(m_radius - delta, -m_radius);  // down
            }
            for (int i = 0; i <= resolution + 1; i++)
            {
                float delta = i * step + offset - step;
                ShowPoint(m_radius, m_radius - delta);  // right
                ShowPoint(-m_radius, -m_radius + delta);  // left
            }
        }
        else
        {
            ShowBounds();
            foreach (var go in m_segments)
            {
                ShowPoint(go.transform.position);
            }
        }
    }

    private void ShowBounds()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawCube(bounds.center, bounds.size);
    }

    private void ShowPoint(float x, float y)
    {
        ShowPoint(transform.TransformPoint(new Vector3(x, y, 0f)));
    }

    private void ShowPoint(Vector3 point)
    {
        Vector3 square = point;
        Vector3 circle = square.normalized * m_radius;

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(square, 0.025f);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(circle, 0.025f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(square, circle);

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(circle, Vector2.zero);
    }
}
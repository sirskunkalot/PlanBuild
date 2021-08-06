using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareProjector : CircleProjector
{
    public int resolution;
    public float step;
    public float offset;
    private List<GameObject> segments = new List<GameObject>();

    private void Start()
    {
        CreateSegments();
    }

    private void Update()
    {
        m_nrOfSegments = Mathf.FloorToInt(m_radius * 4f);
        CreateSegments();
        resolution = m_nrOfSegments / 4;
        step = m_radius / resolution * 2f;
        offset = Mathf.Repeat(Time.time * 0.5f, step);
        for (int i = 0; i < resolution; i++)
        {
            float delta = i * step;
            int chunk = i * 4;
            segments[chunk].transform.position = transform.TransformPoint(new Vector3(m_radius - delta - offset, 0f, -m_radius));
            segments[chunk + 1].transform.position = transform.TransformPoint(new Vector3(-m_radius + delta + offset, 0f, m_radius));
            segments[chunk + 2].transform.position = transform.TransformPoint(new Vector3(m_radius, 0f, -delta + m_radius - offset));
            segments[chunk + 3].transform.position = transform.TransformPoint(new Vector3(-m_radius, 0f, delta - m_radius + offset));
        }
    }
    private void CreateSegments()
    {
        if (segments.Count == m_nrOfSegments)
        {
            return;
        }

        foreach (GameObject segment in segments)
        {
            UnityEngine.Object.Destroy(segment);
        }

        segments.Clear();
        for (int i = 0; i < m_nrOfSegments; i++)
        {
            GameObject item = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.identity, base.transform);
            segments.Add(item);
        }
    }
    private void OnDrawGizmos()
    {
        if (segments.Count == 0)
        {
            m_nrOfSegments = Mathf.FloorToInt(m_radius * 4f);
            resolution = m_nrOfSegments / 4;
            step = m_radius / resolution * 2f;
            offset = Mathf.Repeat(offset, step);
            for (int i = 0; i < resolution; i++)
            {
                float delta = i * step;
                ShowPoint(m_radius - delta - offset, -m_radius);
                ShowPoint(-m_radius + delta + offset, m_radius);
                ShowPoint(m_radius, -delta + m_radius - offset);
                ShowPoint(-m_radius, delta - m_radius + offset);
            }
        }
        else
        {
            foreach (var go in segments)
            {
                ShowPoint(go.transform.position);
            }
        }
    }

    private void ShowPoint(float x, float z)
    {
        ShowPoint(transform.TransformPoint(new Vector3(x, 0f, z)));
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
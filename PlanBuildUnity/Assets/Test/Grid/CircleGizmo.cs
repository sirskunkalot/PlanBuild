using UnityEngine;

public class CircleGizmo : MonoBehaviour {

	public int resolution = 10;
	public float radius = 2f;

	private void OnDrawGizmos() {
		float step = radius / resolution * 2;
		for (int i = 0; i <= resolution; i++) {
			ShowPoint(i * step - radius, -radius);
			ShowPoint(i * step - radius, radius);
		}
		for (int i = 1; i < resolution; i++) {
			ShowPoint(-radius, i * step - radius);
			ShowPoint(radius, i * step - radius);
		}
	}

	private void ShowPoint (float x, float y) {
		Vector2 square = transform.TransformPoint(new Vector2(x, y));
		Vector2 circle = square.normalized * radius;

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
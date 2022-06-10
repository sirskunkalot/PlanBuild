using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class ShapedProjector : MonoBehaviour
    {
        internal static bool ShowProjectors = true;

        private static GameObject _segment;

        private static GameObject SelectionSegment
        {
            get
            {
                if (!_segment)
                {
                    GameObject workbench = PrefabManager.Instance.GetPrefab("piece_workbench");
                    _segment = Instantiate(workbench.GetComponentInChildren<CircleProjector>().m_prefab);
                    _segment.SetActive(false);
                }

                return _segment;
            }
        }

        internal enum ProjectorShape
        {
            Circle, Square
        }

        private ProjectorShape Shape = ProjectorShape.Circle;
        private float Radius = 10f;
        private int Rotation;

        private CircleProjector Circle;
        private SquareProjector Square;

        private void Update()
        {
            if (ShowProjectors)
            {
                Enable();
            }
            if (!ShowProjectors)
            {
                Disable();
            }
        }

        protected void OnDestroy()
        {
            Disable();
        }

        public void Enable()
        {
            if (!ShowProjectors)
            {
                return;
            }

            if (Shape == ProjectorShape.Circle && Circle == null)
            {
                Circle = gameObject.AddComponent<CircleProjector>();
                Circle.m_prefab = SelectionSegment;
                Circle.m_prefab.SetActive(true);
                Circle.m_radius = Radius;
                Circle.m_nrOfSegments = (int)Radius * 4;
            }

            if (Shape == ProjectorShape.Square && Square == null)
            {
                Square = gameObject.AddComponent<SquareProjector>();
                Square.prefab = SelectionSegment;
                Square.radius = Radius;
                Square.rotation = Rotation;
            }
        }

        public void Disable()
        {
            if (Circle != null)
            {
                foreach (GameObject segment in Circle.m_segments)
                {
                    DestroyImmediate(segment);
                }
                DestroyImmediate(Circle);
            }

            if (Square != null)
            {
                Square.StopProjecting();
                DestroyImmediate(Square);
            }
        }

        public void SwitchShape()
        {
            if (Shape == ProjectorShape.Circle)
            {
                SetShape(ProjectorShape.Square);
            }
            else if (Shape == ProjectorShape.Square)
            {
                SetShape(ProjectorShape.Circle);
            }
        }

        public void SetShape(ProjectorShape newShape)
        {
            if (Shape == newShape)
            {
                return;
            }

            Disable();
            Shape = newShape;
            Enable();
        }

        public ProjectorShape GetShape()
        {
            return Shape;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void SetRadius(float newRadius)
        {
            if (Radius == newRadius)
            {
                return;
            }
            Radius = newRadius;

            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                Circle.m_radius = Radius;
                Circle.m_nrOfSegments = (int)Radius * 4;
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                Square.radius = Radius;
            }
        }

        public float GetRadius()
        {
            return Radius;
        }

        public void SetRotation(int newRotation)
        {
            if (Shape == ProjectorShape.Square && Square != null)
            {
                Rotation = newRotation;
                Square.rotation = Rotation;
            }
        }

        public int GetRotation()
        {
            return Rotation;
        }

        public void EnableMask()
        {
            if (Shape == ProjectorShape.Circle && Circle != null && Circle.m_mask != 2048)
            {
                Circle.m_mask = 2048;
            }

            if (Shape == ProjectorShape.Square && Square != null && Square.mask != 2048)
            {
                Square.mask = 2048;
            }
        }

        public void DisableMask()
        {
            if (Shape == ProjectorShape.Circle && Circle != null && Circle.m_mask != 0)
            {
                Circle.m_mask = 0;
            }

            if (Shape == ProjectorShape.Square && Square != null && Square.mask != 0)
            {
                Square.mask = 0;
            }
        }
    }
}
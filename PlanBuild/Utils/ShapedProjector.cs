using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class ShapedProjector : MonoBehaviour
    {
        internal static bool ShowProjectors = true;

        internal enum ProjectorShape
        {
            Circle, Square
        }
        
        private ProjectorShape Shape = ProjectorShape.Circle;
        private float Radius = 10f;

        private CircleProjector Circle;
        private SquareProjector Square;

        private GameObject SelectionSegment;

        private void Awake()
        {
            GameObject workbench = PrefabManager.Instance.GetPrefab("piece_workbench");
            SelectionSegment = Instantiate(workbench.GetComponentInChildren<CircleProjector>().m_prefab);
            SelectionSegment.SetActive(false);
        }

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

        internal void Enable()
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
                Circle.Start();
            }

            if (Shape == ProjectorShape.Square && Square == null)
            {
                Square = gameObject.AddComponent<SquareProjector>();
                Square.prefab = SelectionSegment;
                Square.radius = Radius;
                Square.Start();
            }
        }

        internal void Disable()
        {
            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                foreach (GameObject segment in Circle.m_segments)
                {
                    Destroy(segment);
                }
                Destroy(Circle);
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                Square.StopProjecting();
                Destroy(Square);
            }
        }

        internal void SwitchShape()
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

        internal void SetShape(ProjectorShape newShape)
        {
            if (Shape == newShape)
            {
                return;
            }

            Disable();
            Shape = newShape;
            Enable();
        }

        internal ProjectorShape GetShape()
        {
            return Shape;
        }

        internal void SetRadius(float newRadius)
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

        internal float GetRadius()
        {
            return Radius;
        }
    }
}

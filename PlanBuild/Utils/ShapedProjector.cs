using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class ShapedProjector : MonoBehaviour
    {
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
        // Half the square's depth, Radius is half its width
        private float DepthRadius = 10f;
        private int Rotation;
        // Degrees the shape is tilted up towards its rotation
        private float Slope;

        private RingProjector Circle;
        private SquareProjector Square;

        public void Enable()
        {
            if (Shape == ProjectorShape.Circle && Circle == null)
            {
                Circle = gameObject.AddComponent<RingProjector>();
                Circle.prefab = SelectionSegment;
                Circle.prefab.SetActive(true);
                Circle.radius = Radius;
                Circle.rotation = Rotation;
                Circle.slope = Slope;
            }

            if (Shape == ProjectorShape.Square && Square == null)
            {
                Square = gameObject.AddComponent<SquareProjector>();
                Square.prefab = SelectionSegment;
                Square.radius = Radius;
                Square.depthRadius = DepthRadius;
                Square.rotation = Rotation;
                Square.slope = Slope;
            }
        }

        public void Disable()
        {
            if (Circle != null)
            {
                // RingProjector removes its segments itself
                DestroyImmediate(Circle);
            }

            if (Square != null)
            {
                Square.StopProjecting();
                DestroyImmediate(Square);
            }
        }

        public bool IsEnabled()
        {
            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                return true;
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                return true;
            }

            return false;
        }

        public void ToggleEnabled()
        {
            if (IsEnabled())
            {
                Disable();
            }
            else
            {
                Enable();
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
            SetRadius(newRadius, newRadius);
        }

        public void SetRadius(float newRadius, float newDepthRadius)
        {
            Radius = newRadius;
            DepthRadius = newDepthRadius;

            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                Circle.radius = Radius;
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                Square.radius = Radius;
                Square.depthRadius = DepthRadius;
            }
        }

        public float GetRadius()
        {
            return Radius;
        }

        public float GetDepthRadius()
        {
            return DepthRadius;
        }

        /// <summary>
        ///     Radius of a circle around the whole shape, the corners of a square lie outside its radius
        /// </summary>
        public float GetOuterRadius()
        {
            if (Shape == ProjectorShape.Square)
            {
                return Mathf.Sqrt(Radius * Radius + DepthRadius * DepthRadius);
            }

            return Radius;
        }

        public void SetRotation(int newRotation)
        {
            Rotation = newRotation;

            // The circle only needs the rotation as the direction of its slope
            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                Circle.rotation = Rotation;
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                Square.rotation = Rotation;
            }
        }

        public int GetRotation()
        {
            return Rotation;
        }

        public void SetSlope(float newSlope)
        {
            Slope = newSlope;

            if (Shape == ProjectorShape.Circle && Circle != null)
            {
                Circle.slope = Slope;
            }

            if (Shape == ProjectorShape.Square && Square != null)
            {
                Square.slope = Slope;
            }
        }

        public float GetSlope()
        {
            return Slope;
        }

        public void EnableMask()
        {
            if (Shape == ProjectorShape.Circle && Circle != null && Circle.mask != 2048)
            {
                Circle.mask = 2048;
            }

            if (Shape == ProjectorShape.Square && Square != null && Square.mask != 2048)
            {
                Square.mask = 2048;
            }
        }

        public void DisableMask()
        {
            if (Shape == ProjectorShape.Circle && Circle != null && Circle.mask != 0)
            {
                Circle.mask = 0;
            }

            if (Shape == ProjectorShape.Square && Square != null && Square.mask != 0)
            {
                Square.mask = 0;
            }
        }
    }
}
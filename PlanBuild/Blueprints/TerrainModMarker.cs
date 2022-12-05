using System;
using System.Globalization;
using System.Text;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class TerrainModMarker : MonoBehaviour, Interactable, Hoverable
    {
        private const string ShapeProperty = "shape";
        private const string RadiusProperty = "radius";
        private const string RotationProperty = "rotation";
        private const string SmoothProperty = "smooth";
        private const string PaintProperty = "paint";

        private ZNetView ZNetView;
        private ShapedProjector Projector;

        public void Awake()
        {
            if (ZNetView.m_forceDisableInit)
            {
                Destroy(this);
                return;
            }

            ZNetView = GetComponent<ZNetView>();
            ZNetView.Register<string, string>("SetProperty", RPC_SetProperty);

            Projector = GetComponent<ShapedProjector>();
            Projector.Enable();

            if (string.IsNullOrEmpty(GetProperty(ShapeProperty)))
            {
                SetProperty(ShapeProperty, "Circle");
            }
            if (string.IsNullOrEmpty(GetProperty(RadiusProperty)))
            {
                SetProperty(RadiusProperty, "3");
            }
            if (string.IsNullOrEmpty(GetProperty(RotationProperty)))
            {
                SetProperty(RotationProperty, "0");
            }
            if (string.IsNullOrEmpty(GetProperty(SmoothProperty)))
            {
                SetProperty(SmoothProperty, "0.3");
            }
        }

        public string GetProperty(string property)
        {
            if (!ZNetView.IsValid())
            {
                return null;
            }
            return ZNetView.GetZDO().GetString(property);
        }

        public void SetProperty(string property, string value)
        {
            ZNetView.InvokeRPC("SetProperty", property, value);
        }

        public void RPC_SetProperty(long sender, string property, string value)
        {
            if (!ZNetView.IsOwner())
            {
                return;
            }

            ZNetView.GetZDO().Set(property, value);

            if (property.Equals(ShapeProperty, StringComparison.Ordinal))
            {
                if (value.Equals("Circle", StringComparison.OrdinalIgnoreCase))
                {
                    Projector.SetShape(ShapedProjector.ProjectorShape.Circle);
                }

                if (value.Equals("Square", StringComparison.OrdinalIgnoreCase))
                {
                    Projector.SetShape(ShapedProjector.ProjectorShape.Square);
                }
            }

            if (property.Equals(RadiusProperty, StringComparison.Ordinal))
            {
                Projector.SetRadius(float.Parse(value, CultureInfo.InvariantCulture));
            }

            if (property.Equals(RotationProperty, StringComparison.Ordinal))
            {
                Projector.SetRotation(int.Parse(value));
            }
        }

        public string GetHoverName()
        {
            return "Terrain modification marker";
        }

        public string GetHoverText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[<color=yellow>$KEY_Use</color>] $hud_terrainmod_interact\n");
            sb.Append($"$gui_terrainmod_shape: {GetProperty(ShapeProperty)} \n");
            sb.Append($"$gui_terrainmod_radius: {GetProperty(RadiusProperty)}\n");
            sb.Append($"$gui_terrainmod_rotation: {GetProperty(RotationProperty)}\n");
            sb.Append($"$gui_terrainmod_smooth: {GetProperty(SmoothProperty)}\n");
            sb.Append($"$gui_terrainmod_paint: {GetProperty(PaintProperty)}\n");
            return Localization.instance.Localize(sb.ToString());
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (!(user is Player player))
            {
                return false;
            }

            /*if (hold)
            {
                if (Time.time - LastUseTime < HoldRepeatInterval)
                {
                    return false;
                }
                LastUseTime = Time.time;
                Projector.ToggleEnabled();
                return true;
            }

            if (GetProperty(ShapeProperty).Equals("circle", StringComparison.Ordinal))
            {
                SetProperty(ShapeProperty, "square");
            }
            else if (GetProperty(ShapeProperty).Equals("square", StringComparison.Ordinal))
            {
                SetProperty(ShapeProperty, "circle");
            }*/

            TerrainModGUI.Instance.Show(
                GetProperty(ShapeProperty), GetProperty(RadiusProperty),
                GetProperty(RotationProperty), GetProperty(SmoothProperty),
                GetProperty(PaintProperty),
                (shape, radius, rotation, smooth, paint) =>
                {
                    if (!string.IsNullOrEmpty(shape))
                    {
                        SetProperty(ShapeProperty, shape.ToLowerInvariant());
                    }
                    if (!string.IsNullOrEmpty(radius))
                    {
                        SetProperty(RadiusProperty, radius);
                    }
                    if (!string.IsNullOrEmpty(rotation))
                    {
                        SetProperty(RotationProperty, rotation);
                    }
                    if (!string.IsNullOrEmpty(smooth))
                    {
                        SetProperty(SmoothProperty, smooth);
                    }
                    if (!string.IsNullOrEmpty(paint))
                    {
                        if (paint.Equals("None", StringComparison.OrdinalIgnoreCase))
                        {
                            SetProperty(PaintProperty, string.Empty);
                        }
                        else
                        {
                            SetProperty(PaintProperty, paint);   
                        }
                    }
                },
                () =>
                {

                });
            
            return true;
        }
        
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

    }
}

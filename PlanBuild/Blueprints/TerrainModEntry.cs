using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class TerrainModEntry
    {
        public string line;
        public string shape;
        public float posX;
        public float posY;
        public float posZ;
        public float radius;
        public float smooth;
        public string paint;

        public TerrainModEntry(string line)
        {
            this.line = line;
            string[] parts = line.Split(';');
            shape = parts[0];
            posX = InvariantFloat(parts[1]);
            posY = InvariantFloat(parts[2]);
            posZ = InvariantFloat(parts[3]);
            radius = InvariantFloat(parts[4]);
            smooth = InvariantFloat(parts[5]);
            paint = parts[6];
        }

        public TerrainModEntry(string shape, Vector3 pos, float radius, float smooth, TerrainModifier.PaintType paint)
        {
            line = string.Join(";",
                shape.ToLowerInvariant(), InvariantString(pos.x), InvariantString(pos.y), InvariantString(pos.z),
                InvariantString(radius), InvariantString(smooth), paint.ToString());
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            this.shape = shape;
            this.radius = radius;
            this.smooth = smooth;
            this.paint = paint.ToString();
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        internal static string InvariantString(float f)
        {
            return f.ToString(NumberFormatInfo.InvariantInfo);
        }

        internal static float InvariantFloat(string s)
        {
            if (s.StartsWith("-,"))
            {
                s = s.Replace("-,", "-0,");
            }
            else if (s.StartsWith(","))
            {
                s = "0" + s;
            }
            return float.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }
    }
}
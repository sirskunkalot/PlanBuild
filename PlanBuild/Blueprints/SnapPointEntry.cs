using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class SnapPointEntry
    {
        public string line;
        public float posX;
        public float posY;
        public float posZ;

        public SnapPointEntry(string line)
        {
            this.line = line;
            string[] parts = line.Split(';');
            posX = InvariantFloat(parts[0]);
            posY = InvariantFloat(parts[1]);
            posZ = InvariantFloat(parts[2]);
        }

        public SnapPointEntry(Vector3 pos)
        {
            line = string.Join(";", InvariantString(pos.x), InvariantString(pos.y), InvariantString(pos.z));
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        /// <summary>
        ///     New entry mirrored across the blueprint's local YZ plane, see <see cref="BlueprintMirror"/>
        /// </summary>
        public SnapPointEntry Mirrored()
        {
            return new SnapPointEntry(BlueprintMirror.MirrorPosition(GetPosition()));
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
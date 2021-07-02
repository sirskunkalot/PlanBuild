using System;
using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class PieceEntry
    {
        public string line { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public float posZ { get; set; }
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
        public float rotW { get; set; }
        public string additionalInfo { get; set; }

        public static PieceEntry FromBlueprint(string line)
        {
            // backwards compatibility
            if (line.IndexOf(',') > 0)
            {
                line = line.Replace(',', '.');
            }

            var parts = line.Split(';');
            string name = parts[0];
            string category = parts[1];
            Vector3 pos = new Vector3(InvariantFloat(parts[2]), InvariantFloat(parts[3]), InvariantFloat(parts[4]));
            Quaternion rot = new Quaternion(InvariantFloat(parts[5]), InvariantFloat(parts[6]), InvariantFloat(parts[7]), InvariantFloat(parts[8]));
            string additionalInfo = parts[9];
            return new PieceEntry(name, category, pos, rot, additionalInfo);
        }

        public static PieceEntry FromVBuild(string line)
        {
            // backwards compatibility
            if (line.IndexOf(',') > 0)
            {
                line = line.Replace(',', '.');
            }

            var parts = line.Split(' ');
            string name = parts[0];
            float x = InvariantFloat(parts[1]);
            float y = InvariantFloat(parts[2]);
            float z = InvariantFloat(parts[3]);
            float w = InvariantFloat(parts[4]);
            float x2 = InvariantFloat(parts[5]);
            float y2 = InvariantFloat(parts[6]);
            float z2 = InvariantFloat(parts[7]);
            string category = "Building";
            Quaternion rot = new Quaternion(x, y, z, w);
            Vector3 pos = new Vector3(x2, y2, z2);
            string additionalInfo = string.Empty;
            return new PieceEntry(name, category, pos, rot, additionalInfo);
        }

        public PieceEntry(string name, string category, Vector3 pos, Quaternion rot, string additionalInfo)
        {
            this.name = name.Split('(')[0];
            this.category = category;
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            rotX = rot.x;
            rotY = rot.y;
            rotZ = rot.z;
            rotW = rot.w;
            this.additionalInfo = additionalInfo;

            line = string.Join(";",
                this.name, this.category,
                InvariantString(posX), InvariantString(posY), InvariantString(posZ),
                InvariantString(rotX), InvariantString(rotY), InvariantString(rotZ), InvariantString(rotW),
                this.additionalInfo);
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }

        internal static float InvariantFloat(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0f;
            }
            else
            {
                return float.Parse(s, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
            }
        }

        internal static string InvariantString(float f)
        {
            return f.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}
using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class PieceEntry
    {
        public PieceEntry(string line)
        {
            // backwards compatibility
            if (line.IndexOf(',') > 0)
            {
                line.Replace(',', '.');
            }

            this.line = line;
            var parts = line.Split(';');
            name = parts[0];
            category = parts[1];
            posX = InvariantFloat(parts[2]);
            posY = InvariantFloat(parts[3]);
            posZ = InvariantFloat(parts[4]);
            rotX = InvariantFloat(parts[5]);
            rotY = InvariantFloat(parts[6]);
            rotZ = InvariantFloat(parts[7]);
            rotW = InvariantFloat(parts[8]);
            additionalInfo = parts[9];
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

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }

        private float InvariantFloat(string s)
        {
            return float.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }

        private string InvariantString(float f)
        {
            return f.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}
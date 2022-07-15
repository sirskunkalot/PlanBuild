using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class PieceEntry
    {
        public string line;
        public string name;
        public string category;
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;
        public string additionalInfo;
        public float scaleX;
        public float scaleY;
        public float scaleZ;

        public static PieceEntry FromBlueprint(string line)
        {
            // backwards compatibility
            if (line.IndexOf(',') > -1)
            {
                line = line.Replace(',', '.');
            }

            var parts = line.Split(';');
            string name = parts[0];
            string category = parts[1];
            float posX = InvariantFloat(parts[2]);
            float posY = InvariantFloat(parts[3]);
            float posZ = InvariantFloat(parts[4]);
            float rotX = InvariantFloat(parts[5]);
            float rotY = InvariantFloat(parts[6]);
            float rotZ = InvariantFloat(parts[7]);
            float rotW = InvariantFloat(parts[8]);
            Vector3 pos = new Vector3(posX, posY, posZ);
            Quaternion rot = new Quaternion(rotX, rotY, rotZ, rotW).normalized;
            string additionalInfo = parts[9];
            if (string.IsNullOrEmpty(additionalInfo) || additionalInfo.Equals("\"\""))
            {
                additionalInfo = null;
            }
            else
            {
                try
                {
                    additionalInfo = SimpleJson.SimpleJson.DeserializeObject<string>(additionalInfo);
                }
                catch
                {
                    additionalInfo = parts[9];
                }
            }
            Vector3 scale = Vector3.one;
            if (parts.Length > 10)
            {
                float scaleX = InvariantFloat(parts[10]);
                float scaleY = InvariantFloat(parts[11]);
                float scaleZ = InvariantFloat(parts[12]);
                scale = new Vector3(scaleX, scaleY, scaleZ);
            }
            return new PieceEntry(name, category, pos, rot, additionalInfo, scale);
        }

        public static PieceEntry FromVBuild(string line)
        {
            // backwards compatibility
            if (line.IndexOf(',') > -1)
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
            Quaternion rot = new Quaternion(x, y, z, w).normalized;
            Vector3 pos = new Vector3(x2, y2, z2);
            string additionalInfo = string.Empty;
            return new PieceEntry(name, category, pos, rot, additionalInfo, Vector3.one);
        }

        public PieceEntry(string name, string category, Vector3 pos, Quaternion rot, string additionalInfo, Vector3 scale)
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
            if (!string.IsNullOrEmpty(this.additionalInfo))
            {
                this.additionalInfo = string.Concat(this.additionalInfo.Split(';'));
            }
            scaleX = scale.x;
            scaleY = scale.y;
            scaleZ = scale.z;

            line = string.Join(";",
                this.name, this.category,
                InvariantString(posX), InvariantString(posY), InvariantString(posZ),
                InvariantString(rotX), InvariantString(rotY), InvariantString(rotZ), InvariantString(rotW),
                SimpleJson.SimpleJson.SerializeObject(additionalInfo),
                InvariantString(scaleX), InvariantString(scaleY), InvariantString(scaleZ));
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }

        public Vector3 GetScale()
        {
            return new Vector3(scaleX, scaleY, scaleZ);
        }

        internal static float InvariantFloat(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0f;
            }
            return float.Parse(s, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
        }

        internal static string InvariantString(float f)
        {
            return f.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}
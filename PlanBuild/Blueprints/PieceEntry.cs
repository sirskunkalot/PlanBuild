using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    public class PieceEntry
    {

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
            bool commaValid = 2.5f.ToString().Contains(",");
            var parts = line.Split(' ');
            string name = parts[0];
            float x = AdvancedParse(parts[1], commaValid);
            float y = AdvancedParse(parts[2], commaValid);
            float z = AdvancedParse(parts[3], commaValid);
            float w = AdvancedParse(parts[4], commaValid);
            float x2 = AdvancedParse(parts[5], commaValid);
            float y2 = AdvancedParse(parts[6], commaValid);
            float z2 = AdvancedParse(parts[7], commaValid);
            string category = "Building";
            Quaternion rot = new Quaternion(x, y, z, w);
            Vector3 pos = new Vector3(x2, y2, z2);
            string additionalInfo = "";
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

        internal static float InvariantFloat(string s)
        {
            if(s.StartsWith("-,"))
            {
                s = s.Replace("-,", "-0,");
            } else if(s.StartsWith(","))
            {
                s = "0" + s;
            }
            return float.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }

        internal static string InvariantString(float f)
        {
            return f.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static float AdvancedParse(string value, bool commaValid)
        {
            if (value.Contains("E-"))
            {
                value = value.Replace("E-", "");
            }
            float result;
            if (value.Contains(",") && !commaValid)
            {
                CultureInfo provider = CultureInfo.CreateSpecificCulture("fr-FR");
                if (float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, provider, out result))
                {
                    return result;
                } 
            }
            else if (value.Contains(".") && commaValid)
            {
                CultureInfo provider = CultureInfo.CreateSpecificCulture("en-GB");
                if (float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, provider, out result))
                {
                    return result;
                } 
            }
            else if (!commaValid && value.Contains("."))
            {
                CultureInfo provider = CultureInfo.CreateSpecificCulture("en-GB");
                if (float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, provider, out result))
                {
                    return result;
                } 
            }
            else if (commaValid && value.Contains(","))
            {
                CultureInfo provider = CultureInfo.CreateSpecificCulture("fr-FR");
                if (float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, provider, out result))
                {
                    return result;
                } 
            }
            else
            {
                if (float.TryParse(value, out result))
                {
                    return result;
                } 
            }
            return 0f;
        }
    }
}
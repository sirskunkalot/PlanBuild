using UnityEngine;

namespace PlanBuild.KitBash
{ 
    public class KitBashSourceConfig
    {
        public string name;
        public string targetParentPath;
        public string sourcePrefab;
        public string sourcePath;
        public string materialPrefab;
        public string materialPath;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        internal Vector3 scale = Vector3.one;
        internal int[] materialRemap; 

        public override string ToString()
        {
            return $"KitBashSource(name={name},sourcePrefab={sourcePrefab},sourcePath={sourcePath})";
        }
    } 
}

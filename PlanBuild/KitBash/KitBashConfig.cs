using System.Collections.Generic;

namespace PlanBuild.KitBash
{
    internal class KitBashConfig
    {
        public List<string> boxColliderPaths = new List<string>();
        public List<KitBashSourceConfig> KitBashSources = new List<KitBashSourceConfig>();

        public bool FixReferences { get; internal set; }
    }
}
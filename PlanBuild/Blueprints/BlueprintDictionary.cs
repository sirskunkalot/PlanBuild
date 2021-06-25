using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanBuild.Blueprints
{
    internal class BlueprintDictionary : Dictionary<string, Blueprint>
    {
        /// <summary>
        ///     Location of this list
        /// </summary>
        private BlueprintLocation Location;

        /// <summary>
        ///     Create a location savvy BlueprintDictionary
        /// </summary>
        /// <param name="location"></param>
        public BlueprintDictionary(BlueprintLocation location)
        {
            Location = location;
        }

        /// <summary>
        ///     Create a <see cref="BlueprintDictionary" /> from a <see cref="ZPackage" />
        /// </summary>
        /// <param name="zpkg"></param>
        /// <returns></returns>
        public static BlueprintDictionary FromZPackage(ZPackage zpkg, BlueprintLocation location)
        {
            Jotunn.Logger.LogDebug("Deserializing blueprint list from ZPackage");

            var ret = new BlueprintDictionary(location);

            var numBlueprints = zpkg.ReadInt();
            while (numBlueprints > 0)
            {
                Blueprint bp = Blueprint.FromZPackage(zpkg.ReadPackage());
                ret.Add(bp.ID, bp);
                numBlueprints--;

                Jotunn.Logger.LogDebug(bp.ID);
            }

            return ret;
        }

        /// <summary>
        ///     Create a <see cref="ZPackage" /> of this <see cref="BlueprintDictionary"/>
        /// </summary>
        /// <returns></returns>
        public ZPackage ToZPackage()
        {
            Jotunn.Logger.LogDebug("Serializing blueprint list to ZPackage");

            ZPackage package = new ZPackage();

            package.Write(this.Count());
            foreach (var entry in this)
            {
                Jotunn.Logger.LogDebug($"{entry.Key}");
                package.Write(entry.Value.ToZPackage());
            }

            return package;
        }

        public new void Add(string id, Blueprint blueprint) 
        {
            base.Add(id, blueprint);

            // If the GUI is available, also add the Blueprint to it
            if (BlueprintGUI.IsAvailable())
            {
                switch (Location)
                {
                    case BlueprintLocation.Local:
                        BlueprintGUI.Instance.LocalTab.ListDisplay.AddBlueprint(id, blueprint);
                        break;
                    case BlueprintLocation.Server:
                        BlueprintGUI.Instance.ServerTab.ListDisplay.AddBlueprint(id, blueprint);
                        break;
                    default:
                        break;
                }
            }
        }

        public new void Remove(string id)
        {
            base.Remove(id);

            // If the GUI is available, also remove the BP from there
            if (BlueprintGUI.IsAvailable())
            {
                switch (Location)
                {
                    case BlueprintLocation.Local:
                        BlueprintGUI.Instance.LocalTab.ListDisplay.RemoveBlueprint(id);
                        break;
                    case BlueprintLocation.Server:
                        BlueprintGUI.Instance.ServerTab.ListDisplay.RemoveBlueprint(id);
                        break;
                    default:
                        break;
                }
            }
        }

        public new void Clear()
        {
            base.Clear();

            if (BlueprintGUI.IsAvailable())
            {
                BlueprintGUI.Instance.ClearBlueprints(Location);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Count} blueprint{(Count == 1 ? "" : "s")}");
            foreach (var entry in this.OrderBy(x => x.Key))
            {
                sb.AppendLine(entry.Value.ToString());
            }

            return sb.ToString();
        }
    }
}

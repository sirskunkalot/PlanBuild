using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanBuild.Blueprints
{
    internal class BlueprintList : Dictionary<string, Blueprint>
    {

        /// <summary>
        ///     Create a <see cref="BlueprintList" /> from a <see cref="ZPackage" />
        /// </summary>
        /// <param name="zpkg"></param>
        /// <returns></returns>
        public static BlueprintList FromZPackage(ZPackage zpkg)
        {
            Jotunn.Logger.LogDebug("Deserializing blueprint list from ZPackage");

            var ret = new BlueprintList();

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
        ///     Create a <see cref="ZPackage" /> of this <see cref="BlueprintList"/>
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Count} blueprints");
            foreach (var entry in this.OrderBy(x => x.Key))
            {
                sb.AppendLine($"{entry.Key} ({entry.Value.GetPieceCount()} pieces)");
            }

            return sb.ToString();
        }
    }
}

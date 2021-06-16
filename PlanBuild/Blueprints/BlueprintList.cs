using System.Collections.Generic;
using System.Linq;

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
                string id = zpkg.ReadString();
                Blueprint bp = Blueprint.FromBlob(id, zpkg.ReadByteArray());
                ret.Add(id, bp);
                numBlueprints--;

                Jotunn.Logger.LogDebug(id);
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

                package.Write(entry.Key);
                package.Write(entry.Value.ToBlob());
            }

            return package;
        }
    }
}

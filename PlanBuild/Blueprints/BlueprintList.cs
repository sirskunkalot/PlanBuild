using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanBuild.Blueprints
{
    internal class BlueprintList : Dictionary<string, Blueprint>
    {
/*
        /// <summary>
        ///     Create a <see cref="BlueprintList" /> from a <see cref="ZPackage" />
        /// </summary>
        /// <param name="zpkg"></param>
        /// <returns></returns>
        public static BlueprintList FromZPackage(ZPackage zpkg)
        {
            Jotunn.Logger.LogDebug("Deserializing portal list from ZPackage");

            var ret = new BlueprintList();

            var numBlueprints = zpkg.ReadInt();

            while (numBlueprints > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Jotunn.Logger.LogDebug($"{portalName}@{portalPosition}");
                ret.Add(new Portal(portalPosition, portalName, true));

                numBlueprints--;
            }

            var numUnconnectedPortals = zpkg.ReadInt();

            while (numUnconnectedPortals > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Logger.LogDebug($"{portalName}@{portalPosition}");
                ret.Add(new Portal(portalPosition, portalName, false));

                numUnconnectedPortals--;
            }

            return ret;
        }

        /// <summary>
        ///     Create a <see cref="ZPackage" /> of this <see cref="BlueprintList"/>
        /// </summary>
        /// <returns></returns>
        public ZPackage ToZPackage()
        {
            Jotunn.Logger.LogDebug("Serializing portal list to ZPackage");

            var package = new ZPackage();

            var connected = this.Where(x => x.m_con);

            package.Write(connected.Count());
            foreach (var connectedPortal in connected)
            {
                Jotunn.Logger.LogDebug($"{connectedPortal.m_tag}@{connectedPortal.m_pos}");
                package.Write(connectedPortal.m_pos);
                package.Write(connectedPortal.m_tag);
            }

            var unconnected = this.Where(x => !x.m_con);

            package.Write(unconnected.Count());
            foreach (var unconnectedPortal in unconnected)
            {
                Jotunn.Logger.LogDebug($"{unconnectedPortal.m_tag}@{unconnectedPortal.m_pos}");
                package.Write(unconnectedPortal.m_pos);
                package.Write(unconnectedPortal.m_tag);
            }

            return package;
        }*/
    }
}

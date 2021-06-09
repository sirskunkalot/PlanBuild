using System.Collections.Generic;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class ZDOIDSet : HashSet<ZDOID>
    {
        public static ZDOIDSet From(ZPackage package)
        {
            ZDOIDSet result = new ZDOIDSet();
            int size = package.ReadInt();
            for (int i = 0; i < size; i++)
            {
                result.Add(package.ReadZDOID());
            }
            return result;
        }

        public ZPackage ToZPackage()
        {
            var package = new ZPackage();
            package.Write(this.Count());
            foreach (ZDOID zdoid in this)
            {
                package.Write(zdoid);
            }
            return package;
        }
    }
}
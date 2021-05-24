using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Blueprints;
using System;
using System.Globalization;
using UnityEngine;

namespace PlanBuildTest
{
    [TestClass]
    public class BlueprintParsing
    {
        [TestMethod]
        public void ParseBlueprint()
        {
            PieceEntry pieceEntry = PieceEntry.FromVBuild("wood_beam_45  -,55557  -,83147 -20,08298 1,177017 31,44012"); 
            Assert.AreEqual(pieceEntry.GetPosition(), new Vector3(-20.08298f, 1.177017f, 31.44012f));
        }

        [TestMethod]
        public void ParseBlueprint2()
        {
            PieceEntry pieceEntry = PieceEntry.FromVBuild("wood_beam_45  -.55557  -.83147 -20.08298 1.177017 31.44012");

            Assert.AreEqual(pieceEntry.GetPosition(), new Vector3(-20.08298f, 1.177017f, 31.44012f));
        }

    }
}

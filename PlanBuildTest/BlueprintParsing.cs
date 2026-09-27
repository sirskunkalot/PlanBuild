using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    [TestClass]
    public class BlueprintParsing
    {
        [TestMethod]
        public void ParsePieceEntry_VBuild_1()
        {
            PieceEntry pieceEntry = PieceEntry.FromVBuild("wood_beam_45  -,55557  -,83147 -20,08298 1,177017 31,44012"); 
            Assert.AreEqual(pieceEntry.GetPosition(), new Vector3(-20.08298f, 1.177017f, 31.44012f));
        }

        [TestMethod]
        public void ParsePieceEntry_VBuild_2()
        {
            PieceEntry pieceEntry = PieceEntry.FromVBuild("wood_beam_45  -.55557  -.83147 -20.08298 1.177017 31.44012");

            Assert.AreEqual(pieceEntry.GetPosition(), new Vector3(-20.08298f, 1.177017f, 31.44012f));
        }

        [TestMethod]
        public void ParseTerrainModEntry_NegativeRotation_AnyCultureMinus()
        {
            // Integers used to be written in the system culture
            Assert.AreEqual(-90, new TerrainModEntry("square;0;0;0;3;-90;0.3;").rotation);
            Assert.AreEqual(-90, new TerrainModEntry("square;0;0;0;3;−90;0.3;").rotation);
            Assert.AreEqual(-90, new TerrainModEntry("square;0;0;0;3;‎-90;0.3;").rotation);
            Assert.AreEqual(-90, new TerrainModEntry("square;0;0;0;3;؜-90;0.3;").rotation);
        }

        [TestMethod]
        public void WriteTerrainModEntry_InvariantRotation()
        {
            var entry = new TerrainModEntry("square", Vector3.zero, 3f, -90, 0.3f, string.Empty);
            StringAssert.Contains(entry.line, ";-90;");
            Assert.AreEqual(-90, new TerrainModEntry(entry.line).rotation);
        }

        [TestMethod]
        [ExpectedException(typeof(System.FormatException))]
        public void ParseTerrainModEntry_InvalidRotation_Throws()
        {
            _ = new TerrainModEntry("square;0;0;0;3;abc;0.3;");
        }

      // [TestMethod]
      // public void ParseBlueprint_V1()
      // {
      //     Blueprint.logLoading = false;
      //     Blueprint blueprint = Blueprint.FromPath("resources/TestBox_V1.blueprint");
      //     Assert.AreEqual(blueprint.Name, "TestBox_V1");
      //     Assert.IsTrue(blueprint.Load());
      //     Assert.AreEqual(blueprint.SnapPoints.Length, 0);
      //     Assert.AreEqual(blueprint.PieceEntries.Length, 6);
      // }
      //  
      // [TestMethod]
      // public void ParseBlueprint_V2()
      // {
      //     Blueprint.logLoading = false;
      //     Blueprint blueprint = Blueprint.FromPath("resources/TestBox_V2.blueprint");
      //     Assert.AreEqual(blueprint.Name, "TestBox_V2");
      //     Assert.IsTrue(blueprint.Load());
      //     Assert.AreEqual(blueprint.Name, "Custom Name");
      //     Assert.AreEqual(blueprint.Description, "Description with\nnewlines and such :)"); 
      //     Assert.AreEqual(blueprint.SnapPoints.Length, 0);
      //     Assert.AreEqual(blueprint.PieceEntries.Length, 6);
      // }

    }
}

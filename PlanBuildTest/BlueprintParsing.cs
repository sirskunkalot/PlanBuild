using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Blueprints;
using System;
using System.Globalization;
using System.IO;
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

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

        // Quaternion.Euler is native Unity code, build rotations around Y by hand
        private static Quaternion YRotation(float degrees)
        {
            float half = degrees * Mathf.Deg2Rad / 2f;
            return new Quaternion(0f, (float)System.Math.Sin(half), 0f, (float)System.Math.Cos(half));
        }

        private static void AssertSameRotation(Quaternion expected, Quaternion actual)
        {
            foreach (var axis in new[] { Vector3.forward, Vector3.up, Vector3.right })
            {
                Assert.IsTrue((expected * axis - actual * axis).magnitude < 1e-4f,
                    $"{expected} != {actual} on {axis}");
            }
        }

        private static void AssertSamePosition(Vector3 expected, Vector3 actual)
        {
            Assert.IsTrue((expected - actual).magnitude < 1e-4f, $"{expected} != {actual}");
        }

        [TestMethod]
        public void MirrorPieceEntry_NegatesX()
        {
            var entry = new PieceEntry("wood_wall", "Building", new Vector3(1f, 2f, 3f), Quaternion.identity, "", Vector3.one);
            AssertSamePosition(new Vector3(-1f, 2f, 3f), entry.Mirrored(0).GetPosition());
        }

        [TestMethod]
        public void MirrorPieceEntry_TurnsYRotationAround()
        {
            var entry = new PieceEntry("wood_wall", "Building", Vector3.zero, YRotation(90f), "", Vector3.one);
            AssertSameRotation(YRotation(-90f), entry.Mirrored(0).GetRotation());
        }

        [TestMethod]
        public void MirrorRotation_EqualsReflectionOfTheAxes()
        {
            // R' = M R M, so R' v = M (R (M v)) for any direction v
            var rotation = (YRotation(30f) * new Quaternion(0.2f, 0f, 0.1f, 0.97f)).normalized;
            var mirrored = BlueprintMirror.MirrorRotation(rotation);
            foreach (var v in new[] { Vector3.forward, Vector3.up, Vector3.right, new Vector3(1f, 2f, 3f) })
            {
                Vector3 mv = new Vector3(-v.x, v.y, v.z);
                Vector3 rmv = rotation * mv;
                AssertSamePosition(new Vector3(-rmv.x, rmv.y, rmv.z), mirrored * v);
            }
        }

        [TestMethod]
        public void MirrorPieceEntry_TwiceIsOriginal()
        {
            var rotation = (YRotation(30f) * new Quaternion(0.2f, 0f, 0.1f, 0.97f)).normalized;
            var entry = new PieceEntry("wood_stair", "Building", new Vector3(1.5f, -2f, 3.25f), rotation, "", Vector3.one);
            var twice = entry.Mirrored(0).Mirrored(0);
            AssertSamePosition(entry.GetPosition(), twice.GetPosition());
            AssertSameRotation(entry.GetRotation(), twice.GetRotation());
        }

        [TestMethod]
        public void MirrorPieceEntry_LineMatchesFields()
        {
            var entry = new PieceEntry("sign", "Furniture", new Vector3(1f, 2f, 3f), YRotation(45f), "some text", new Vector3(1f, 2f, 1f));
            var mirrored = entry.Mirrored(0);
            var parsed = PieceEntry.FromBlueprint(mirrored.line);
            Assert.AreEqual("sign", parsed.name);
            Assert.AreEqual("some text", parsed.additionalInfo);
            AssertSamePosition(mirrored.GetPosition(), parsed.GetPosition());
            AssertSameRotation(mirrored.GetRotation(), parsed.GetRotation());
            AssertSamePosition(new Vector3(1f, 2f, 1f), parsed.GetScale());
        }

        [TestMethod]
        public void MirrorSnapPointEntry_NegatesX()
        {
            var mirrored = new SnapPointEntry(new Vector3(1f, 2f, 3f)).Mirrored();
            AssertSamePosition(new Vector3(-1f, 2f, 3f), mirrored.GetPosition());
            AssertSamePosition(mirrored.GetPosition(), new SnapPointEntry(mirrored.line).GetPosition());
        }

        [TestMethod]
        public void MirrorTerrainModEntry_NegatesXAndRotation()
        {
            var entry = new TerrainModEntry("square", new Vector3(1f, 2f, 3f), 3f, 30, 0.3f, "Paved");
            var mirrored = entry.Mirrored();
            AssertSamePosition(new Vector3(-1f, 2f, 3f), mirrored.GetPosition());
            Assert.AreEqual(-30, mirrored.rotation);
            var parsed = new TerrainModEntry(mirrored.line);
            Assert.AreEqual(-30, parsed.rotation);
            Assert.AreEqual("Paved", parsed.paint);
            Assert.AreEqual(3f, parsed.radius);
        }

        // Snap points of vanilla prefabs
        private static readonly Vector3[] WoodwallSnaps =
            { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f) };
        private static readonly Vector3[] WoodBeam26Snaps =
            { new Vector3(-1f, 0f, 0f), new Vector3(1f, 1f, 0f) };
        private static readonly Vector3[] WoodRoofIcornerSnaps =
            { new Vector3(1f, 1f, -1f), new Vector3(1f, 0f, 1f), new Vector3(-1f, 1f, -1f), new Vector3(-1f, 1f, 1f) };

        [TestMethod]
        public void FindCorrection_SymmetricPieceIsNotTurned()
        {
            Assert.AreEqual(0, BlueprintMirror.FindCorrection(WoodwallSnaps));
            Assert.AreEqual(0, BlueprintMirror.FindCorrection(new Vector3[0]));
        }

        [TestMethod]
        public void FindCorrection_AngledBeamIsTurnedAround()
        {
            Assert.AreEqual(180, BlueprintMirror.FindCorrection(WoodBeam26Snaps));
        }

        [TestMethod]
        public void FindCorrection_RoofCornerIsTurnedAQuarter()
        {
            Assert.AreEqual(270, BlueprintMirror.FindCorrection(WoodRoofIcornerSnaps));
        }

        [TestMethod]
        public void FindCorrection_ChiralPieceIsNotTurned()
        {
            // A spiral has no turn that matches its mirror image
            var spiral = new[] { new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 1f), new Vector3(-1f, 2f, 0f) };
            Assert.AreEqual(0, BlueprintMirror.FindCorrection(spiral));
        }

        [TestMethod]
        public void MirrorWithCorrection_SnapPointsMatchTheMirrorImage()
        {
            var rotation = (YRotation(30f) * new Quaternion(0.2f, 0f, 0.1f, 0.97f)).normalized;
            foreach (var snaps in new[] { WoodBeam26Snaps, WoodRoofIcornerSnaps })
            {
                int correction = BlueprintMirror.FindCorrection(snaps);
                var mirrored = BlueprintMirror.MirrorRotation(rotation, correction);
                foreach (var snap in snaps)
                {
                    // Every snap point of the mirror image must be one of the placed piece's
                    Vector3 image = BlueprintMirror.MirrorPosition(rotation * snap);
                    Assert.IsTrue(System.Array.Exists(snaps, s => (mirrored * s - image).magnitude < 1e-4f),
                        $"{image} missing for correction {correction}");
                }
            }
        }

        [TestMethod]
        public void MirrorPieceEntry_WithCorrectionTwiceIsOriginal()
        {
            var rotation = (YRotation(30f) * new Quaternion(0.2f, 0f, 0.1f, 0.97f)).normalized;
            var entry = new PieceEntry("wood_roof_icorner", "Building", new Vector3(1.5f, -2f, 3.25f), rotation, "", new Vector3(1f, 2f, 3f));
            foreach (int correction in new[] { 90, 180, 270 })
            {
                var twice = entry.Mirrored(correction).Mirrored(correction);
                AssertSamePosition(entry.GetPosition(), twice.GetPosition());
                AssertSameRotation(entry.GetRotation(), twice.GetRotation());
                AssertSamePosition(entry.GetScale(), twice.GetScale());
            }
        }

        [TestMethod]
        public void MirrorScale_QuarterTurnSwapsXAndZ()
        {
            AssertSamePosition(new Vector3(3f, 2f, 1f), BlueprintMirror.MirrorScale(new Vector3(1f, 2f, 3f), 90));
            AssertSamePosition(new Vector3(1f, 2f, 3f), BlueprintMirror.MirrorScale(new Vector3(1f, 2f, 3f), 180));
        }

        [TestMethod]
        public void MirroredName_AddsAndRemovesSuffix()
        {
            Assert.AreEqual("House (mirrored)", BlueprintMirror.MirroredName("House"));
            Assert.AreEqual("House", BlueprintMirror.MirroredName("House (mirrored)"));
        }

        [TestMethod]
        public void MirroredID_AddsAndRemovesSuffix()
        {
            // Matches the IDs CreateIDString gave copies from their mirrored name
            Assert.AreEqual("Jules_House_(mirrored)", BlueprintMirror.MirroredID("Jules_House"));
            Assert.AreEqual("Jules_House", BlueprintMirror.MirroredID("Jules_House_(mirrored)"));
            Assert.IsTrue(BlueprintMirror.IsMirroredID("Jules_House_(mirrored)"));
            Assert.IsFalse(BlueprintMirror.IsMirroredID("Jules_House"));
            Assert.AreEqual("__005_(mirrored)", BlueprintMirror.MirroredID("__005"));
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

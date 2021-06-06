// PaintBrush
// a Valheim mod skeleton using Jötunn
// 
// File:    PaintBrush.cs
// Project: PaintBrush

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using UnityEngine;

namespace PaintBrush
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PaintBrush : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.jotunnmodstub";
        public const string PluginName = "PaintBrush";
        public const string PluginVersion = "0.0.1";

        private void Awake()
        {
            // Do all your init stuff here
            // Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            Config.Bind<int>("Main Section", "Example configuration integer", 1, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ModStub has landed");

            On.Player.PieceRayTest += Player_PieceRayTest;
        }

        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            int layerMask = self.m_placeRayMask;
            if (water)
            {
                layerMask = self.m_placeWaterRayMask;
            }
            if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, layerMask) && (bool)hitInfo.collider && !hitInfo.collider.attachedRigidbody && Vector3.Distance(self.m_eye.position, hitInfo.point) < self.m_maxPlaceDistance)
            {
                Vector2 textureCoord = hitInfo.textureCoord;

                point = hitInfo.point;
                normal = hitInfo.normal;
                piece = hitInfo.collider.GetComponentInParent<Piece>();
                heightmap = hitInfo.collider.GetComponent<Heightmap>();
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    waterSurface = hitInfo.collider;
                }
                else
                {
                    waterSurface = null;
                }
                return true;
            }
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            if(piece)
            {
                MeshFilter meshFilter = piece.GetComponentInChildren<MeshFilter>();
                if (meshFilter)
                {
                    System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
                    meshFilter.mesh.GetVertices(vertices);

                    for (int i = 0; i < vertices.Count; i++)
                    {

                    }
                }
            }
            return result;
        }




#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            { // Set a breakpoint here to break on F9 key press
                Jotunn.Logger.LogInfo("Right here");
            }
        }
#endif
    }
}
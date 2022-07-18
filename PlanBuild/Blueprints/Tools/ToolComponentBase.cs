using PlanBuild.ModCompat;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class ToolComponentBase : MonoBehaviour
    {
        public static ShapedProjector SelectionProjector;
        public static float SelectionRadius = 10.0f;
        public static int SelectionRotation;
        public static float CameraOffset;
        public static Vector3 PlacementOffset = Vector3.zero;
        public static Vector3 MarkerOffset = Vector3.zero;

        internal bool SuppressPieceHighlight = true;
        internal bool ResetPlacementOffset = true;
        internal bool ResetMarkerOffset = true;

        private void Start()
        {
            OnStart();
            
            if (ResetPlacementOffset)
            {
                PlacementOffset = Vector3.zero;
            }

            if (ResetMarkerOffset)
            {
                MarkerOffset = Vector3.zero;
            }
            
            On.Player.UpdatePlacement += Player_UpdatePlacement;
            On.Player.UpdateWearNTearHover += Player_UpdateWearNTearHover;
            On.Player.PlacePiece += Player_PlacePiece;

            On.Player.UpdatePlacementGhost += Player_UpdatePlacementGhost;
            On.Player.PieceRayTest += Player_PieceRayTest;

            On.GameCamera.UpdateCamera += GameCamera_UpdateCamera;
            On.Hud.SetupPieceInfo += Hud_SetupPieceInfo;

            Jotunn.Logger.LogDebug($"{gameObject.name} started");
        }

        public virtual void OnStart()
        {
        }

        private void OnDestroy()
        {
            if (!ZNetScene.instance)
            {
                Jotunn.Logger.LogDebug("Skipping destroy because the game is exiting");
                return;
            }

            OnOnDestroy();
            DisableSelectionProjector();

            On.Player.UpdatePlacement -= Player_UpdatePlacement;
            On.Player.UpdateWearNTearHover -= Player_UpdateWearNTearHover;
            On.Player.PlacePiece -= Player_PlacePiece;

            On.Player.UpdatePlacementGhost -= Player_UpdatePlacementGhost;
            On.Player.PieceRayTest -= Player_PieceRayTest;

            On.GameCamera.UpdateCamera -= GameCamera_UpdateCamera;
            On.Hud.SetupPieceInfo -= Hud_SetupPieceInfo;

            Jotunn.Logger.LogDebug($"{gameObject.name} destroyed");
        }

        public virtual void OnOnDestroy()
        {
        }

        /// <summary>
        ///     Update the tool's placement
        /// </summary>
        private void Player_UpdatePlacement(On.Player.orig_UpdatePlacement orig, Player self, bool takeInput, float dt)
        {
            orig(self, takeInput, dt);

            if (self.m_placementGhost && takeInput)
            {
                OnUpdatePlacement(self);
            }
        }

        /// <summary>
        ///     Default UpdatePlacement when subclass does not override.
        /// </summary>
        public virtual void OnUpdatePlacement(Player self)
        {
            PlacementOffset = Vector3.zero;
            MarkerOffset = Vector3.zero;
            CameraOffset = 0f;
            DisableSelectionProjector();
        }
        
        /// <summary>
        ///     Dont highlight pieces while capturing when enabled
        /// </summary>
        private void Player_UpdateWearNTearHover(On.Player.orig_UpdateWearNTearHover orig, Player self)
        {
            if (!SuppressPieceHighlight)
            {
                orig(self);
            }
        }

        public float GetPlacementOffset(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (Config.InvertPlacementOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                return -Config.PlacementOffsetIncrementConfig.Value;
            }
            else
            {
                return Config.PlacementOffsetIncrementConfig.Value;
            }
        }

        public void UndoRotation(Player player, float scrollWheel)
        {
            if (scrollWheel < 0f)
            {
                player.m_placeRotation++;
            }
            else
            {
                player.m_placeRotation--;
            }
        }

        public void UpdateSelectionRadius(float scrollWheel)
        {
            if (SelectionProjector == null)
            {
                return;
            }

            bool scrollingDown = scrollWheel < 0f;
            if (Config.InvertSelectionScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                SelectionRadius -= Config.SelectionIncrementConfig.Value;
            }
            else
            {
                SelectionRadius += Config.SelectionIncrementConfig.Value;
            }

            SelectionRadius = Mathf.Clamp(SelectionRadius, 2f, 100f);
            SelectionProjector.SetRadius(SelectionRadius);
        }
        
        public void UpdateSelectionRotation(float scrollWheel)
        {
            if (SelectionProjector == null)
            {
                return;
            }

            bool scrollingDown = scrollWheel < 0f;
            if (Config.InvertRotationScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                SelectionRotation -= Config.RotationIncrementConfig.Value;
            }
            else
            {
                SelectionRotation += Config.RotationIncrementConfig.Value;
            }

            SelectionProjector.SetRotation(SelectionRotation);
        }

        public void EnableSelectionProjector(Player self, bool enableMask = false)
        {
            if (SelectionProjector == null)
            {
                SelectionProjector = self.m_placementMarkerInstance.AddComponent<ShapedProjector>();
                SelectionProjector.SetRadius(SelectionRadius);
                SelectionProjector.SetRotation(SelectionRotation);
            }
            if (enableMask)
            {
                SelectionProjector.EnableMask();
            }
            else
            {
                SelectionProjector.DisableMask();
            }
        }

        public void DisableSelectionProjector()
        {
            if (SelectionProjector != null)
            {
                DestroyImmediate(SelectionProjector);
            }
        }

        public void UpdateCameraOffset(float scrollWheel)
        {
            // TODO: base min/max off of selected piece dimensions
            float minOffset = 0f;
            float maxOffset = 20f;
            bool scrollingDown = scrollWheel < 0f;
            if (Config.InvertCameraOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                CameraOffset = Mathf.Clamp(CameraOffset + Config.CameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
            else
            {
                CameraOffset = Mathf.Clamp(CameraOffset - Config.CameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
        }

        /// <summary>
        ///     Flatten placement marker and apply the PlacementOffset
        /// </summary>
        private void Player_UpdatePlacementGhost(On.Player.orig_UpdatePlacementGhost orig, Player self, bool flashGuardStone)
        {
            orig(self, flashGuardStone);

            if (self.m_placementMarkerInstance && self.m_placementGhost)
            {
                self.m_placementMarkerInstance.transform.up = Vector3.back;

                if (PlacementOffset != Vector3.zero)
                {
                    var pos = self.m_placementGhost.transform.position;
                    var rot = self.m_placementGhost.transform.rotation;
                    pos += rot * Vector3.right * PlacementOffset.x;
                    pos += rot * Vector3.up * PlacementOffset.y;
                    pos += rot * Vector3.forward * PlacementOffset.z;
                    self.m_placementGhost.transform.position = pos;
                }
            }
        }
        
        /// <summary>
        ///     Apply the MarkerOffset and react on piece hover
        /// </summary>
        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            if (result && self.m_placementGhost && MarkerOffset != Vector3.zero)
            {
                point += self.m_placementGhost.transform.TransformDirection(MarkerOffset);
            }
            OnPieceHovered(piece);
            return result;
        }
        
        public virtual void OnPieceHovered(Piece hoveredPiece)
        {
        }

        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        private bool Player_PlacePiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
        {
            OnPlacePiece(self, piece);
            return false;
        }

        public virtual void OnPlacePiece(Player self, Piece piece)
        {
        }
        
        /// <summary>
        ///     Adjust camera height
        /// </summary>
        private void GameCamera_UpdateCamera(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            orig(self, dt);

            if (PatcherBuildCamera.UpdateCamera
                && Player.m_localPlayer
                && Player.m_localPlayer.InPlaceMode()
                && Player.m_localPlayer.m_placementGhost)
            {
                self.transform.position += new Vector3(0, CameraOffset, 0);
            }
        }

        /// <summary>
        ///     Hook SetupPieceInfo to alter the piece description per tool.
        /// </summary>
        private void Hud_SetupPieceInfo(On.Hud.orig_SetupPieceInfo orig, Hud self, Piece piece)
        {
            orig(self, piece);
            if (!self.m_pieceSelectionWindow.activeSelf)
            {
                UpdateDescription();
            }
        }

        public virtual void UpdateDescription()
        {
            
        }
    }
}
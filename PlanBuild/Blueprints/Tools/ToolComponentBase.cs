using PlanBuild.ModCompat;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class ToolComponentBase : MonoBehaviour
    {
        public static ShapedProjector SelectionProjector;
        public static float SelectionRadius = 10.0f;
        public static float CameraOffset = 5.0f;
        public static Vector3 PlacementOffset = Vector3.zero;

        internal bool SuppressPieceHighlight = true;

        private void Awake()
        {
            if (!Player.m_localPlayer || !gameObject.name.StartsWith(Player.m_localPlayer.GetSelectedPiece().gameObject.name))
            {
                return;
            }

            Init();

            On.Player.UpdateWearNTearHover += Player_UpdateWearNTearHover;
            On.Player.PieceRayTest += Player_PieceRayTest;
            On.Player.UpdatePlacement += Player_UpdatePlacement;
            On.Player.UpdatePlacementGhost += Player_UpdatePlacementGhost;
            On.Player.PlacePiece += Player_PlacePiece;
            On.GameCamera.UpdateCamera += GameCamera_UpdateCamera;

            Jotunn.Logger.LogDebug($"{gameObject.name} awoken");
        }

        public virtual void Init()
        {
        }

        private void OnDestroy()
        {
            Remove();
            DisableSelectionProjector();

            On.Player.UpdateWearNTearHover -= Player_UpdateWearNTearHover;
            On.Player.PieceRayTest -= Player_PieceRayTest;
            On.Player.UpdatePlacement -= Player_UpdatePlacement;
            On.Player.UpdatePlacementGhost -= Player_UpdatePlacementGhost;
            On.Player.PlacePiece -= Player_PlacePiece;
            On.GameCamera.UpdateCamera -= GameCamera_UpdateCamera;

            Jotunn.Logger.LogDebug($"{gameObject.name} destroyed");
        }

        public virtual void Remove()
        {
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

        /// <summary>
        ///     Apply the PlacementOffset to the placementMarker
        /// </summary>
        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            if (result && PlacementOffset != Vector3.zero && self.m_placementGhost)
            {
                point += self.m_placementGhost.transform.TransformDirection(PlacementOffset);
            }
            return result;
        }

        /// <summary>
        ///     Update the tool's placement
        /// </summary>
        private void Player_UpdatePlacement(On.Player.orig_UpdatePlacement orig, Player self, bool takeInput, float dt)
        {
            orig(self, takeInput, dt);

            if (self.m_placementGhost && takeInput)
            {
                UpdatePlacement(self);
            }
        }

        /// <summary>
        ///     Default UpdatePlacement when subclass does not override.
        /// </summary>
        /// <param name="self"></param>
        public virtual void UpdatePlacement(Player self)
        {
            DisableSelectionProjector();

            CameraOffset = 5f;
            PlacementOffset = Vector3.zero;
        }

        public float GetPlacementOffset(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (BlueprintConfig.InvertPlacementOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                return -BlueprintConfig.PlacementOffsetIncrementConfig.Value;
            }
            else
            {
                return BlueprintConfig.PlacementOffsetIncrementConfig.Value;
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
            if (BlueprintConfig.InvertSelectionScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                SelectionRadius -= BlueprintConfig.SelectionIncrementConfig.Value;
            }
            else
            {
                SelectionRadius += BlueprintConfig.SelectionIncrementConfig.Value;
            }

            SelectionRadius = Mathf.Clamp(SelectionRadius, 2f, 100f);
            SelectionProjector.SetRadius(SelectionRadius);
        }

        public void EnableSelectionProjector(Player self, bool enableMask = false)
        {
            if (SelectionProjector == null)
            {
                SelectionProjector = self.m_placementMarkerInstance.AddComponent<ShapedProjector>();
                SelectionProjector.SetRadius(SelectionRadius);
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
                Destroy(SelectionProjector);
            }
        }

        public void UpdateCameraOffset(float scrollWheel)
        {
            // TODO: base min/max off of selected piece dimensions
            float minOffset = 2f;
            float maxOffset = 20f;
            bool scrollingDown = scrollWheel < 0f;
            if (BlueprintConfig.InvertCameraOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                CameraOffset = Mathf.Clamp(CameraOffset += BlueprintConfig.CameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
            else
            {
                CameraOffset = Mathf.Clamp(CameraOffset -= BlueprintConfig.CameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
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
        ///     Flatten placement marker
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="flashGuardStone"></param>
        private void Player_UpdatePlacementGhost(On.Player.orig_UpdatePlacementGhost orig, Player self, bool flashGuardStone)
        {
            orig(self, flashGuardStone);

            if (self.m_placementMarkerInstance && self.m_placementGhost)
            {
                self.m_placementMarkerInstance.transform.up = Vector3.back;
            }
        }
        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        private bool Player_PlacePiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
        {
            return PlacePiece(self, piece);
        }

        public virtual bool PlacePiece(Player self, Piece piece)
        {
            return false;
        }
    }
}

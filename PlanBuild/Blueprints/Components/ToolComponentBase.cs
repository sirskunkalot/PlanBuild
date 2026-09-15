using HarmonyLib;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class ToolComponentBase : MonoBehaviour
    {
        public static ShapedProjector SelectionProjector;
        public static float SelectionRadius = 10.0f;
        public static int SelectionRotation;
        public static float CameraOffset;
        public static Vector3 PlacementOffset = Vector3.zero;
        public static Vector3 MarkerOffset = Vector3.zero;

        internal bool SuppressGizmo = true;
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

            Jotunn.Logger.LogDebug($"{gameObject.name} destroyed");
        }

        public virtual void OnOnDestroy()
        {
        }

        /// <summary>
        ///     Look up the ToolComponentBase on the local player's currently active placement ghost, if any.
        /// </summary>
        internal static bool TryGetActive(out ToolComponentBase tool)
        {
            tool = null;
            return Player.m_localPlayer && Player.m_localPlayer.m_placementGhost
                && Player.m_localPlayer.m_placementGhost.TryGetComponent(out tool);
        }

        /// <summary>
        ///     Update the tool's placement
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
        [HarmonyPostfix]
        private static void Player_UpdatePlacement_Postfix(Player __instance, bool takeInput)
        {
            if (__instance.m_placementGhost && takeInput
                && __instance.m_placementGhost.TryGetComponent(out ToolComponentBase tool))
            {
                tool.OnUpdatePlacement(__instance);
            }
        }

        /// <summary>
        ///     Default UpdatePlacement when subclass does not override.
        /// </summary>
        public virtual void OnUpdatePlacement(Player self)
        {
        }

        /// <summary>
        ///     Dont highlight pieces while capturing when enabled
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateWearNTearHover))]
        [HarmonyPrefix]
        private static bool Player_UpdateWearNTearHover_Prefix(Player __instance)
        {
            return !(__instance.m_placementGhost
                && __instance.m_placementGhost.TryGetComponent(out ToolComponentBase tool)
                && tool.SuppressPieceHighlight);
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
                SelectionProjector.Enable();
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
                SelectionProjector.Disable();
                DestroyImmediate(SelectionProjector);
            }
        }

        public void UpdateCameraOffset(float scrollWheel)
        {
            // TODO: base min/max off of selected piece dimensions
            float minOffset = 0f;
            float maxOffset = 30f;
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
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        [HarmonyPostfix]
        private static void Player_UpdatePlacementGhost_Postfix(Player __instance)
        {
            if (!__instance.m_placementGhost || !__instance.m_placementGhost.TryGetComponent(out ToolComponentBase _))
            {
                return;
            }

            if (!__instance.m_placementMarkerInstance) return;
            __instance.m_placementMarkerInstance.transform.up = Vector3.back;
            if (!(PlacementOffset != Vector3.zero)) return;

            var rot = __instance.m_placementGhost.transform.rotation;
            __instance.m_placementGhost.transform.Rotate(Quaternion.Inverse(rot).eulerAngles);
            __instance.m_placementGhost.transform.Translate(PlacementOffset);
            __instance.m_placementGhost.transform.Rotate(rot.eulerAngles);
        }

        /// <summary>
        ///     Apply the MarkerOffset and react on piece hover
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
        [HarmonyPostfix]
        private static void Player_PieceRayTest_Postfix(Player __instance, ref Vector3 point, Piece piece, bool __result)
        {
            if (!__instance.m_placementGhost || !__instance.m_placementGhost.TryGetComponent(out ToolComponentBase tool))
            {
                return;
            }

            if (__result && MarkerOffset != Vector3.zero)
            {
                point += __instance.m_placementGhost.transform.TransformDirection(MarkerOffset);
            }
            tool.OnPieceHovered(piece);
        }

        public virtual void OnPieceHovered(Piece hoveredPiece)
        {
        }

        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        [HarmonyPrefix]
        private static bool Player_TryPlacePiece_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (__instance.m_placementGhost && __instance.m_placementGhost.TryGetComponent(out ToolComponentBase tool))
            {
                tool.OnPlacePiece(__instance, piece);
                __result = false;
                return false;
            }
            return true;
        }

        public virtual void OnPlacePiece(Player self, Piece piece)
        {
        }

        /// <summary>
        ///     Adjust camera height
        /// </summary>
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
        [HarmonyPostfix]
        private static void GameCamera_UpdateCamera_Postfix(GameCamera __instance)
        {
            if (!TryGetActive(out _))
            {
                return;
            }

            __instance.transform.position += new Vector3(0, CameraOffset, 0);
        }

        /// <summary>
        ///     Hook SetupPieceInfo to alter the piece description per tool.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
        [HarmonyPostfix]
        private static void Hud_SetupPieceInfo_Postfix(Hud __instance)
        {
            if (!__instance.m_pieceSelectionWindow.activeSelf && TryGetActive(out var tool))
            {
                tool.UpdateDescription();
            }
        }

        public virtual void UpdateDescription()
        {

        }
    }
}
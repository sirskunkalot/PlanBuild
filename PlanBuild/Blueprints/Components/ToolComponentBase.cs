using System.Collections.Generic;
using HarmonyLib;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Blueprints.Components
{
    internal class ToolComponentBase : MonoBehaviour
    {
        public static ShapedProjector SelectionProjector;
        public static float SelectionRadius = 10.0f;
        // Half the square's depth, SelectionRadius is half its width
        public static float SelectionDepthRadius = 10.0f;
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
            // vanilla UpdatePlacement returns early while the build menu is open, but the postfix still runs
            if (__instance.m_placementGhost && takeInput && !Hud.IsPieceSelectionVisible()
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

        private float GetSelectionIncrement(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (Config.InvertSelectionScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                return -Config.SelectionIncrementConfig.Value;
            }
            else
            {
                return Config.SelectionIncrementConfig.Value;
            }
        }

        /// <summary>
        ///     Change the width and the depth of the selection
        /// </summary>
        public void UpdateSelectionRadius(float scrollWheel)
        {
            if (SelectionProjector == null)
            {
                return;
            }

            float increment = GetSelectionIncrement(scrollWheel);
            SelectionRadius = Mathf.Clamp(SelectionRadius + increment, 2f, 100f);
            SelectionDepthRadius = Mathf.Clamp(SelectionDepthRadius + increment, 2f, 100f);
            SelectionProjector.SetRadius(SelectionRadius, SelectionDepthRadius);
        }

        /// <summary>
        ///     Change only the width of a square selection
        /// </summary>
        public void UpdateSelectionWidth(float scrollWheel)
        {
            if (SelectionProjector == null || SelectionProjector.GetShape() != ShapedProjector.ProjectorShape.Square)
            {
                return;
            }

            SelectionRadius = Mathf.Clamp(SelectionRadius + GetSelectionIncrement(scrollWheel), 2f, 100f);
            SelectionProjector.SetRadius(SelectionRadius, SelectionDepthRadius);
        }

        /// <summary>
        ///     Change only the depth of a square selection
        /// </summary>
        public void UpdateSelectionDepth(float scrollWheel)
        {
            if (SelectionProjector == null || SelectionProjector.GetShape() != ShapedProjector.ProjectorShape.Square)
            {
                return;
            }

            SelectionDepthRadius = Mathf.Clamp(SelectionDepthRadius + GetSelectionIncrement(scrollWheel), 2f, 100f);
            SelectionProjector.SetRadius(SelectionRadius, SelectionDepthRadius);
        }

        /// <summary>
        ///     Get the terrain indices under the selection projector
        /// </summary>
        public Dictionary<TerrainComp, Indices> GetSelectionIndices()
        {
            var pos = SelectionProjector.GetPosition();
            var rad = SelectionProjector.GetRadius();

            if (SelectionProjector.GetShape() == ShapedProjector.ProjectorShape.Square)
            {
                return TerrainTools.GetCompilerIndicesWithRect(pos, rad * 2, SelectionProjector.GetDepthRadius() * 2,
                    SelectionProjector.GetRotation() * Mathf.PI / 180f, BlockCheck.Off);
            }

            return TerrainTools.GetCompilerIndicesWithCircle(pos, rad * 2, BlockCheck.Off);
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
                SelectionProjector.SetRadius(SelectionRadius, SelectionDepthRadius);
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
            // An open build menu shows the hovered piece. Vanilla hides the old m_pieceSelectionWindow,
            // mods can bring it back.
            if (!Hud.IsPieceSelectionVisible() && !__instance.m_pieceSelectionWindow.activeSelf
                && TryGetActive(out var tool))
            {
                tool.UpdateDescription();
            }
        }

        public virtual void UpdateDescription()
        {

        }
    }
}
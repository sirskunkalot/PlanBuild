using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elevator
{
    class Patches
    {
        private static Piece m_lastRayPiece;

		[HarmonyPatch(typeof(Hud), "UpdateShipHud")]
		[HarmonyPrefix]
		public static bool UpdateShipHud(Hud __instance, Player player, float dt)
        {
            Elevator controlledElevator = player.GetControlledShip() as Elevator;
			if(controlledElevator)
            {
				Ship.Speed speedSetting = controlledElevator.GetSpeedSetting();
				float rudder = controlledElevator.GetRudder();
				float rudderValue = controlledElevator.GetRudderValue();
				__instance.m_shipHudRoot.SetActive(value: true);
				__instance.m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
				__instance.m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
				__instance.m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
				__instance.m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
				__instance.m_rudderLeft.SetActive(value: false);
				__instance.m_rudderRight.SetActive(value: false);
				__instance.m_fullSail.SetActive(false);
				__instance.m_halfSail.SetActive(false);
                __instance.m_shipWindIconRoot.gameObject.SetActive(false);
				__instance.m_shipWindIndicatorRoot.gameObject.SetActive(false);
				GameObject rudder2 = __instance.m_rudder; 
				rudder2.SetActive(false);
				if ((rudder > 0f && rudderValue < 1f) || (rudder < 0f && rudderValue > -1f))
				{
					__instance.m_shipRudderIcon.transform.Rotate(new Vector3(0f, 0f, 200f * (0f - rudder) * dt));
				} 
				__instance.m_shipRudderIndicator.gameObject.SetActive(value: false); 
				 
				float shipYawAngle = controlledElevator.GetShipYawAngle();
				__instance.m_shipWindIndicatorRoot.localRotation = Quaternion.Euler(0f, 0f, shipYawAngle);
				float windAngle = controlledElevator.GetWindAngle();
				__instance.m_shipWindIconRoot.localRotation = Quaternion.Euler(0f, 0f, windAngle);
				float windAngleFactor = controlledElevator.GetWindAngleFactor();
				__instance.m_shipWindIcon.color = Color.Lerp(new Color(0.2f, 0.2f, 0.2f, 1f), Color.white, windAngleFactor);
				Camera mainCamera = Utils.GetMainCamera();
				if (!(mainCamera == null))
				{
					__instance.m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPoint(controlledElevator.m_controlGuiPos.position);
				}
				return false;
            }
			return true;
        }

		[HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
		[HarmonyPrefix]
		private static bool OnAnimatorIK(CharacterAnimEvent __instance, int layerIndex)
		{
			Player player = __instance.m_character as Player;
			if ((object)player != null && player.IsAttached() && (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
			{
				ElevatorControlls elevator = player.m_attachPoint.parent.GetComponent<ElevatorControlls>();
				if (elevator)
				{
					elevator.UpdateIK(player .m_animator);
				} 
			}
			return true;
		}


		[HarmonyPatch(typeof(Player), "PlacePiece")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> PlacePiece(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].operand != null && list[i].operand.ToString() == "UnityEngine.GameObject Instantiate[GameObject](UnityEngine.GameObject, UnityEngine.Vector3, UnityEngine.Quaternion)")
				{
					list.InsertRange(i + 2, new CodeInstruction[3]
					{
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldloc_3),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), "PlacedPiece"))
					});
					break;
				}
			}
			return list;
		}

		public static void PlacedPiece(Player player, GameObject gameObject)
		{
			Piece piece = gameObject.GetComponent<Piece>();
			if (!piece)
			{
				return;
			} 
			if (m_lastRayPiece)
			{
				MoveableBaseRoot moveableBaseRoot = m_lastRayPiece.GetComponentInParent<MoveableBaseRoot>();
				if (moveableBaseRoot)
				{
					moveableBaseRoot.AddNewPiece(piece);
				}
			}
		}

		[HarmonyPatch(typeof(Player), "SetShipControl")]
		[HarmonyPrefix]

		public static bool SetShipControl(Player __instance, ref Vector3 moveDir)
        {
            Elevator elevator = __instance.GetControlledShip() as Elevator;
			if(elevator)
            {
				elevator.ApplyMovementControlls(moveDir);
				return false;
            }
			return true;
        }

		[HarmonyPatch(typeof(Player), "PieceRayTest")]
		[HarmonyPrefix]
		public static bool PieceRayTest(Player __instance, ref bool __result, ref Vector3 point, ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface, bool water)
		{
			int placeRayMask = __instance.m_placeRayMask;
			MoveableBaseRoot componentInParent = __instance.GetComponentInParent<MoveableBaseRoot>();
			if ((bool)componentInParent)
			{
				Vector3 vector = componentInParent.transform.InverseTransformPoint(__instance.transform.position);
				Vector3 position = vector + Vector3.up * 2f;
				position = componentInParent.transform.TransformPoint(position);
				Quaternion quaternion = __instance.m_lookYaw * Quaternion.Euler(__instance.m_lookPitch, 0f - componentInParent.transform.rotation.eulerAngles.y, 0f);
				Vector3 direction = componentInParent.transform.rotation * quaternion * Vector3.forward;
				if (Physics.Raycast(position, direction, out var hitInfo, 10f, placeRayMask) && (bool)hitInfo.collider)
				{
					MoveableBaseRoot componentInParent2 = hitInfo.collider.GetComponentInParent<MoveableBaseRoot>();
					if ((bool)componentInParent2)
					{
						point = hitInfo.point;
						normal = hitInfo.normal;
						piece = hitInfo.collider.GetComponentInParent<Piece>();
						heightmap = null;
						waterSurface = null;
						__result = true;
						return false;
					}
				}
			} 

			return true;
		} 

		[HarmonyPatch(typeof(Player), "PieceRayTest")]
		[HarmonyPrefix]
		public static bool PieceRayTestPrefix(Player __instance, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water, ref bool __result)
		{
			int layerMask = __instance.m_placeRayMask;
			if (water)
			{
				layerMask = __instance.m_placeWaterRayMask;
			}
			if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, layerMask)
				&& (bool)hitInfo.collider
				&& (!hitInfo.collider.attachedRigidbody || hitInfo.collider.attachedRigidbody.GetComponent<MoveableBaseRoot>() != null)
				&& Vector3.Distance(__instance.m_eye.position, hitInfo.point) < __instance.m_maxPlaceDistance)
			{
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
                __result = true;
				return false;
			}
			point = Vector3.zero;
			normal = Vector3.zero;
			piece = null;
			heightmap = null;
			waterSurface = null;
			__result = false;
			return false; 
		}  

		[HarmonyPatch(typeof(Player), "PieceRayTest")]
		[HarmonyPostfix]
		public static void PieceRayTestPostfix(Player __instance, ref bool __result, ref Vector3 point, ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface, bool water)
		{
			m_lastRayPiece = piece;
		}

		[HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
		[HarmonyPrefix]
		public static bool UpdateSupport(WearNTear __instance)
		{
			if (!__instance.isActiveAndEnabled)
			{
				return false;
			}
			MoveableBaseRoot componentInParent = __instance.GetComponentInParent<MoveableBaseRoot>();
			if (!componentInParent)
			{
				return true;
			}
			if (__instance.transform.localPosition.y < 1f)
			{
				__instance.m_nview.GetZDO().Set("support", 1500f);
				return false;
			}
			return false;
		}

		[HarmonyPatch(typeof(Player), "FindHoverObject")]
		[HarmonyPrefix]
		public static bool FindHoverObject(Player __instance, ref GameObject hover, ref Character hoverCreature)
		{
			hover = null;
			hoverCreature = null;
			RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
			Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
			RaycastHit[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit raycastHit = array2[i];
				if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject)
				{
					continue;
				}
				if (hoverCreature == null)
				{
					Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
					if (character != null)
					{
						hoverCreature = character;
					}
				}
				if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) < __instance.m_maxInteractDistance)
				{
					if (raycastHit.collider.GetComponent<Hoverable>() != null)
					{
						hover = raycastHit.collider.gameObject;
					}
					else if ((bool)raycastHit.collider.attachedRigidbody && !raycastHit.collider.attachedRigidbody.GetComponent<MoveableBaseRoot>())
					{
						hover = raycastHit.collider.attachedRigidbody.gameObject;
					}
					else
					{
						hover = raycastHit.collider.gameObject;
					}
				}
				break;
			}
			return false;
		}
	}
}

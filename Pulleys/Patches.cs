using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pulleys
{
    class Patches
    {
        private static Piece m_lastRayPiece;
		   /*
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
				if (Physics.Raycast(position, direction, out var hitInfo, 10f, placeRayMask) && hitInfo.collider)
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
				&& hitInfo.collider
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
		   */ 

	}
}

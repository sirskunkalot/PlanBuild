using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elevator
{
    public class ElevatorControlls : ShipControlls
    {
        private Transform m_handAttach;
        private Collider m_wheelCollider;
        internal Elevator m_elevator;

        public new void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
            m_nview.Register<ZDOID>("RequestControl", RPC_RequestControl);
            m_nview.Register<ZDOID>("ReleaseControl", RPC_ReleaseControl);
            m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
            m_elevator = GetComponentInParent<Elevator>();
            m_ship = m_elevator;
            m_handAttach = transform.parent.Find("New/crank/handattach").transform;
            m_wheelCollider = GetComponent<Collider>();
            m_attachPoint = m_ship.transform.Find("attachpoint");
        }

        public new void OnUseStop(Player player)
        {
            player.m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
          //  Physics.IgnoreCollision(m_wheelCollider, Player.m_localPlayer.GetComponent<Collider>(), ignore: false);
            base.OnUseStop(player);
        }
          
        public new void RPC_RequestControl(long sender, ZDOID playerID)
        {
            if (m_nview.IsOwner() && m_ship.IsPlayerInBoat(playerID))
            {
                if (GetUser() == playerID || !HaveValidUser())
                {
                    m_nview.GetZDO().Set("user", playerID);
                    m_nview.InvokeRPC(sender, "RequestRespons", true);
                }
                else
                {
                    m_nview.InvokeRPC(sender, "RequestRespons", false);
                }
            }
        }

        public new void RPC_RequestRespons(long sender, bool granted)
        {
            Jotunn.Logger.LogInfo("Request response: " + granted);
            if(granted)
            {
                Player.m_localPlayer.m_animator.SetIKPosition(AvatarIKGoal.RightHand, m_handAttach.position);
                Player.m_localPlayer.m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
             //   Physics.IgnoreCollision(m_wheelCollider, Player.m_localPlayer.GetComponent<Collider>());
            }
            base.RPC_RequestRespons(sender, granted);
        }



        [HarmonyPatch(typeof(Player), "SetControls")]
        [HarmonyPrefix]
        static bool Player_SetControls_Prefix(Player __instance, ShipControlls ___m_shipControl)
        {
            ShipControlls elevatorControl = ___m_shipControl;
            if (elevatorControl != null)
            {
                
                return true;
            }
            return true;
        }

        internal void UpdateIK(Animator m_animator)
        {
            m_animator.SetIKPosition(AvatarIKGoal.RightHand, m_handAttach.position);
            m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        }
    } 
}

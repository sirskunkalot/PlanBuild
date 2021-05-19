using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pulleys
{
    public class PulleyControlls : ShipControlls
    {
        private Transform m_handAttach; 
        internal Pulley m_pulley;
        internal MoveableBaseRoot m_baseRoot;


        public new void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
            m_nview.Register<ZDOID>("RequestControl", RPC_RequestControl);
            m_nview.Register<ZDOID>("ReleaseControl", RPC_ReleaseControl);
            m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
            m_pulley = GetComponentInParent<Pulley>();
            m_baseRoot = GetComponentInParent<MoveableBaseRoot>();
            m_ship = m_baseRoot;
            m_handAttach = m_pulley.transform.Find("New/crank/handattach").transform; 
            m_attachPoint = m_pulley.transform.Find("attachpoint");
        }

        public new bool Interact(Humanoid user, bool hold)
        {
            if(!m_ship)
            {
                return false;
            }
            return base.Interact(user, hold);
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
                m_baseRoot.SetActiveControll(this);
             //   Physics.IgnoreCollision(m_wheelCollider, Player.m_localPlayer.GetComponent<Collider>());
            }
            base.RPC_RequestRespons(sender, granted);
        }
         

        [HarmonyPatch(typeof(Player), "SetControls")]
        [HarmonyPrefix]
        static bool Player_SetControls_Prefix(Player __instance, ShipControlls ___m_shipControl)
        {
            ShipControlls pulleyControl = ___m_shipControl;
            if (pulleyControl != null)
            { 
                return true;
            }
            return true;
        }

        internal void UpdateIK(Animator m_animator)
        {
#if DEBUG
            //Jotunn.Logger.LogInfo("HandAttach: Position: " + m_handAttach.position);
#endif
            m_animator.SetIKPosition(AvatarIKGoal.RightHand, m_handAttach.position);
            m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        }
    } 
}

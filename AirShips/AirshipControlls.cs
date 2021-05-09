using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AirShips
{
    class AirshipControlls : ShipControlls
    { 
        public new void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
            m_nview.Register<ZDOID>("RequestControl", RPC_RequestControl);
            m_nview.Register<ZDOID>("ReleaseControl", RPC_ReleaseControl);
            m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
            m_ship = GetComponentInParent<Airship>();
            m_attachPoint = m_ship.transform.Find("attachpoint");
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
            base.RPC_RequestRespons(sender, granted);
        }

        [HarmonyPatch(typeof(Player), "SetControls")]
        [HarmonyPrefix]
        static bool Player_SetControls_Prefix(ShipControlls ___m_shipControl)
        {
            AirshipControlls airshipControlls = ___m_shipControl as AirshipControlls;
            if(airshipControlls != null)
            {

                return true;
            }
            return true;
        }
    }
}

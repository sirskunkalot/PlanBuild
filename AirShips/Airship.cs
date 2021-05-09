using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AirShips
{
    class Airship: Ship
    {



        public new void Awake()
        {
            m_sailObject = new GameObject("fake_sail");
            base.Awake();
            m_shipControlls = GetComponentInChildren<AirshipControlls>();
        }

        public new void ApplyMovementControlls(Vector3 direction)
        {
            Jotunn.Logger.LogInfo("Intercept direction: " + direction);    
            base.ApplyMovementControlls(direction);
        }

        public new void UpdateSail(float dt)
        {
            //Nothing to do
        }

        public new void UpdateSailSize(float dt)
        {
            //Nothing to do
        }
    }
}

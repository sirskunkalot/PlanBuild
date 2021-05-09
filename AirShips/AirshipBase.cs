using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimRAFT;

namespace AirShips
{
    class AirshipBase: MoveableBaseRoot
    {
        private GameObject m_surtlingCore;
        private Vector3 defaultSurtlingCorePosition;

        new void Awake()
        {
            base.Awake();
            m_rigidbody.useGravity = false;
            m_surtlingCore = transform.Find("surtlingCore").gameObject;
            defaultSurtlingCorePosition = m_surtlingCore.transform.localPosition;
        }

        void Update()
        {
            // Save the y position prior to start floating (maybe in the Start function):

            // Put the floating movement in the Update function:

            Vector3 surtlingCorePosition = m_surtlingCore.transform.position;
            surtlingCorePosition.y = defaultSurtlingCorePosition.y + 0.2f * Mathf.Sin(1f * Time.time);
        }
    }
}

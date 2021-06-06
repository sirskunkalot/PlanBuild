using Jotunn.Managers;
using PlanBuild.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild
{
    class WorldBlueprintManager : MonoBehaviour, Interactable, Hoverable
    {


        public void Awake()
        {
            
        }

        public void Update()
        {
            if(BlueprintManager.Instance.ActiveRunestone(this))
            {
                if(ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape))
                {
                    BlueprintManager.Instance.SetActiveRunestone(null);
                }
            }           
        }

        public string GetHoverName()
        {
            return "Blueprint Runestone";
        }

        public string GetHoverText()
        {
            return Localization.instance.Localize("[<color=yellow>$KEY_Use</color>] Manage blueprints");
        }

        public bool Interact(Humanoid user, bool hold)
        {
            if(hold)
            {
                return false;
            }
            BlueprintManager.Instance.SetActiveRunestone(this);
            
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}

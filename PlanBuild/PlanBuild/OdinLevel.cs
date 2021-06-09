using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild 
{
    class OdinLevel : MonoBehaviour, Interactable, Hoverable
    {
        public string GetHoverName()
        {
            return "Level";
        }

        public string GetHoverText()
        {
            return Localization.instance.Localize("[<color=yellow>$KEY_Use</color>] Toggle grid");
        }

        public bool Interact(Humanoid user, bool hold)
        {  
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}

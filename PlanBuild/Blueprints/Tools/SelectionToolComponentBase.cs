using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    class SelectionToolComponentBase : ToolComponentBase
    {
        public override void Init()
        {
            StartCoroutine(FlashSelection());
        }

        public IEnumerator<YieldInstruction> FlashSelection()
        {
            while (true)
            {
                // yield return new WaitForSeconds(1);
                yield return null;
                foreach (GameObject selected in new List<GameObject>(BlueprintManager.Instance.activeSelection))
                {
                    if (selected && selected.TryGetComponent(out WearNTear wearNTear))
                    {
                        wearNTear.Highlight(Color.green);
                    }
                    yield return null;
                }
            }
        }

        public override void UpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                EnableSelectionProjector(self);
                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (scrollWheel != 0f)
                { 
                    UpdateSelectionRadius(scrollWheel);
                }
                else
                {
                    UndoRotation(self, scrollWheel);
                }
            } else
            {
                DisableSelectionProjector();
            } 
        }

    }
}

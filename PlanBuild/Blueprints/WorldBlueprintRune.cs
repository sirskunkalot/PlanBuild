using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class WorldBlueprintRune : MonoBehaviour, Interactable, Hoverable
    {
        private Piece m_piece;

        public void Awake()
        {
            m_piece = GetComponent<Piece>();
            
        }

        private Color GetEmissionColor()
        {
            foreach(Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials) {
                    if(material.HasProperty("_EmissionColor"))
                    {
                    return material.GetColor("_EmissionColor");
                    }
                }
            }
            return Color.red;
        }

        public string GetHoverName()
        {
            return m_piece.m_name;
        }

        public string GetHoverText()
        {
            return Localization.instance.Localize(
                $"{GetHoverName()}\n" +
                $"[<color=yellow>$KEY_Use</color>] Open Blueprint Marketplace"
            );
        }

        public bool Interact(Humanoid user, bool hold)
        {
            if (hold)
            {
                return false;
            }
            BlueprintGUI.Instance.Toggle();
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
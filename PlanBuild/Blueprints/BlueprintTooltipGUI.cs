using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    /// <summary>
    ///     Panel showing a bigger thumbnail of a blueprint piece hovered in the build menu.
    ///     The new build UI does not use <see cref="UITooltip"/> for its piece buttons any more,
    ///     so the panel is shown from <see cref="BlueprintManager"/>'s Hud.UpdateBuild patch
    ///     and anchored to the hovered button itself.
    /// </summary>
    internal static class BlueprintTooltipGUI
    {
        private static GameObject Panel;
        private static RectTransform Background;
        private static Image BackgroundImage;
        private static Image ThumbnailImage;
        private static Text NameText;

        /// <summary>
        ///     Show the tooltip for a blueprint above the given anchor, the mouse position when null.
        /// </summary>
        public static void Show(Blueprint blueprint, Sprite thumbnail, Component anchor)
        {
            if (!CreatePanel())
            {
                return;
            }

            BackgroundImage.color = Config.TooltipBackgroundConfig.Value;
            ThumbnailImage.sprite = thumbnail;
            NameText.text = blueprint.Name;

            Panel.SetActive(true);
            Panel.transform.position = AnchorPosition(anchor);
            global::Utils.ClampUIToScreen(Background);
        }

        public static void Hide()
        {
            if (Panel)
            {
                Panel.SetActive(false);
            }
        }

        private static bool CreatePanel()
        {
            if (Panel)
            {
                return true;
            }
            if (!BlueprintAssets.BlueprintTooltip || !GUIManager.CustomGUIFront)
            {
                return false;
            }

            Panel = Object.Instantiate(BlueprintAssets.BlueprintTooltip, GUIManager.CustomGUIFront.transform);
            Background = (RectTransform)Panel.transform.Find("Background");
            BackgroundImage = Background.GetComponent<Image>();
            ThumbnailImage = Background.Find("BPImage").GetComponent<Image>();
            NameText = Background.Find("BPText").GetComponent<Text>();

            // never steal the hover from the piece button the panel is drawn over
            foreach (var graphic in Panel.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            return true;
        }

        /// <summary>
        ///     Top center of the anchor, like vanilla's UITooltip.AnchorTooltip
        /// </summary>
        private static Vector3 AnchorPosition(Component anchor)
        {
            if (!(anchor?.transform is RectTransform rect))
            {
                return ZInput.pointerPosition;
            }

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[1] + corners[2]) / 2f;
        }
    }
}

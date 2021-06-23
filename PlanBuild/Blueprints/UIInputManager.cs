using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    /// <summary>
    /// Just example usage of the Menu -- and simpl-ish input manager to handle the menu open/closing.
    /// Must Declare UIBlueprintManager in mod as static.
    /// public static UIBlueprintMenuManager UIBlueprintManager { get; set; } = new UIBlueprintMenuManager();
    /// Change MainMod to whatever.
    /// </summary>
    public static class UIInputManager
    {
        /*public static bool MapIsOpen { get; set; } = false;
        public static bool MenuWindowIsOpen { get; set; } = false;
        public static void Update()
        {
            if (ZInput.instance != null)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        ModMain.UIBlueprintManager.Toggle(shutWindow: true);
                    }

                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        if (!ModMain.UIBlueprintManager.IsActive && !MenuWindowIsOpen)
                        {
                            MapIsOpen = !MapIsOpen;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        if (ModMain.UIBlueprintManager.Window == null)
                        {
                            var menuPrefab = AssetManager.GrabPrefab("blueprintmenuui", "BlueprintMenu");
                            var detailsContainer = AssetManager.GrabPrefab("blueprintmenuui", "BPDetailsContainer");
                            var bpIcon = AssetManager.GrabPrefab("blueprintmenuui", "BPIcon");
                            ModMain.UIBlueprintManager.Register(menuPrefab.LoadedPrefab, detailsContainer.LoadedPrefab, bpIcon.LoadedPrefab);

                            List<string> clientSide = new List<string>()
                            {
                                @"<b>GreatHall</b>
(3269 pieces)",@"<b>House</b>
(485 pieces)",@"<b>Hutte</b>
(181 pieces)",
                            };

                            List<string> serverSide = new List<string>()
                            {
                                @"<b>MOAR</b>
(420 pieces)",
                            };

                            foreach (var bpDescription in clientSide)
                            {
                                ModMain.UIBlueprintManager.MyTab.ListDisplay.AddBlueprint(Guid.NewGuid().ToString(), bpDescription);
                            }

                            foreach (var bpDescription in serverSide)
                            {
                                ModMain.UIBlueprintManager.ServerTab.ListDisplay.AddBlueprint(Guid.NewGuid().ToString(), bpDescription);
                            }
                        }
                        ModMain.UIBlueprintManager.Toggle();
                    }

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        if (MapIsOpen)
                        {
                            MapIsOpen = !MapIsOpen;
                        }
                        else if (!MapIsOpen && ModMain.UIAscendedMenuManager.IsActive)
                        {
                            MenuWindowIsOpen = !MenuWindowIsOpen;
                        }

                        ModMain.UIBlueprintManager.Toggle(shutWindow: true);
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogMessage($"Input Manager failed -- {ex}");
                }
            }
        }*/
    }
}

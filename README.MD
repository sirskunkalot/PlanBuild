# PlanBuild - Blueprint Edition

PlanBuild enables you to plan, copy and share your building creations in Valheim with ease.

![BlueprintRune](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/Blueprints/Icons/BlueprintRuneIcon.png)

Introducing a new item, the __Blueprint Rune__, which can be crafted from a single stone and festering willpower given to you by the gods. The rune comes with two modes you can switch between by pressing __P__ while having the rune equipped (can be adjusted via config file).

## Planning mode

![Plan mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanMode.png)

Plan the construction of pieces without the need to gather the resources first. Anyone can add the required resources to the planned structure later and finish the construction after it was placed.

The __Blueprint Rune__ is compatible with custom piece tables from other mods. All custom pieces will be incorporated into the runes table for the planned pieces. You still need a __Hammer__ and the required crafting station to finish the construction.

Planned pieces that are __unsupported__ can not be finished. These pieces are also slightly more transparent so you can see what is and isn't supported. The planned pieces themselves do not require support, so you can build forever (if you can reach far enough).

Real pieces also snap to the planned pieces, so you could even use them as __spacers__ or __rulers__.

### PlanTotem

![Plan Totem](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanTotem.png)

Build a __PlanTotem__ near your planned structures to be able to add resources in a centralized location for all individual pieces on the plan.

This needs to be built with the vanilla Hammer tool and costs you __5 Fine Wood__, __5 Grey Dwarf Eye__ and __1 Surtling Core__.

### Skuld Crystal

![Skuld Crystal](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_crystal.png)

Includes the __Skuld Crystal__, a wearable item that removes the shader effect from the blueprints, so you can see what the construction will look like when completed.

Create it by combining a __Ruby__ and a __Grey Dwarf Eye__.

__Watch your step!__ The pieces are still not really there, and will not support you!

## Blueprint mode

![Blueprint mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMode.png)

Copy existing structures into __Blueprints__ and rebuild them as planned or regular pieces all at once. The blueprints are saved in and loaded from the filesystem as __.blueprint__ files. Also supports __.vbuild__ files (you can load and build your BuildShare saves with this mod)! After switching to the blueprint mode, the piece table of the Blueprint Rune offers two different categories:

### Tools

![Blueprint tools](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintTools.png)

The Blueprint Rune comes with a handful of tools to aid handling blueprint creation and building. All terrain tools can be disabled via server side enforced configuration. Here is a handy list:

* __Create new blueprint:__ Create a blueprint of a construction. Planned pieces are captured as real pieces. 
  * Press __Ctrl__ to see what pieces are currently selected. 
  * Use the __Scroll Wheel__ to change the capture radius. 
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Snap point marker:__ Add snap point markers to all points you want to have as snap points in your blueprint. The rotation of the markers does not matter, only the center point. We highly suggest that you also use [Snap points made easy](https://www.nexusmods.com/valheim/mods/299)﻿ so you can cycle through the snap points when placing the blueprint.

* __Center point marker:__ Add a center point marker to your blueprint to determine the center of the blueprint. This is where it will be anchored while placing it. If a blueprint does not have a center point marker, a bottom corner of the blueprint is found and used as the center.

* __Remove planned pieces:__ Delete planned pieces again. Per default only the hovered piece will be deleted. But you can use various modifiers to change that behaviour.
  * Press __Alt__ to delete all plans that are associated with a placed blueprint. Plans that are already finished will not be removed. Resources that were already added to the unfinished plans will be refunded.
  * Press __Ctrl__ to delete plans in a radius, can be used to clean up after using it to measure distances, or as a general cleanup tool. Resources that were already added to the unfinished plans will be refunded.
  * Use the __Scroll Wheel__ while holding __Ctrl__ to change the deletion radius.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Terrain Tools:__ Allows you to "flatten" the terrain in a chosen radius or remove previously made modifications. Uses Valheim's TerrainCompiler and is 100% compatible with the vanilla game and modifications made with the Hoe for example.
  * Press __Q__ to switch between a circle and a square shaped marker.
  * Press __Ctrl__ to remove terrain modifications.
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Ctrl + Alt + Scroll__ to move the marker on the Y-axis.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Delete Objects:__ Allows you to remove vegetation objects in a chosen radius.
  * Press __Ctrl__ to remove all objects including Pieces and Items (__Warning:__ Very destructive).
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Paint terrain:__ Allows you to reset the terrain "paint" per biome (grass in the Meadows, sand at beaches, etc). Can also paint dirt or paved onto every terrain. Can be used as a "brush" by holding down the Attack button continously.
  * Press __Ctrl__ to paint "dirt".
  * Press __Alt__ to paint "paved".
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Shift + Scroll__ to adjust the camera distance.

### Blueprints

![Blueprint tools](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintBlueprints.png)

Place a blueprint as planned pieces. Select your previously saved blueprint and place it anywhere in the world. This works just like any other vanilla building piece. Additionally there are some extra controls to make placing your structures exactly as you want them as easy as possible:

* Use __Scroll__ to rotate the blueprint.
* Use __Ctrl + Scroll__ to move the blueprint on the Z-axis.
* Use __Alt + Scroll__ to move the blueprint on the X-axis.
* Use __Ctrl + Alt + Scroll__ to move the blueprint on the Y-axis.
* Use __Shift + Scroll__ to adjust the camera distance.
* There is a (server enforced) config option to allow placing the blueprints as regular pieces, so you can configure per server if you want to allow "cheating" structures without resources. When enabled, build your structures without building costs by pressing __Ctrl__ while placing the blueprint.

## Blueprint Marketplace

![Blueprint mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMarket.png)

Manage and share your blueprints through a custom GUI added to the game. Per default the marketplace is accessible via __End__ key. Rename your local blueprints and add a description to them. If a server has this feature enabled, upload your local blueprints to that server so others can download and build your creations as well. Players with admin rights on a server can also manage the server side list through that interface.

### Marketplace Pieces

![Blueprint pieces](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintPieces.png)

You can place one of two new rune themed pieces in the world which provide access to your own blueprints and the market on the server. If you want to completely stop clients from accessing the server blueprints via Hotkey, there is a server enforced config which disables that Hotkey for all clients.

## Compatibility

Fully compatible with:
* [Build Camera](https://www.nexusmods.com/valheim/mods/226)﻿
* [Craft from Containers](https://www.nexusmods.com/valheim/mods/40)﻿
* [ValheimRAFT](https://www.nexusmods.com/valheim/mods/1136)

The Hammer's PieceTable is scanned automatically, mods that add Pieces should be compatible. If you find a mod that adds pieces to the Hammer and they don't show up, try toggling the Blueprint Rune with __P__ which will trigger a rescan. If it still doesn't work, please post a bug report with a link to the mod.

## Installing

Use Vortex to install, or if you want to install it manually, drop the "PlanBuild" folder into BepInEx\plugins (so you end up with BepInEx\plugins\PlanBuild). Make sure to include all files, not just the DLL!

This mod adds interactable objects, so __all__ clients & server will need this mod!

Most values are configurable:
* General
    * Show all plans, even for pieces you don't know yet (default __false__)
    * Build radius of the Plan Totem (default __30__)
* Blueprint Market
    * Allow clients to use the Hotkey to access the marketplace (default __true__)
    * Hotkey for the Blueprint Marketplace GUI (default __End__)
    * Allow sharing of blueprints on this server (default __false__)
* Blueprint Rune
    * Hotkey to switch between Blueprint Rune modes (default __P__)
    * Allow building of blueprints as actual pieces without needing the resources (default __false__)
    * Allow usage of the terrain modification tools (default __false__)
    * Place distance of the Blueprint Rune (default __50__)
    * Invert and sensitivity options for each input with the scroll wheel
* Directories
    * Blueprint search directory (default __.__ (current working directory, usually Valheim game install directory))
    * Blueprint save directory (default __BepInEx/config/PlanBuild/blueprints__)
* Visual
    * Apply plan shader to ghost placement (currently placing piece) (default __false__)
    * Color of unsupported plan pieces (default __10% white__)
    * Color of supported plan pieces (default __50% white__)
    * Additional transparency for finer control (default __30%__)

Source available on GitHub: [https://github.com/sirskunkalot/PlanBuild](https://github.com/sirskunkalot/PlanBuild)﻿. All contributions welcome!

You can also find us at the [Valheim Modding Discord](https://discord.gg/RBq2mzeu4z) or the [Jötunn Discord](https://discord.gg/DdUt6g7gyA).

## Building community

Head over to the [Valheimians](https://www.valheimians.com) page to find a community of builders and share your own creations. PlanBuild blueprints are supported.

## Credits

The original PlanBuild mod was created by __[MarcoPogo](https://github.com/MathiasDecrock)__

Blueprint functionality originally created and merged by __[Algorithman](https://github.com/Algorithman)__ & __[Jules](https://github.com/sirskunkalot)__

Blueprint Marketplace GUI created by __[Dreous](https://github.com/imcanida)__

All further coding by __[MarcoPogo](https://github.com/MathiasDecrock)__ & __[Jules](https://github.com/sirskunkalot)__

Made with Löve and __[Jötunn](https://github.com/Valheim-Modding/Jotunn)__
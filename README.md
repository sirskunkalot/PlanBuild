
# BIG REVAMP

Version 0.10.0 of PlanBuild comes with a big internal code refactor and many changes to the handling of plans and blueprints. Please make sure to check out the documentation below. The biggest changes in a nutshell:

- Instead of switching plan and blueprint mode on the rune, you now have two distinct tools, the __Plan Hammer__ and the __Blueprint Rune__.
- All of the blueprinting is done via the __Selection__ tools. You can add and remove parts of your buildings to a selection and need to use the __Edit Selection__ tool or the __selection.gui__ console command to save or copy your blueprint.
- The configuration was redesigned for more clarity. Please revise your configuration once after installing this version.

# PlanBuild

PlanBuild enables you to plan, copy and share your building creations in Valheim with ease. The mod adds two new tools to the game. The __Plan Hammer__ is used to plan your creations before actually gathering all the materials. When you are happy with your build, you can add the required building materials one by one or use a custom totem to automatically build the pieces for you. The __Blueprint Rune__ lets you copy, save or delete your creations as a single building piece which can also be shared with other players using the mod and also includes terrain modification tools for quick and more precise terraforming without using the Hoe or Cultivator.

## Planning

![Plan mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanMode.png)

Introducing a new item, the __Plan Hammer__, which can be crafted from a single log of __wood__ and the blessing of Odin shining upon you.

![PlanHammer](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_hammer.png)

Plan the construction of pieces without the need to gather the resources first. Anyone can add the required resources to the planned structure later and finish the construction after it was placed.

The __Plan Hammer__ is compatible with custom piece tables from other mods. All custom pieces will be incorporated into the runes table for the planned pieces. You still need a __Hammer__ and the required crafting station to finish the construction.

Planned pieces that are __unsupported__ can not be finished. These pieces are also slightly more transparent so you can see what is and isn't supported. The planned pieces themselves do not require support, so you can build forever (if you can reach far enough).

Real pieces also snap to the planned pieces, so you could even use them as __spacers__ or __rulers__.

### Plan Totem

![Plan Totem](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanTotem.png)

Build a __Plan Totem__ near your planned structures to be able to add resources in a centralized location for all individual pieces on the plan.

It will also protect existing structures. Any piece that is destroyed _(not removed by the Hammer)_ will be replaced with a plan for that same piece in the same place!

This needs to be built with the vanilla Hammer tool and costs you __1 Wood__ and __1 Grey Dwarf Eye__.

### Skuld Crystal

![Skuld Crystal](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_crystal.png)

Includes the __Skuld Crystal__, a wearable item that removes the shader effect from the blueprints, so you can see what the construction will look like when completed.

Create it from a single __Grey Dwarf Eye__.

__Watch your step!__ The pieces are still not really there, and will not support you!

## Blueprinting

![Blueprint mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMode.png)

Introducing a new item, the __Blueprint Rune__, which can be crafted from a single __stone__ and festering willpower given to you by the gods.

![BlueprintRune](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/Blueprints/Icons/BlueprintRuneIcon.png)

Copy existing structures into __Blueprints__ and rebuild them as planned or regular pieces all at once. The blueprints are saved in and loaded from the filesystem as __.blueprint__ files. Also supports __.vbuild__ files (you can load and build your BuildShare saves with this mod)! After switching to the blueprint mode, the piece table of the Blueprint Rune offers three different categories:

### Tools

![Blueprint tools](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintTools.png)

The Blueprint Rune comes with a handful of tools to handle blueprint creation and terraforming. 

* __Create new Blueprint__: Directly create a blueprint from a selection indicated by the circle marker.
  * Hold __Ctrl__ to highlight all pieces which will be saved into the blueprint
  * Use the __Scroll Wheel__ to change the selection radius
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Add to selection__: Add pieces to the current selection. Currently selected pieces will be highlighted in green. Per default only the hovered piece will be added to the selection. You can use various modifiers to change that behaviour.
  * Press __Q__ to quickly switch to the "Remove from selection" tool
  * Hold __Alt__ and click to add all connected pieces. This uses a flood fill to find connected pieces.
  * Hold __Ctrl__ and click to add pieces in a radius
  * Use the __Scroll Wheel__ while holding __Ctrl__ to change the selection radius
  * Hold __Shift__ and click once to define a "starting piece" and click again on another piece to select every piece in between those two.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Remove from selection__: Remove pieces from the current selection. Currently selected pieces will be highlighted in green.  Per default only the hovered piece will be removed from the selection. You can use various modifiers to change that behaviour.
  * Press __Q__ to quickly switch to the "Add to selection" tool
  * Hold __Alt__ and click to remove all connected pieces. This uses a flood fill to find connected pieces.
  * Hold __Ctrl__ and click to remove pieces in a radius
  * Use the __Scroll Wheel__ while holding __Ctrl__ to change the selection radius
  * Hold __Shift__  and click to clear the current selection
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Edit selection__: Left click to open a menu. From here you can choose what to do with your current selection:
  * __Copy with custom SnapPoints__: Make a temporary blueprint of your current selection. It will copy all pieces in the selection into a new building piece and automatically select that piece for you to build. The copy is also saved into the "Clipboard" category of the rune for you to access until you log out from the current world.
  * __Copy with vanilla SnapPoints__: Make a temporary blueprint of your current selection which also includes all vanilla snap points of the pieces selected.
  * __Save__: Save the current selection as a new blueprint into the file system. These blueprints are kapt between game sessions and can also be used in the marketplace and shared with other players.
  * __Delete__: Delete all pieces in the current selection. This removes all the pieces without refunding the building materials.
  * __Cancel__: Exit the menu without any action.

* __Snap point marker:__ Add snap point markers to all points you want to have as snap points in your blueprint. The rotation of the markers does not matter, only the center point. We highly suggest that you also use [Snap points made easy](https://www.nexusmods.com/valheim/mods/299)﻿ so you can cycle through the snap points when placing the blueprint.

* __Center point marker:__ Add a center point marker to your blueprint to determine the center of the blueprint. This is where it will be anchored while placing it. If a blueprint does not have a center point marker, a bottom corner of the blueprint is found and used as the center.

* __Remove planned pieces:__ Delete planned pieces again. Per default only the hovered piece will be deleted. But you can use various modifiers to change that behaviour.
  * Press __Ctrl__ to delete plans in a radius, can be used to clean up after using it to measure distances, or as a general cleanup tool. Resources that were already added to the unfinished plans will be refunded.
  * Use the __Scroll Wheel__ while holding __Ctrl__ to change the deletion radius.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Terrain Tools:__ Allows you to "flatten" the terrain in a chosen radius or remove previously made modifications. Uses Valheim's TerrainCompiler and is 100% compatible with the vanilla game and modifications made with the Hoe for example.
  * Press __Q__ to switch between a circle and a square shaped marker.
  * Press __Ctrl__ to add smooth edges to the flattened area
  * Press __Alt__ to remove terrain modifications.
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Ctrl + Scroll__ to rotate the square marker.
  * Use __Alt + Scroll__ to move the marker on the Y-axis.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Delete Objects:__ Allows you to remove vegetation objects in a chosen radius.
  * Press __Ctrl__ to remove all objects including Pieces and Items (__Warning:__ Very destructive).
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Shift + Scroll__ to adjust the camera distance.

* __Paint terrain:__ Allows you to reset the terrain "paint" per biome (grass in the Meadows, sand at beaches, etc). Can also paint dirt or paved onto every terrain. Can be used as a "brush" by holding down the Attack button continously.
  * Press __Q__ to switch between a circle and a square shaped marker.
  * Press __Ctrl__ to paint "dirt".
  * Press __Alt__ to paint "paved".
  * Use the __Scroll Wheel__ to change the tool radius.
  * Use __Ctrl + Scroll__ to rotate the square marker.
  * Use __Shift + Scroll__ to adjust the camera distance.

### Clipboard

![Blueprint tools](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintClipboard.png)

You can find all of the temporary blueprints you make using the "Copy" command of the "Edit Selection" tool here. These are reset on every logout.

### Blueprints

![Blueprint tools](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintBlueprints.png)

Place a blueprint as planned pieces. Select your previously saved blueprint and place it anywhere in the world. This works just like any other vanilla building piece. Additionally there are some extra controls to make placing your structures exactly as you want them as easy as possible:

* Use __Scroll__ to rotate the blueprint.
* Use __Ctrl + Scroll__ to move the blueprint on the Z-axis.
* Use __Alt + Scroll__ to move the blueprint on the X-axis.
* Use __Ctrl + Alt + Scroll__ to move the blueprint on the Y-axis.
* Use __Shift + Scroll__ to adjust the camera distance.
* There is a (server enforced) config option to allow placing the blueprints as regular pieces, so you can configure per server if you want to allow "cheating" structures without resources. When enabled, build your structures without building costs by pressing __Ctrl__ while placing the blueprint. Admin's are always allowed to "direct build".

## Blueprint Marketplace

![Blueprint mode](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMarket.png)

Manage and share your blueprints through a custom GUI added to the game. Per default the marketplace is accessible via __End__ key. Rename your local blueprints, assign a Category for them (which will create a new tab on your Blueprint rune for those blueprints) or add a description to them. If a server has this feature enabled, upload your local blueprints to that server so others can download and build your creations as well. Players with admin rights on a server can also manage the server side list through that interface.

### Marketplace Pieces

![Blueprint pieces](https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintPieces.png)

You can place one of two new rune themed pieces in the world which provide access to your own blueprints and the market on the server. If you want to completely stop clients from accessing the server blueprints via Hotkey, there is a server enforced config which disables that Hotkey for all clients.

## Installing

It is recommended to use a mod manager to install PlanBuild and all of its dependencies.

If you want to install it manually, load all of these mods as they are all required for PlanBuild to function and install them according to their respective install instructions:

* [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim)
* [HookGenPatcher](https://valheim.thunderstore.io/package/ValheimModding/HookGenPatcher)
* [Jötunn, the Valheim Library](https://valheim.thunderstore.io/package/ValheimModding/Jotunn)

Finally extract *all* of the contents of the PlanBuild mod archive into ```<Valheim>\BepInEx\plugins\PlanBuild```

It is possible for clients not using PlanBuild to connect to a server using it. Those clients won't see any planned pieces but are still able to play. You can not connect to a server without PlanBuild when the mod is installed on your local game for griefing reasons.

## Compatibility

Fully compatible with:
* [Build Camera](https://www.nexusmods.com/valheim/mods/226)﻿
* [Craft from Containers](https://www.nexusmods.com/valheim/mods/40)﻿
* [ValheimRAFT](https://www.nexusmods.com/valheim/mods/1136)

The Hammer's PieceTable is scanned automatically, mods that add Pieces should be compatible. If you find a mod that adds pieces to the Hammer and they don't show up, please post a bug report with a link to the mod or join the [Jötunn Discord](https://discord.gg/DdUt6g7gyA) and ping ```@Jules``` or ```@MarcoPogo```.

## Configuration

A lot aspects of this mod are configurable either through the config file found in your game folder (```<Valheim>\BepInEx\configs\marcopogo.PlanBuild.cfg```) or using the [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager):
* __Server Settings__ (enforced on all clients connecting to a server)
  * __Allow direct build__: Allow placement of blueprints without materials on this server. Admins are always allowed to use it. (default __false__)
  * __Allow terrain tools__: Allow usage of the terrain modification tools on this server. Admins are always allowed to use them. (default __false__)
  * __Allow serverside blueprints__: Allow sharing of blueprints on this server. (default __false__)
  * __Allow clients to use the GUI toggle key__: Allow clients to use the Hotkey to access server blueprints. Admins are always allowed to use it. (default __true__)
  * __Excluded plan prefabs__: Comma separated list of prefab names to exclude from the planned piece table on this server. Admins are always allowed to build them.
* __Blueprints__
  * __Default build mode__: Default build mode when placing blueprints. (default __Plan__)
  * __Unlimited health__: Set Piece health to its maximum value when directly building blueprints. (default __false__)
  * __Place distance__: Place distance while using the Blueprint Rune. (default __50__)
  * __Camera offset increment__: Camera height change when holding the camera modifier key and scrolling. (default __2__)
  * __Invert camera offset scroll__: Invert the direction of camera offset scrolling. (default __false__)
  * __Placement offset increment__: Placement height change when holding the modifier key and scrolling. (default __0.1__)
  * __Invert placement height change scroll__: Invert the direction of placement offset scrolling. (default __false__)
  * __Selection increment__: Selection radius increment when scrolling. (default __1__)
  * __Invert selection scroll__: Invert the direction of selection scrolling. (default __false__)
  * __Selection connected check margin__: Distance of the shell used to check for connectedness. (default __0.01__)
  * __Show the transform bound grid__: Shows a grid around the blueprints' bounds to visualize the blueprints' edges. (default __false__)
  * __Tooltip enabled__: Show a tooltip with a bigger thumbnail for blueprint pieces. (default __true__)
  * __Tooltip Color__: Set the background color for the tooltip on blueprint pieces.
* __Directories__
  * __Blueprint search directory__ Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location (default __.__)
  * __Save directory__: Directory to save blueprint files, relative paths are relative to the valheim.exe location (default __BepInEx/config/PlanBuild/blueprints__)
* __Keybindings__
  * __Blueprint Marketplace GUI toggle key__: Hotkey to show the blueprint marketplace GUI (default __End__)
  * __ShiftModifier__: First modifier key to change behaviours on various tools (default __LeftShift__)
  * __CtrlModifier__: Second modifier key to change behaviours on various tools (default __LeftCtrl__)
  * __AltModifier__: Third modifier key to change behaviours on various tools (default __LeftAlt__)
  * __Toggle__: Key to switch between modes on various tools. (default __Q__)
* __Plans__:
  * __Plan unknown pieces__: Show all plans, even for pieces you don't know yet. (default __false__)
  * __Plan totem build radius__: Build radius of the plan totem (default __30__)
  * __Plan totem particle effects__: Show particle effects when building pieces with the plan totem. (default __true__)
* __Visual__
  * __Transparent Ghost Placement__: Apply plan shader to ghost placement (currently placing piece). (default __true__)
  * __Unsupported color__: Color of unsupported plan pieces.
  * __Supported color__: Color of supported plan pieces.
  * __Transparency__: Additional transparency for finer control. (default __30%__)
  * __Plan totem glow color__: Color of the glowing lines on the Plan totem.
    
## Console commands

PlanBuild adds some new console commands to the game:
* __plan.blacklist.print__ - Print out the server's plan blacklist
* __plan.blacklist.add__ - [prefab_name] Add a prefab to the server's plan blacklist
* __plan.blacklist.remove__ - [prefab_name] Removes a prefab from the server's plan blacklist
* __bp.local__ - Get the list of your local blueprints
* __bp.remove__ - [blueprint_id] Remove a local blueprint
* __bp.push__ - [blueprint_id] Upload a local blueprint to the current connected server
* __bp.server__ - Get the list of the current connected servers blueprints
* __bp.pull__ - [blueprint_id] Load a blueprint from the current connected server and add it to your local blueprints
* __bp.thumbnail__ - [blueprint_id] ([rotation]) Create a new thumbnail for a blueprint from the actual blueprint data, optionally provide additional rotation of the blueprint on the thumbnail
* __bp.regenthumbnails__ - Create a new thumbnail for all local blueprints
* __bp.undo__ - Undo your last built blueprint
* __bp.clearclipboard__ - Clear the clipboard category of all saved blueprints
* __selection.gui__ - Show the selection GUI
* __selection.clear__ - Clears the current selection
* __selection.copy__ - Copy the current selection as a temporary blueprint
* __selection.copywithsnappoints__ - Copy the current selection as a temporary blueprint including the vanilla snap points
* __selection.save__ - Save the current selection as a blueprint
* __selection.delete__ - Delete all prefabs in the current selection

## Building Community

Head over to the [Valheimians](https://www.valheimians.com) page to find a community of builders and share your own creations. PlanBuild blueprints are supported.

## Credits

The original PlanBuild mod was created by __[MarcoPogo](https://github.com/MathiasDecrock)__

Blueprint functionality originally created by __[Algorithman](https://github.com/Algorithman)__ & __[Jules](https://github.com/sirskunkalot)__

Blueprint Marketplace GUI created by __[Dreous](https://github.com/imcanida)__

All further coding by __[MarcoPogo](https://github.com/MathiasDecrock)__ & __[Jules](https://github.com/sirskunkalot)__

Made with Löve and __[Jötunn](https://github.com/Valheim-Modding/Jotunn)__

## Contact

Source available on GitHub: [https://github.com/sirskunkalot/PlanBuild](https://github.com/sirskunkalot/PlanBuild)﻿. All contributions welcome!

You can find us at the [Jötunn Discord](https://discord.gg/DdUt6g7gyA) (```Jules#7950``` and ```MarcoPogo#6095```).

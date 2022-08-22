[size=6][b]BIG REVAMP[/b][/size]
Version 0.10.0 of PlanBuild comes with a big internal code refactor and many changes to the handling of plans and blueprints. Please make sure to check out the documentation below. The biggest changes in a nutshell:

[list]
[*]Instead of switching plan and blueprint mode on the rune, you now have two distinct tools, the [b]Plan Hammer[/b] and the [b]Blueprint Rune[/b].
[*]Lots of improvements to the [b]Selection tools[/b]. You can add and remove parts of your buildings to a selection and need to use the [b]Edit Selection[/b] tool or the [b]selection.gui[/b] console command to save or copy your blueprint.
[*]You can copy+paste blueprints directly without having to save them (clipboard).
[*]Most actions can be reversed and replayed using the [b]bp.undo[/b] and [b]bp.redo[/b] console commands.
[*]New save dialogue which lets you enter a category and description directly
[*]The configuration was redesigned for more clarity. Please revise your configuration once after installing this version.
[/list]

[size=6][b]PlanBuild[/b][/size]
PlanBuild enables you to plan, copy and share your building creations in Valheim with ease. The mod adds two new tools to the game. The [b]Plan Hammer[/b] is used to plan your creations before actually gathering all the materials. When you are happy with your build, you can add the required building materials one by one or use a custom totem to automatically build the pieces for you. The [b]Blueprint Rune[/b] lets you copy, save or delete your creations as a single building piece which can also be shared with other players using the mod and also includes terrain modification tools for quick and more precise terraforming without using the Hoe or Cultivator.

[size=5][b]Planning[/b][/size]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanMode.png[/img]

Introducing a new item, the [b]Plan Hammer[/b], which can be crafted from a single log of [b]wood[/b] and the blessing of Odin shining upon you.

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_hammer.png[/img]

Plan the construction of pieces without the need to gather the resources first. Anyone can add the required resources to the planned structure later and finish the construction after it was placed.

The [b]Plan Hammer[/b] is compatible with custom piece tables from other mods. All custom pieces will be incorporated into the runes table for the planned pieces. You still need a [b]Hammer[/b] and the required crafting station to finish the construction.

Planned pieces that are [b]unsupported[/b] can not be finished. These pieces are also slightly more transparent so you can see what is and isn't supported. The planned pieces themselves do not require support, so you can build forever (if you can reach far enough).

Real pieces also snap to the planned pieces, so you could even use them as [b]spacers[/b] or [b]rulers[/b].

[size=4][b]Plan Totem[/b][/size]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanTotem.png[/img]

Build a [b]Plan Totem[/b] near your planned structures to be able to add resources in a centralized location for all individual pieces on the plan.

It will also protect existing structures. Any piece that is destroyed [u](not removed by the Hammer)[/u] will be replaced with a plan for that same piece in the same place!

This needs to be built with the vanilla Hammer tool and costs you [b]1 Wood[/b] and [b]1 Grey Dwarf Eye[/b].

[b][size=4]Skuld Crystal[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_crystal.png[/img]

Includes the [b]Skuld Crystal[/b], a wearable item that removes the shader effect from the blueprints, so you can see what the construction will look like when completed.

Create it from a single [b]Grey Dwarf Eye[/b].

[b]Watch your step![/b] The pieces are still not really there, and will not support you!

[b][size=5]Blueprinting[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMode.png[/img]

Introducing a new item, the [b]Blueprint Rune[/b], which can be crafted from a single [b]stone[/b] and festering willpower given to you by the gods.

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/Blueprints/Icons/BlueprintRuneIcon.png[/img]

Copy existing structures into [b]Blueprints[/b] and rebuild them as planned or regular pieces all at once. The blueprints are saved in and loaded from the filesystem as [b].blueprint[/b] files. Also supports [b].vbuild[/b] files (you can load and build your BuildShare saves with this mod)! After switching to the blueprint mode, the piece table of the Blueprint Rune offers three different categories:

[b][size=4]Tools[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintTools.png[/img]

The Blueprint Rune comes with a handful of tools to handle blueprint creation and terraforming.

[list]
[*][b]Create new blueprint[/b]: Directly create a blueprint from a selection indicated by the circle marker.
[list]
[*]Hold [b]Ctrl[/b] to highlight all pieces which will be saved into the blueprint
[*]Use the [b]Scroll Wheel[/b] to change the selection radius
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Add to selection[/b]: Add pieces to the current selection. Currently selected pieces will be highlighted in green. Per default only the hovered piece will be added to the selection. You can use various modifiers to change that behaviour.
[list]
[*]Press [b]Q[/b] to quickly switch to the "Remove from selection" tool
[*]Hold [b]Alt[/b] and click to add all connected pieces. This uses a flood fill to find connected pieces.
[*]Hold [b]Ctrl[/b] and click to add pieces in a radius
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the selection radius
[*]Hold [b]Shift[/b] and click once to define a "starting piece" and click again on another piece to select every piece in between those two.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Remove from selection[/b]: Remove pieces from the current selection. Currently selected pieces will be highlighted in green.  Per default only the hovered piece will be removed from the selection. You can use various modifiers to change that behaviour.
[list]
[*]Press [b]Q[/b] to quickly switch to the "Add to selection" tool
[*]Hold [b]Alt[/b] and click to remove all connected pieces. This uses a flood fill to find connected pieces.
[*]Hold [b]Ctrl[/b] and click to remove pieces in a radius
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the selection radius
[*]Hold [b]Shift[/b]  and click to clear the current selection
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Edit selection[/b]: Left click to open a menu. From here you can choose what to do with your current selection:
[list]
[*][b]Copy with custom SnapPoints[/b]: Make a temporary blueprint of your current selection. It will copy all pieces in the selection into a new building piece and automatically select that piece for you to build. The copy is also saved into the "Clipboard" category of the rune for you to access until you log out from the current world.
[*][b]Copy with vanilla SnapPoints[/b]: Make a temporary blueprint of your current selection which also includes all vanilla snap points of the pieces selected.
[*][b]Save[/b]: Save the current selection as a new blueprint into the file system. These blueprints are kapt between game sessions and can also be used in the marketplace and shared with other players.
[*][b]Delete[/b]: Delete all pieces in the current selection. This removes all the pieces without refunding the building materials.
[*][b]Cancel[/b]: Exit the menu without any action.
[/list][*][b]Snap point marker:[/b] Add snap point markers to all points you want to have as snap points in your blueprint. The rotation of the markers does not matter, only the center point. We highly suggest that you also use [url=https://www.nexusmods.com/valheim/mods/299]Snap points made easy[/url]﻿ so you can cycle through the snap points when placing the blueprint. [b]Note[/b]: You have to select the marker piece in order to capture it in the blueprint. Markers placed while having an active selection will automatically be added to that selection.
[list]
[*]Use [b]Remove[/b] to delete a placed marker again (just like you would delete a piece with the hammer).
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Center point marker:[/b] Add a center point marker to your blueprint to determine the center of the blueprint. This is where it will be anchored while placing it. If a blueprint does not have a center point marker, a bottom corner of the blueprint is found and used as the center. [b]Note[/b]: You have to select the marker piece in order to capture it in the blueprint. Markers placed while having an active selection will automatically be added to that selection.
[list]
[*]Use [b]Remove[/b] to delete a placed marker again (just like you would delete a piece with the hammer).
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list]
[*][b]Remove planned pieces:[/b] Delete planned pieces again. Per default only the hovered piece will be deleted. But you can use various modifiers to change that behaviour.
[list]
[*]Press [b]Ctrl[/b] to delete plans in a radius, can be used to clean up after using it to measure distances, or as a general cleanup tool. Resources that were already added to the unfinished plans will be refunded.
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the deletion radius.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Terrain Tools:[/b] Allows you to "flatten" the terrain in a chosen radius or remove previously made modifications. Uses Valheim's TerrainCompiler and is 100% compatible with the vanilla game and modifications made with the Hoe for example.
[list]
[*]Press [b]Q[/b] to switch between a circle and a square shaped marker.
[*]Press [b]Ctrl[/b] to add smooth edges to the flattened area
[*]Press [b]Alt[/b] to remove terrain modifications.
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Ctrl + Scroll[/b] to rotate the square marker.
[*]Use [b]Alt + Scroll[/b] to move the marker on the Y-axis.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Delete Objects:[/b] Allows you to remove vegetation objects in a chosen radius.
[list]
[*]Press [b]Ctrl[/b] to remove all objects including Pieces and Items ([b]Warning:[/b] Very destructive).
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Paint terrain:[/b] Allows you to reset the terrain "paint" per biome (grass in the Meadows, sand at beaches, etc). Can also paint dirt or paved onto every terrain. Can be used as a "brush" by holding down the Attack button continously.
[list]
[*]Press [b]Q[/b] to switch between a circle and a square shaped marker.
[*]Press [b]Ctrl[/b] to paint "dirt".
[*]Press [b]Alt[/b] to paint "paved".
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Ctrl + Scroll[/b] to rotate the square marker.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][/list]

[b][size=4]Clipboard[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintClipboard.png[/img]

You can find all of the temporary blueprints you make using the "Copy" command of the "Edit Selection" tool here. These are reset on every logout.

[b][size=4]Blueprints[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintBlueprints.png[/img]

Place a blueprint as planned pieces. Select your previously saved blueprint and place it anywhere in the world. This works just like any other vanilla building piece. Additionally there are some extra controls to make placing your structures exactly as you want them as easy as possible:

[list]
[*]Use [b]Scroll[/b] to rotate the blueprint.
[*]Use [b]Ctrl + Scroll[/b] to move the blueprint on the Z-axis.
[*]Use [b]Alt + Scroll[/b] to move the blueprint on the X-axis.
[*]Use [b]Ctrl + Alt + Scroll[/b] to move the blueprint on the Y-axis.
[*]Use [b]Q[/b] to reset the offset on all axes.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[*]There is a (server enforced) config option to allow placing the blueprints as regular pieces, so you can configure per server if you want to allow "cheating" structures without resources. When enabled, build your structures without building costs by pressing [b]Ctrl[/b] while placing the blueprint. Admins are always allowed to "direct build". You can change the default building behaviour in the config file.
[/list]

[b][size=4]Undo/Redo[/size][/b]

The Blueprint Rune features an undo/redo mechanic for most of its actions. Placed blueprints, terrain/paint modifications and object deletions can be reversed using the __bp.undo__ console command. A reversed action can also be replayed using the __bp.redo__ console command. For easy access we recommend binding those commands to a key of your liking. To bind those commands to your mouse's "back" and "forth" keys for example type this into the game's console:

[code]
bind mouse3 bp.undo
bind mouse4 bp.redo
[/code]

[b][size=5]Blueprint Marketplace[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMarket.png[/img]

Manage and share your blueprints through a custom GUI added to the game. Per default the marketplace is accessible via [b]End[/b] key. Rename your local blueprints, assign a Category for them (which will create a new tab on your Blueprint rune for those blueprints) or add a description to them. If a server has this feature enabled, upload your local blueprints to that server so others can download and build your creations as well. Players with admin rights on a server can also manage the server side list through that interface.

[b][size=5]Marketplace Pieces[/size][/b]
[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintPieces.png[/img]

You can place one of two new rune themed pieces in the world which provide access to your own blueprints and the market on the server. If you want to completely stop clients from accessing the server blueprints via Hotkey, there is a server enforced config which disables that Hotkey for all clients.

[b][size=5]Installing[/size][/b]
It is recommended to use a mod manager to install PlanBuild and all of its dependencies.

If you want to install it manually, load all of these mods as they are all required for PlanBuild to function and install them according to their respective install instructions:

[list]
[*][url=https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim]BepInExPack for Valheim[/url]
[*][url=https://valheim.thunderstore.io/package/ValheimModding/HookGenPatcher]HookGenPatcher[/url]
[*][url=https://valheim.thunderstore.io/package/ValheimModding/Jotunn]Jötunn, the Valheim Library[/url]
[/list]

Finally extract [i]all[/i] of the contents of the PlanBuild mod archive into [pre]<Valheim>\BepInEx\plugins\PlanBuild[/pre]

It is possible for clients not using PlanBuild to connect to a server using it. Those clients won't see any planned pieces but are still able to play. You can not connect to a server without PlanBuild when the mod is installed on your local game for griefing reasons.

[b][size=5]Compatibility[/size][/b]
Fully compatible with:

[list]
[*][url=https://www.nexusmods.com/valheim/mods/226]Build Camera[/url]
[*][url=https://www.nexusmods.com/valheim/mods/40]Craft from Containers[/url]
[*][url=https://www.nexusmods.com/valheim/mods/1136]ValheimRAFT[/url] (partial compat, blueprinting does not work)
[*][url=https://github.com/redseiko/ValheimMods/releases/tag/ComfyGizmo-v1.3.0]ComfyGizmo[/url]
[/list]

The Hammer's PieceTable is scanned automatically, mods that add Pieces should be compatible. If you find a mod that adds pieces to the Hammer and they don't show up, please post a bug report with a link to the mod or join the [url=https://discord.gg/DdUt6g7gyA]Jötunn Discord[/url] and ping [pre]@Jules[/pre] or [pre]@MarcoPogo[/pre].

[b][size=5]Configuration[/size][/b]
A lot aspects of this mod are configurable either through the config file found in your game folder ([pre]<Valheim>\BepInEx\configs\marcopogo.PlanBuild.cfg[/pre]) or using the [url=https://github.com/BepInEx/BepInEx.ConfigurationManager]BepInEx ConfigurationManager[/url]:

[list]
[*][b]Server Settings[/b] (enforced on all clients connecting to a server)
[list]
[*][b]Allow direct build[/b]: Allow placement of blueprints without materials on this server. Admins are always allowed to use it. (default [b]false[/b])
[*][b]Allow terrain tools[/b]: Allow usage of the terrain modification tools on this server. Admins are always allowed to use them. (default [b]false[/b])
[*][b]Allow serverside blueprints[/b]: Allow sharing of blueprints on this server. (default [b]false[/b])
[*][b]Allow clients to use the GUI toggle key[/b]: Allow clients to use the Hotkey to access server blueprints. Admins are always allowed to use it. (default [b]true[/b])
[*][b]Excluded plan prefabs[/b]: Comma separated list of prefab names to exclude from the planned piece table on this server. Admins are always allowed to build them.
[/list][*][b]Blueprints[/b]
[list]
[*][b]Default build mode[/b]: Default build mode when placing blueprints. (default [b]Plan[/b])
[*][b]Unlimited health[/b]: Set Piece health to its maximum value when directly building blueprints. (default [b]false[/b])
[*][b]Place distance[/b]: Place distance while using the Blueprint Rune. (default [b]50[/b])
[*][b]Terrain smoothing[/b]: Smoothing value of the terrain tool when flattening with smoothing modifier key pressed. (default [b]0.5[/b])
[*][b]Camera offset increment[/b]: Camera height change when holding the camera modifier key and scrolling. (default [b]2[/b])
[*][b]Invert camera offset scroll[/b]: Invert the direction of camera offset scrolling. (default [b]false[/b])
[*][b]Placement offset increment[/b]: Placement height change when holding the modifier key and scrolling. (default [b]0.1[/b])
[*][b]Invert placement height change scroll[/b]: Invert the direction of placement offset scrolling. (default [b]false[/b])
[*][b]Selection increment[/b]: Selection radius increment when scrolling. (default [b]1[/b])
[*][b]Invert selection scroll[/b]: Invert the direction of selection scrolling. (default [b]false[/b])
[*][b]Selection connected check margin[/b]: Distance of the shell used to check for connectedness. (default [b]0.01[/b])
[*][b]Show the transform bound grid[/b]: Shows a grid around the blueprints' bounds to visualize the blueprints' edges. (default [b]false[/b])
[*][b]Tooltip enabled[/b]: Show a tooltip with a bigger thumbnail for blueprint pieces. (default [b]true[/b])
[*][b]Tooltip Color[/b]: Set the background color for the tooltip on blueprint pieces.
[*][b]Undo queue name[/b]: Global name of the blueprint undo queue used for bp.undo and bp.redo commands. Can be set to the same value as other mods' config (if supported) to combine their undo queues. (default [b]blueprintqueue[/b])
[*][b]Add player prefix to file name[/b]: Add your current player profile name to any blueprint file created with that player. (default [b]true[/b])
[/list][*][b]Directories[/b]
[list]
[*][b]Blueprint search directory[/b] Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location (default [b].[/b])
[*][b]Save directory[/b]: Directory to save blueprint files, relative paths are relative to the valheim.exe location (default [b]BepInEx/config/PlanBuild/blueprints[/b])
[/list][*][b]Keybindings[/b]
[list]
[*][b]Blueprint Marketplace GUI toggle key[/b]: Hotkey to show the blueprint marketplace GUI (default [b]End[/b])
[*][b]ShiftModifier[/b]: First modifier key to change behaviours on various tools (default [b]LeftShift[/b])
[*][b]CtrlModifier[/b]: Second modifier key to change behaviours on various tools (default [b]LeftCtrl[/b])
[*][b]AltModifier[/b]: Third modifier key to change behaviours on various tools (default [b]LeftAlt[/b])
[*][b]Toggle[/b]: Key to switch between modes on various tools. (default [b]Q[/b])
[/list][*][b]Plans[/b]:
[list]
[*][b]Plan unknown pieces[/b]: Show all plans, even for pieces you don't know yet. (default [b]false[/b])
[*][b]Plan totem build radius[/b]: Build radius of the plan totem (default [b]30[/b])
[*][b]Plan totem particle effects[/b]: Show particle effects when building pieces with the plan totem. (default [b]true[/b])
[/list][*][b]Visual[/b]
[list]
[*][b]Transparent Ghost Placement[/b]: Apply plan shader to ghost placement (currently placing piece). (default [b]true[/b])
[*][b]Unsupported color[/b]: Color of unsupported plan pieces.
[*][b]Supported color[/b]: Color of supported plan pieces.
[*][b]Transparency[/b]: Additional transparency for finer control. (default [b]30%[/b])
[*][b]Plan totem glow color[/b]: Color of the glowing lines on the Plan totem.
[/list][/list]

[b][size=5]Console commands[/size][/b]
PlanBuild adds some new console commands to the game:

[list]
[*][b]plan.blacklist.print[/b] - Print out the server's plan blacklist
[*][b]plan.blacklist.add[/b] - [prefab_name] Add a prefab to the server's plan blacklist
[*][b]plan.blacklist.remove[/b] - [prefab_name] Removes a prefab from the server's plan blacklist
[*][b]bp.local[/b] - Get the list of your local blueprints
[*][b]bp.remove[/b] - [blueprint_id] Remove a local blueprint
[*][b]bp.push[/b] - [blueprint_id] Upload a local blueprint to the current connected server
[*][b]bp.server[/b] - Get the list of the current connected servers blueprints
[*][b]bp.pull[/b] - [blueprint_id] Load a blueprint from the current connected server and add it to your local blueprints
[*][b]bp.thumbnail[/b] - [blueprint_id] ([rotation]) Create a new thumbnail for a blueprint from the actual blueprint data, optionally provide additional rotation of the blueprint on the thumbnail
[*][b]bp.regenthumbnails[/b] - Create a new thumbnail for all local blueprints
[*][b]bp.undo[/b] - Undo your last rune action (build, delete or terrain)
[*][b]bp.redo[/b] - Redo your last undone rune action (build, delete or terrain)
[*][b]bp.clearclipboard[/b] - Clear the clipboard category of all saved blueprints
[*][b]selection.gui[/b] - Show the selection GUI
[*][b]selection.clear[/b] - Clears the current selection
[*][b]selection.copy[/b] - Copy the current selection as a temporary blueprint
[*][b]selection.copywithsnappoints[/b] - Copy the current selection as a temporary blueprint including the vanilla snap points
[*][b]selection.cut[/b] - Cut out (copy and delete) the current selection as a temporary blueprint
[*][b]selection.cutwithsnappoints[/b] - Cut out (copy and delete) the current selection as a temporary blueprint including the vanilla snap points
[*][b]selection.save[/b] - Save the current selection as a blueprint
[*][b]selection.delete[/b] - Delete all prefabs in the current selection
[/list]

[b][size=5]Building Community[/size][/b]
Head over to the [url=https://www.valheimians.com]Valheimians[/url] page to find a community of builders and share your own creations. PlanBuild blueprints are supported.

[b][size=5]Credits[/size][/b]
The original PlanBuild mod was created by [b][url=https://github.com/MathiasDecrock]MarcoPogo[/url][/b]

Blueprint functionality originally created by [b][url=https://github.com/Algorithman]Algorithman[/url][/b] & [b][url=https://github.com/sirskunkalot]Jules[/url][/b]

Blueprint Marketplace GUI created by [b][url=https://github.com/imcanida]Dreous[/url][/b]

All further coding by [b][url=https://github.com/MathiasDecrock]MarcoPogo[/url][/b] & [b][url=https://github.com/sirskunkalot]Jules[/url][/b]

Special thanks to [b][url=https://github.com/JereKuusela]Jere[/url][/b] for exchanging code and ideas

Made with Löve and [b][url=https://github.com/Valheim-Modding/Jotunn]Jötunn[/url][/b]

[b][size=5]Contact[/size][/b]
Source available on GitHub: [url=https://github.com/sirskunkalot/PlanBuild]https://github.com/sirskunkalot/PlanBuild[/url]﻿. All contributions welcome!

You can find us at the [url=https://discord.gg/DdUt6g7gyA]Jötunn Discord[/url] ([pre]Jules#7950[/pre] and [pre]MarcoPogo#6095[/pre]).
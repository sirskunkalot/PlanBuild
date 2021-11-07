[size=6][b]PlanBuild - Blueprint Edition[/b][/size]

PlanBuild enables you to plan, copy and share your building creations in Valheim with ease.

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/Blueprints/Icons/BlueprintRuneIcon.png[/img]

Introducing a new item, the [b]Blueprint Rune[/b], which can be crafted from a single stone and festering willpower given to you by the gods. The rune comes with two modes you can switch between by pressing [b]P[/b] while having the rune equipped (can be adjusted via config file).

[size=5][b]Planning mode[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanMode.png[/img]

Plan the construction of pieces without the need to gather the resources first. Anyone can add the required resources to the planned structure later and finish the construction after it was placed.

The [b]Blueprint Rune[/b] is compatible with custom piece tables from other mods. All custom pieces will be incorporated into the runes table for the planned pieces. You still need a [b]Hammer[/b] and the required crafting station to finish the construction.

Planned pieces that are [b]unsupported[/b] can not be finished. These pieces are also slightly more transparent so you can see what is and isn't supported. The planned pieces themselves do not require support, so you can build forever (if you can reach far enough).

Real pieces also snap to the planned pieces, so you could even use them as [b]spacers[/b] or [b]rulers[/b].

[size=4][b]Plan Totem[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/PlanTotem.png[/img]

Build a [b]Plan Totem[/b] near your planned structures to be able to add resources in a centralized location for all individual pieces on the plan.

It will also protect existing structures. Any piece that is destroyed [i](not removed by the Hammer)[/i] will be replaced with a plan for that same piece in the same place!

This needs to be built with the vanilla Hammer tool and costs you [b]5 Fine Wood[/b], [b]5 Grey Dwarf Eye[/b] and [b]1 Surtling Core[/b].

[size=4][b]Skuld Crystal[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuildUnity/Assets/PlanBuild/Icons/plan_crystal.png[/img]

Includes the [b]Skuld Crystal[/b], a wearable item that removes the shader effect from the blueprints, so you can see what the construction will look like when completed.

Create it by combining a [b]Ruby[/b] and a [b]Grey Dwarf Eye[/b].

[b]Watch your step![/b] The pieces are still not really there, and will not support you!

[size=5][b]Blueprint mode[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMode.png[/img]

Copy existing structures into [b]Blueprints[/b] and rebuild them as planned or regular pieces all at once. The blueprints are saved in and loaded from the filesystem as [b].blueprint[/b] files. Also supports [b].vbuild[/b] files (you can load and build your BuildShare saves with this mod)! After switching to the blueprint mode, the piece table of the Blueprint Rune offers two different categories:

[size=4][b]Tools[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintTools.png[/img]

The Blueprint Rune comes with a handful of tools to aid handling blueprint creation and building. All terrain tools can be disabled via server side enforced configuration. Here is a handy list:

[list]
[*][b]Create new blueprint:[/b] Create a blueprint of a construction. Planned pieces are captured as real pieces. 
[list]
[*]Press [b]Ctrl[/b] to see what pieces are currently selected. 
[*]Use the [b]Scroll Wheel[/b] to change the capture radius. 
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Add to selection:[/b]  Add pieces to the current selection. Currently selected pieces will be highlighted in green. Per default only the hovered piece will be added to the selection. You can use various modifiers to change that behaviour.
[list]
[*]Press [b]Alt[/b] to add all connected pieces. This uses a flood fill to find connected pieces.
[*]Press [b]Ctrl[/b] to add pieces in a radius
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the selection radius
[/list][*][b]Remove from selection:[/b] Remove pieces from the current selection. Currently selected pieces will be highlighted in green.  Per default only the hovered piece will be removed from the selection. You can use various modifiers to change that behaviour.
[list]
[*]Press [b]Alt[/b] to remove all connected pieces. This uses a flood fill to find connected pieces.
[*]Press [b]Ctrl[/b] to remove pieces in a radius
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the selection radius
[*]Press [b]Alt[/b] and [b]Ctrl[/b] and click to clear selection
[/list][*][b]Save selection:[/b] Click to save the current selection to a blueprint
[*][b]Snap point marker:[/b] Add snap point markers to all points you want to have as snap points in your blueprint. The rotation of the markers does not matter, only the center point. We highly suggest that you also use [url=https://www.nexusmods.com/valheim/mods/1504]Snap points made easy[/url]﻿﻿ so you can cycle through the snap points when placing the blueprint.
[*][b]Center point marker:[/b] Add a center point marker to your blueprint to determine the center of the blueprint. This is where it will be anchored while placing it. If a blueprint does not have a center point marker, a bottom corner of the blueprint is found and used as the center.
[*][b]Remove planned pieces:[/b] Delete planned pieces again. Per default only the hovered piece will be deleted. But you can use various modifiers to change that behaviour.
[list]
[*]Press [b]Alt[/b] to delete all plans that are associated with a placed blueprint. Plans that are already finished will not be removed. Resources that were already added to the unfinished plans will be refunded.
[*]Press [b]Ctrl[/b] to delete plans in a radius, can be used to clean up after using it to measure distances, or as a general cleanup tool. Resources that were already added to the unfinished plans will be refunded.
[*]Use the [b]Scroll Wheel[/b] while holding [b]Ctrl[/b] to change the deletion radius.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Terrain Tools:[/b] Allows you to "flatten" the terrain in a chosen radius or remove previously made modifications. Uses Valheim's TerrainCompiler and is 100% compatible with the vanilla game and modifications made with the Hoe for example.
[list]
[*]Press [b]Q[/b] to switch between a circle and a square shaped marker.
[*]Press [b]Alt[/b] to remove terrain modifications.
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Ctrl + Alt + Scroll[/b] to move the marker on the Y-axis.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Delete Objects:[/b] Allows you to remove vegetation objects in a chosen radius.
[list]
[*]Press [b]Ctrl[/b] to remove all objects including Pieces and Items ([b]Warning:[/b] Very destructive).
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][*][b]Paint terrain:[/b] Allows you to reset the terrain "paint" per biome (grass in the Meadows, sand at beaches, etc). Can also paint dirt or paved onto every terrain. Can be used as a "brush" by holding down the Attack button continuously.
[list]
[*]Press [b]Ctrl[/b] to paint "dirt".
[*]Press [b]Alt[/b] to paint "paved".
[*]Use the [b]Scroll Wheel[/b] to change the tool radius.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[/list][/list][size=4][b]
Blueprints[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintBlueprints.png[/img]

Place a blueprint as planned pieces. Select your previously saved blueprint and place it anywhere in the world. This works just like any other vanilla building piece. Additionally there are some extra controls to make placing your structures exactly as you want them as easy as possible:

[list]
[*]Use [b]Scroll[/b] to rotate the blueprint.
[*]Use [b]Ctrl + Scroll[/b] to move the blueprint on the Z-axis.
[*]Use [b]Alt + Scroll[/b] to move the blueprint on the X-axis.
[*]Use [b]Ctrl + Alt + Scroll[/b] to move the blueprint on the Y-axis.
[*]Use [b]Shift + Scroll[/b] to adjust the camera distance.
[*]There is a (server enforced) config option to allow placing the blueprints as regular pieces, so you can configure per server if you want to allow "cheating" structures without resources. When enabled, build your structures without building costs by pressing [b]Ctrl[/b] while placing the blueprint.
[/list]

[size=5][b]Blueprint Marketplace[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintMarket.png[/img]

Manage and share your blueprints through a custom GUI added to the game. Per default the marketplace is accessible via [b]End[/b] key. Rename your local blueprints and add a description to them. If a server has this feature enabled, upload your local blueprints to that server so others can download and build your creations as well. Players with admin rights on a server can also manage the server side list through that interface.

[size=4][b]Marketplace Pieces[/b][/size]

[img]https://raw.githubusercontent.com/sirskunkalot/PlanBuild/master/PlanBuild/resources/BlueprintPieces.png[/img]

You can place one of two new rune themed pieces in the world which provide access to your own blueprints and the market on the server. If you want to completely stop clients from accessing the server blueprints via Hotkey, there is a server enforced config which disables that Hotkey for all clients.

[size=5][b]Compatibility[/b][/size]

Compatible with:

[list]
[*][url=https://www.nexusmods.com/valheim/mods/226]Build Camera[/url]﻿ 
[*][url=https://www.nexusmods.com/valheim/mods/40]Craft from Containers[/url]﻿   
[*]﻿[url=https://www.nexusmods.com/valheim/mods/1136]ValheimRAFT[/url]
Placing blueprints on rafts work, still working on capturing rafts[/list]

The Hammer's PieceTable is scanned automatically, mods that add Pieces should be compatible. If you find a mod that adds pieces to the Hammer and they don't show up, try toggling the Blueprint Rune with [b]P[/b] which will trigger a rescan. If it still doesn't work, please post a bug report with a link to the mod.

[size=5][b]Installing[/b][/size]

Use Vortex to install, or if you want to install it manually, drop the "PlanBuild" folder into BepInEx\plugins (so you end up with BepInEx\plugins\PlanBuild). Make sure to include all files, not just the DLL!

This mod adds interactable objects, so [b]all[/b] clients & server will need this mod!

Most values are configurable:
[list]
[*]General[list]
[*]Show all plans, even for pieces you don't know yet (default [b]false[/b])
[*]Build radius of the Plan Totem (default [b]30[/b])
[*]Show particle effects when building pieces with the plan totem (default [b]true[/b])
[*]Comma separated list of prefab names to exclude from the planned piece table for non-admin players (not browsable via Mod Settings, edit 
directly or via console commands)[/list]
[*]Blueprint Market[list]
[*]Allow clients to use the Hotkey to access the marketplace (default [b]true[/b])
[*]Hotkey for the Blueprint Marketplace GUI (default [b]End[/b])
[*]Allow sharing of blueprints on this server (default [b]false[/b])
[/list]
[*]Blueprint Rune[list]
[*]Hotkey to switch between Blueprint Rune modes (default [b]P[/b])
[*]Allow building of blueprints as actual pieces without needing the resources (default [b]false[/b])
[*]Allow usage of the terrain modification tools (default [b]false[/b])
[*]Place distance of the Blueprint Rune (default [b]50[/b])
[*]Show a tooltip with a bigger thumbnail for blueprint pieces (default [b]true[/b])
[*]Set the background color for the tooltip on blueprint pieces
[*]Invert and sensitivity options for each input with the scroll wheel
[/list]
[*]Directories[list]
[*]Blueprint search directory (default [b].[/b] (current working directory, usually Valheim game install directory))
[*]Blueprint save directory (default [b]BepInEx/config/PlanBuild/blueprints[/b])
[/list]
[*]Keybinds[list]
[*]Define your modifier keys for blueprint and terrain tools
[/list]
[*]Visual[list]
[*]Apply plan shader to ghost placement (currently placing piece) (default [b]false[/b])
[*]Color of unsupported plan pieces (default [b]10% white[/b])
[*]Color of supported plan pieces (default [b]50% white[/b])
[*]Additional transparency for finer control (default [b]30%[/b])
[*]Color of the glowing lines on the Plan totem (default [b]cyan[/b])
[/list]
[/list]

[size=5][b]Console commands[/b][/size]

PlanBuild adds some new console commands to the game:
[list]
[*][b]bp.local[/b] - Get the list of your local blueprints
[*][b]bp.remove[/b] - [blueprint_id] Remove a local blueprint
[*][b]bp.push[/b] - [blueprint_id] Upload a local blueprint to the current connected server
[*][b]bp.server[/b] - Get the list of the current connected servers blueprints
[*][b]bp.pull[/b] - [blueprint_id] Load a blueprint from the current connected server and add it to your local blueprints
[*][b]bp.thumbnail[/b] - [blueprint_id] ([rotation]) Create a new thumbnail for a blueprint from the actual blueprint data, 
optionally provide additional rotation of the blueprint on the thumbnail[*][b]bp.regenthumbnails[/b] - Create a new thumbnail for all local blueprints
[*][b]plan.blacklist.print[/b] - Print out the server's plan blacklist
[*][b]plan.blacklist.add[/b] - [prefab_name] Add a prefab to the server's plan blacklist
[*][b]plan.blacklist.remove[/b] - [prefab_name] Removes a prefab from the server's plan blacklist
[/list]

[size=5][b]Building community[/b][/size]

Head over to the [url=https://www.valheimians.com]Valheimians[/url] page to find a community of builders and share your own creations. PlanBuild blueprints are supported.

[size=5][b]Credits[/b][/size]

Source available on GitHub: [url=https://github.com/sirskunkalot/PlanBuild]https://github.com/sirskunkalot/PlanBuild[/url]﻿. 

All contributions welcome!

You can also find us at the [url=https://discord.gg/DdUt6g7gyA]Jötunn Discord[/url].

The original PlanBuild mod was created by [b][url=https://github.com/MathiasDecrock]MarcoPogo[/url][/b]

Blueprint functionality originally created by [b][url=https://github.com/Algorithman]Algorithman[/url][/b] & [b][url=https://github.com/sirskunkalot]Jules[/url][/b]

Blueprint Marketplace GUI created by [b][url=https://github.com/imcanida]Dreous[/url][/b]

All further coding by [b][url=https://github.com/MathiasDecrock]MarcoPogo[/url][/b] & [b][url=https://github.com/sirskunkalot]Jules[/url][/b]

Made with Löve and [b][url=https://github.com/Valheim-Modding/Jotunn]Jötunn[/url][/b]
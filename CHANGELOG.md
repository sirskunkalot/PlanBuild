# Version 0.10.1
* Added stripping of Semicolons on "additionalText" to prevent CSV errors
* Added unlimited health to other types of prefabs (Trees, Destructibles)
* Added back "quick select tool" for direct blueprinting

# Version 0.10.0
* Split plan and blueprint functionality into two separate items
* __If you want to use plans, you must craft the new item "Plan Hammer" from your player crafting screen__
* Reorganized configuration entries for more clarity
* __Please revise your configuration once after installing this version__
* Added saving of local scaling into blueprints. Note that the scale is not saved/synced by the vanilla game for most of the prefabs, so an additional mod for piece scaling should be installed.
* Added saving of item stand and armor stand items. Loadouts only get placed when using "Direct Build".
* Added the ability to change blueprint categories via the Marketplace GUI
* Removed the blueprint "Quick Select" tool, use the add/remove/edit selection tools instead
* Added temporary capturing of blueprints from the "Edit Selection" tool at runtime without saving a file for it (copy+paste)
* Added deletion of selected pieces from the "Edit Selection" tool
* Added ability to capture blueprints using the vanilla snap points (can be used in copy+paste)
* Added ability to "finish" planned pieces without having the needed resources when the "no placement cost" debug option is active
* Changed building requirements for the plan totem (1x Wood, 1x Grey Dwarf Eye) and skuld crystal (1 x Grey Dwarf Eye)
* Added rotation to the square shaped marker
* Added config to set piece health to "unlimited" for direct builds
* __Check out the README file on github for more in-depth instructions on the new usage: https://github.com/sirskunkalot/PlanBuild/blob/master/README.md__

# Version 0.9.5
* Fixed placement of non-Piece prefabs

# Version 0.9.4
* Fixed placement offset not resetting
* Fixed custom inputs for 0.207.20

# Version 0.9.3
* Fixed manual selection tools

# Version 0.9.2
* Added config for default blueprint build mode
* Expanded plan totem inventory slots to 28
* Fixed NRE in main menu when using auga
* Fixed blueprint tooltip while using a gamepad

# Version 0.9.0
* Fixed syncing of blueprints from the server
* Fixed replacing of blueprints on capture not writing files
* Fixed piece description size for blueprints
* Needs at least Jötunn v2.4.0, Server needs PlanBuild v0.9.0 as well

# Version 0.8.5
* Lowered the scanning runs for new plan prefabs
* Fixed a NRE when a game object can be found but not instantiated

# Version 0.8.4
* Fixed capturing and placing of meta pieces
* Fixed breaking terrain mesh when making large changes at zone borders

# Version 0.8.3
* Fixed redundant blueprint manager GameObject when placing directly

# Version 0.8.2
* Fixed selection not resetting after capture
* Fixed Gizmo Reloaded rotation patch

# Version 0.8.1
* Fixed tooltip display on inventory items

# Version 0.8.0
* Added prefab blacklist for server owners to prevent non-admin users from planning or blueprinting pieces from that list
* Prefabs can be added directly in the config file or via console commands while being connected to the server
* Added custom piece table categories to the .blueprint format
* Added new Thumbnail creation by rendering the blueprint and taking a screenshot of it
* You can recreate your current blueprint thumbnails via console command "bp.thumbnail" for single blueprints and "bp.regenthumbnails" for all current blueprints
* Added a new tooltip overlay for blueprints previewing the blueprint's thumbnail
* Added Piece-only mode to the deletion tool
* Fixed camera offset in blueprint mode

# Version 0.7.1
* Fix requirements count in Plan Totem

# Version 0.7.0
* Plan Totems will now replace broken pieces in range with plans
* Added enable/disable to individual Plan Totems (both building & replacing with plans are controlled by this)

# Version 0.6.14
* Fix stuck square tool outline

# Version 0.6.13
* Fixed admin overrides for tools: direct place, terrain and marketplace GUI hotkey

# Version 0.6.12
* Added new tools to modify your blueprint selection manually before saving it - add or remove single pieces or connected structures all at once

# Version 0.6.11
* Fix GizmoReloaded patch when shifting blueprints (unwanted rotation)

# Version 0.6.10
* Fix ValheimRAFT support

# Version 0.6.9
* Made modifier keys in blueprint mode configurable
* Compress blueprints before transfer and always resize PNG files to max 160 width
* Needs at least Jötunn v2.3.7

# Version 0.6.8
* Compatibility patch for Gizmo Reloaded (Gizmo is disabled in blueprint mode because of conflicting usage of modifier keys)

# Version 0.6.7
* Fixed compatibility with Build Camera

# Version 0.6.6
* Made the totem piece collection more failsafe
* Better plan pieces compatibility
* Remove all GOs with EffectArea from plan pieces
* Reverted the reset between switching tools
* Capped the circle size to 100

# Version 0.6.5
* Fixed terrain tool removal not saving

# Version 0.6.4
* Enable clients without PlanBuild to connect to servers with PlanBuild installed but prevent clients with PlanBuild to connect to servers without it (needs Jötunn v2.3.3)

# Version 0.6.3
* Reset selection radius & offset when switching tools

# Version 0.6.2
* Fixed ValheimRAFT compat

# Version 0.6.1
* Fixed BuildCamera compat

# Version 0.6.0
* Hearth & Home update

# Version 0.5.2
* Fixed projector mask for paint / deletion tools
* Almost every aspect of the mod is translatable now (thx Dominowood371)
* Added more german translation

# Version 0.5.1
* Remove the ability of the BlueprintRune to "middle-mouse-delete" non-planned pieces
* Don't make noise and attract mobs when placing blueprints
* "Plan unknown pieces" is now a server enforced configuration
* Handle duplicate piece names to avoid new piece notification spam

# Version 0.5.0
* Made the mod mandatory on server and client again - too many griefing oppertunities with the new terrain tools
* Blueprint market GUI is now translatable (check PlanBuild\assets\Translations)
* Show ghosts for blueprints with missing pieces
* Fixed some errors with missing pieces
* More russian translation (thx Dominowood371)

# Version 0.4.3
* Added new russian translation

# Version 0.4.2
* Fixed new particle effects config option ...
* Added null check in RPC call, should fix infinite build issue

# Version 0.4.1
* Fixed compatibility with ValheimRAFT
* Added config for particle effects of Plan Totem

# Version 0.4.0
* Added new tools for terrain modification, terrain painting and vegetation/object deletion to the Blueprint Rune (controlled by server side config)
* Use the TerrainComp system from Valheim for all tools
* Removed flatten while placing a blueprint (too inaccurate, use the terrain tools before placing the blueprint)
* Added a square marker for terrain tools

# Version 0.3.5
* Hopefully fixed some NullReferenceExceptions

# Version 0.3.4
* Changed the way requirements are checked (for allow unknown pieces config option). Should fix the "new piece" spam

# Version 0.3.3
* Compatibility with ValheimRAFT!
* Added server-enforced config option to allow flattening terrain while placing blueprints

# Version 0.3.2
* New blueprint marketplace pieces
* Support for pieces in tools other than the hammer (like BuildIt and Clutter)
* Use bounds to calculate flatten (should be more accurate)
* Fixed material swap issue when hovering over a piece and using the Skuld Crystal

# Version 0.3.1
* Fixed center point marker issue with pieces lower than the marker

# Version 0.3.0
* New blueprint marketplace! Press "End" to open the server GUI. The server must be configured to accept the blueprints!
* Additional movement options when placing the blueprints
* Pieces no longer flash when selecting, instead you should press Ctrl to see the current selection
* Circle while selecting will remain horizontal now

# Version 0.2.12
* Fix blueprint rotation (oops)

# Version 0.2.11
* Fix markers
* Fix interaction with Gizmo
* Fix invisble hover-target piece with Skuld Crystal
* Added config for directories (scan & save)

# Version 0.2.10
* Allow right Shift & Ctrl (helps with Build Camera)

# Version 0.2.9
* Allow Misc items that can be placed as plans to be part of blueprints (no Misc pieces were allowed before)
* Preload blueprints on startup, should remove lag when equiping Blueprint Rune

# Version 0.2.8
* Set a minimum of 8 for placementDistance

# Version 0.2.7
* Changed the way the maxPlaceDistance is set while using BluePrint Rune, should be more compatible with other mods that also modify this

# Version 0.2.6
* Added height offset when placing blueprints, change it by Ctrl + Scrolling
* Added Undo to remove entire blueprints (will not work on blueprints from previous versions, sorry! use the Radius delete to remove them instead)
* Added Delete to remove all plans in a radius
* Plans now highlight in the "unsupported" color
* Changed the way that the ghost prefab is created, should be much better for performance. Let me know if you see anything wonky while placing a blueprint!
* Placing plans & blueprints no longer consume Stamina

# Version 0.2.5
* Added snap point markers for blueprints!
* Blueprint parsing is done later & is more forgiving, pieces that are not found show a warning instead

# Version 0.2.4
* Added some automatic fixing of prefabs that are not fully registered from other mods

# Version 0.2.1
* Fixed placement distance for blueprints

# Version 0.2.0
* Changed the Plan Hammer to the Blueprint Rune from Veilheim (many many thanks to Algorithman & sirskunkalot!!!)
* Added support for .vbuild & .blueprint files!
* Updated to Jotunn 2.0.11

# Version 0.1.8
* Set CreatorID of finished pieces, fixes refund issue (only 1/3 refunded) of completed pieces

# Version 0.1.7
* Compatibility with Equipment & Quick Slots

# Version 0.1.6
* Avoid updating known recipes if not required, hopefully this removes the new piece message spam

# Version 0.1.5
* Added null check to prevent issues if ScanHammer is called too early (Fixes compatibility issue with RRR NPCs)

# Version 0.1.4
* Updated to Jotunn 2.0.9
* Updated to Valheim 0.153.2

# Version 0.1.3
* Fixed (hopefully for real this time) incorrect stack count of dropped resources

# Version 0.1.2
* Resources are now dropped in stacks of 1, as a workaround for known issue with ItemDrops

# Version 0.1.1
* Added support for Craft from Containers
* Added late scan for custom pieces in case a prefab is not found in ZNetScene

# Version 0.1.0
* Updated JotunnLib to Jotunn
* Planned pieces no longer provide comfort
* Patches for BuildShare & Build Camera have been moved to main mod dll (Thanks for the help ramonsantana!)

# Version 0.0.4
* Added the repair "recipe" to the Plan Hammer
* Fixed issue with transparent pieces while wearing Skuld Crystal
* Fixed names of pieces so they are unique (Fixes compatibility issue with Comfort Tweaks)

# Version 0.0.3
* Handle pieces without WearNTear component (fixes compatibility issue with EpicLoot)
* Disabling the "Show all pieces" option now removes the pieces again

# Version 0.0.2
* Enemies will no longer target plans

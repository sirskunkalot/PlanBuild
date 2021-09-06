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

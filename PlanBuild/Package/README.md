Craft the **Blueprint Rune** with **1 Stone**. Press **P** to toggle between Planning & Blueprint Mode

## **Planning mode**
Plan the construction of pieces for free with the Blueprint Rune. 
Anyone can add the required resources to the plan and finish the construction.

You still need a hammer and the required crafting station to finish the construction.

Planned pieces that are unsupported can not be finished, these pieces are also slightly more transparent so you can see what is and isn't supported.The planned pieces themselves do not require support, so you can build forever (if you can reach far enough).

Real pieces also snap to the planned pieces, so you can also use them as spacers or rulers.
## **Blueprint mode**
Copy existing structures (or plans, these are saved as regular pieces) into a **.blueprint** file. **.vbuild** files are also supported!
These files are used to create **Blueprints**, which are replicas of your saved structure, but as planned pieces.

There is a (server enforced) config option to allow placing the blueprints as regular pieces, do this by pressing **Ctrl** while placing the blueprint.
## **Create blueprint**
Create a blueprint of a construction. Planned pieces are captured as real pieces. Use the scroll wheel to change the capture radius. Captured pieces will flash green. Use **Shift + scroll** to move the camera up.
## **Place blueprint**
Place a blueprint as planned pieces. Use **Ctrl + scroll** to move the placement up & down. Use **Shift + scroll** to move the camera up and down.
Hold **Ctrl** while placing the blueprint to place it as real pieces, this is a server enforced config (defaults to not allowed)
## **Snap point marker**
Add **Snap point markers** to all points you want to have as snap points in your blueprint. The rotation of the markers does not matter, only the center point.
I highly suggest that you also use [Snap points made easy](https://www.nexusmods.com/valheim/mods/299)﻿ so you can cycle through the snap points when placing the blueprint.

## **Center point  marker**
Add a **Center point marker** to your blueprint to determine the center of the blueprint, this is where it will be anchored while placing it. If a blueprint does not have a Center point marker, a bottom corner of the blueprint is found and used as the center.

## **Delete plans**
Delete plans in a radius, can be used to clean up after using it to measure distances, or as a general cleanup tool. Resources that were already added to the unfinished plans will be refunded.

## Undo blueprint**
Delete all plans that are associated with a placed blueprint. Plans that are already finished will not be removed. Resources that were already added to the unfinished plans will be refunded.

Includes the **Skuld Crystal**, a wearable item that removes the shader effect from the blueprints, so you can see what the construction will look like when completed.
**Watch your step!** The pieces are still not really there, and will not support you!
Create it by combining a **Ruby** and a **Grey Dwarf Eye**.

Fully compatible with:
* [Build Camera](https://www.nexusmods.com/valheim/mods/226)﻿
* [Craft from Containers](https://www.nexusmods.com/valheim/mods/40)﻿

The hammer's PieceTable is scanned automatically, mods that add Pieces should be compatible. If you find a mod that adds pieces to the hammer, but don't show up, try toggling the Blueprint Rune with **P**, it should trigger a scan. If it still doesn't work, please post a bug report with a link to the mod.

## Installing
Use Vortex to install, or if you want to install it manually, drop the "PlanBuild" folder into BepInEx\plugins (so you end up with BepInEx\plugins\PlanBuild).
Make sure to include all files, not just the DLL!

This mod adds interactable objects, so **all** clients & server will need this mod!

Most values are configurable:

* Hotkey to switch between Blueprint Rune modes (default **P**)
* Show all plans, even for pieces you don't know yet (default **false**)
* Apply plan shader to ghost placement (currently placing piece) (default **false**)
* Color of unsupported plan pieces (default **10% white**)
* Color of supported plan pieces (default **50% white**)
* Additional transparency (default **30%**)

Source available on GitHub: [https://github.com/MathiasDecrock/ValheimMods/tree/master/PlanBuild](https://github.com/MathiasDecrock/ValheimMods/tree/master/PlanBuild)﻿

All contributions welcome! You can also find me at the [Valheim Modding Discord](https://discord.gg/RBq2mzeu4z)

A massive thanks to **[Algorithman](https://github.com/Algorithman)** & **[sirskunkalot](https://github.com/sirskunkalot)** (helped me a lot to merge this into my mod!) for their Blueprint Rune!  Check out their mod [Veilheim](https://github.com/sirskunkalot/Veilheim)!
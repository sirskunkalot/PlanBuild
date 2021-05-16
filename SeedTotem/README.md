Feedback & Bug reports on Nexus please: https://www.nexusmods.com/valheim/mods/876

The Seed Totem disperses stored seeds in an area around it.

Place the totem using the Cultivator. It costs:

    5 Fine Wood
    5 Greydwarf Eyes
    1 Surtling Core
    1 Ancient Seed


Use a seed with the item hotkeys to restrict the totem to a single type of seed. It will always make sure that newly placed plants have enough room to grow, but it won't check to make sure that everything around it still has enough room to grow; a totem is not that clever. Limiting it to one type of seed solves this issue.

The green glow fades when the totem runs out of seeds, letting you know to add more.

Also works with trees!

Hold the Use key to add all permitted seeds in Inventory.

Give the totem a whack to harvest all ready plants in the area. (Does not work on trees)

The totem scans the Cultivator, anything that creates a Plant object is considered a "seed".
This means it will include both "Carrot" & "Carrot Seeds" for example.
Notably this does not include the Berry Bushes from Planting Plus. These are Pickable objects instead, they skip the Plant stage, but are permanent, so it should be fine to place them yourself.

Most values are configurable:
﻿Server config (enforced by Mod Config Enforcer)

    Dispersion radius: Dispersion radius of the Seed Totem (default: 5)
    Dispersion time: Time (in seconds) between each dispersion (default: 10)
    Space requirement margin: Extra distance to make sure plants have enough space (default: 0.1)
    Max retries: Number of retries when randomly sampling possible planting locations(default: 8) (Don't increase too much, can cause lag!)
    Max seeds in totem: Soft limit of number of seeds (like the Kiln or Smelters), 0 is no limit (default: 0)
    Dispersion count: Maximum number of plants to place when dispersing (default: 5)
    Harvest on hit: Should the Seed totem send out a wave to pick all pickables in radius when damaged? (default: true)
    Check for cultivated ground: Should the Seed totem also check for cultivated land? (default: true)
    Check for correct biome: Should the Seed totem also check for the correct biome? (default: true)
    Custom piece requirements: Load custom piece requirements from seed-totem-custom-requirements.json instead of default? (default: false)


﻿Local config (not enforced)
UI

    Build menu: In which build menu is the Seed totem located (default: Cultivator)
    Show queue: Show the current queue on hover (default: true)
    Language: File to use for localization, relative to SeedTotem folder


Graphical

    Glow lines color: Color of the glowing lines on the Seed Totem (default: green)
    Glow light color: Color of the light from the Seed totem (default: green)
    Glow light intensity: Intensity of the light from the Seed totem (default: 3)
    Glow flare color: Color of the light flare from the Seed totem (default: green)
    Glow flare size: Size of the light flare from the Seed totem (default: 3)


Source available on GitHub: https://github.com/MathiasDecrock/ValheimMods/tree/master/SeedTotem
All contributions are welcome!
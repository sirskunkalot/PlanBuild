Dynamically replace the sail textures with semi-transparent pixelated textures.

The texture is only replaced when the sail is active.

Fully client-side.

Use the Configuration hotkey (default F1) to choose the settings. The texture will be updated dynamically, so play around to choose your favorite look.

Configuration options:
    sailTransparencyDitheringModulo: Sail Transparency Dithering Mask Modulo (default: 4)
    sailTransparencyDitheringX: Sail Transparency Dithering Mask X Increment (default: 2)
	sailTransparencyDitheringY: Sail Transparency Dithering Mask Y Increment (default: 1)
	sailTransparencyShaderTransparency: Additional transparency
	sailTransparencyWhen: When should the sail be transparent?
	 - steering: Only when steering the boat
	 - current_boat: Only for the boat you are currently on
	 - all_boats: All boats

Source available on GitHub: https://github.com/MathiasDecrock/Valheim-TransparentSail

All contributions are welcome!
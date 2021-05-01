# ThunderRoad-SimpleBallistics
A custom ballistics framework for the VR game 'Blade &amp; Sorcery'. Designed to respect the imbue classes and methods introduced in ThunderRoad U8

## Changelog

====Version 1.9.0====
- Updated mod framework for Game Version U9.2

====Version 1.8.0====
- Fixed imbuement breaking bugs
- Continued improvement of flintlock system
- Added additional error-checking for helper functions

====Version 1.7.1====
- Fixed Vortex mod-manager installs
- Added NPC Firearm Wave: "Dual Wielding Magnums"
- Updated "Drakefire" (as an example/template Flintlock firearm)
- Added JSON swtiches to allow modders to make Flintlock pistols
- Updated example prefabs for modders

====Version 1.7.0====
- Updated Elemental Firearms for U9 (Performance Update)

====Version 1.6.0====
- Fixed bug that allowed rifles to be shot from the foregrip
- Added Drakefire Musket Pistol (and generally added code support for muskets)
- Added additional NPC firearm waves
- Added NPC dual-wielding for pistols
- Tweaked NPC AI and containers

====Version 1.5.0====
- Updated codebase for u8.4 asynchronous spawning methods
- Updated assets and JSONs to new addressable asset structure
- Tweaked code for NPC waves/damage

 ** Known Issues **
 - Shooting a fire-imbued bullet at the skybox will cause post-processing to be washed out for a short time.

====Version 1.4.1====
- Add stability for modders in case the JSON is not correctly setup
- Allow a list of allowed firemodes to now be set in JSON

====Version 1.4.0====
- Added NPC Waves: Five (5) new waves at the bottom of the book, under "Firearm Waves"
    Details:
        * NPC interaction code added to framework
        * Added custom JSON templates for Brain, Container, CreatureTable & LootTable files
        * Enabled dual wielding for certain NPCs
        * Increased player health during Firearm waves

====Version 1.3.0====
- Continued tweaking on bullet physics
- Code optimization and refactoring (hopefully less lag overall after some initial spikes)
- Imbuement effects only last shortly now

====Version 1.2.0====
- Added two (2) new pistol variants: "Nambu" and "Enforcer"
- Fixed handle orientations (gun grips now held at 30 degree offset from horizontal)
- Improved bullet physics (better penetration, less "bounce" from indirect hits)
- Implemented ammo tracking and reloads (if "ammoCapacity" is set in JSON, otherwise pistol will have inf ammo)
- Added support for reload sounds
- Reduced the "bare minimum" required to create a Unity Prefab that works with the framework
- Removed support for `soundVolume` option in JSON (not compatible with AudioMixerLinker)
- Cleaned up some log message spam

====Version 1.1.0====
- Added support for fire mode selection. Currently supported fire modes: Safe, Semi-Auto, Bust, and Full-Auto
- Added 9mm Pistol Variant

====Version 1.0.0====
- Created framework and added Revolver as first concept weapon

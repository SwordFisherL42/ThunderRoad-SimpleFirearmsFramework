# ThunderRoad-SimpleBallistics
A custom ballistics framework for the VR game 'Blade &amp; Sorcery'. Designed to respect the imbue classes and methods introduced in ThunderRoad U8

## Changelog

====Version 1.2.0====
- Added two (2) new variants: "Nambu" and "Enforcer"
- Fixed handle orientations (grips now held at 30 degree offset from horizontal)
- Improved projectile physics (better penetration, less "bounce" from indirect hits)
- Implemented ammo tracking and reloads (if "ammoCapacity" is set in JSON, otherwise pistol will have inf ammo)
- Added support for reload sounds
- Reduced the "bare minimum" required to create a Unity Prefab that works with the framework
- Removed support for `soundVolume` option in JSON (not compatible with AudioMixerLinker)
- Cleaned up some log message spam

====Version 1.1.0====
- Added support for fire mode selection. Currently supported fire modes: Safe, Semi-Auto, Bust, and Full-Auto
- Added 9mm Variant

====Version 1.0.0====
- Created framework and added Revolver as first concept

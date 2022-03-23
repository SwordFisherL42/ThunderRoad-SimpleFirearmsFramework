# Simple Firearms Framework
A ballistics framework designed for the VR game 'Blade & Sorcery'. Offers users a simple module with customizable fields for easily implementing projectile weapons in VR.

## Deployment on NexusMods
The compiled framework (complete with sample weapons) is available on NexusMods here: https://www.nexusmods.com/bladeandsorcery/mods/2057

## Framework Usage Guide
For modders who want to leverage this framework for making their own *Blade & Sorcery* mods, a complete setup guide is provided here:

Google Doc ==> https://docs.google.com/document/d/1Q-JZaZaNhbhZ3duXws9UJB8yGYrqe7RyC_ypbH3RszE

## Sample implementation
Loaded as an item module, the framework allows for weapon behaviour and Unity prefab references to be set via JSON and passed to the main item script once initialized.
```
"modules":[
  {
      "$type": "SimpleBallistics.ItemModuleMagicFirearm, SimpleBallistics",
      "projectileID": "FisherElementalProjectile",
      "allowCycleFireMode": true,
      "fireMode": 1,
      "allowedFireModes": [0,1,2,3],
      "bulletForce": 10.0,
      "ammoCapacity": 0,
      "burstNumber": 3,
      "fireRate": 600,
      "throwMult": 1.0,
      "recoilMult": 1.0,
      "hapticForce": 4.0,
      "recoilForces": [
        0.0,
        0.0,
        600.0,
        800.0,
        -3000.0,
        -2000.0
      ],
      "recoilTorques": [
        500.0,
        700.0,
        0.0,
        0.0,
        0.0,
        0.0
      ],
      "npcMeleeEnableOverride": false,
      "npcMeleeEnableDistance": 0.4,
      "npcDistanceToFire": 15.0,
      "npcDamageToPlayer": 2.0,
      "npcRaycastPositionRef": "NPCRayCast",
      "muzzlePositionRef": "MuzzlePoint",
      "muzzleFlashRef": "MuzzleFlash",
      "animatorRef": "Animations",
      "fireAnim": "fire",
      "emptyAnim": "empty",
      "reloadAnim": "reload",
      "fireSoundRef": "FireSound",
      "emptySoundRef": "EmptySound",
      "switchSoundRef": "SwitchSound",
      "mainGripID": "Grip"
  }
]
```

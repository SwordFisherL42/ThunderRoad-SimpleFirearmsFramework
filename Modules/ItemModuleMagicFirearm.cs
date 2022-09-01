using ThunderRoad;

namespace SimpleBallistics
{
    public class ItemModuleMagicFirearm : ItemModule
    {
        public bool verbose_logging = false;
        // Unity prefab references
        public string muzzlePositionRef;
        public string muzzleFlashRef;
        public string fireSoundRef;
        public string emptySoundRef;
        public string swtichSoundRef;
        public string reloadSoundRef;
        public string animatorRef;
        public string fireAnim;
        public string emptyAnim;
        public string reloadAnim;
        //public string shellEjectionRef; // TODO: Particle system shell casings

        // Item definition references
        public string mainGripID;
        public string projectileID;
        public bool pooled = false;

        // NPC settings
        public string npcRaycastPositionRef;
        public float npcDistanceToFire = 10.0f;
        public bool npcMeleeEnableOverride = true;
        public float npcDamageToPlayer = 1.0f;
        public float npcDetectionRadius = 100f;
        public float npcMeleeEnableDistance = 0.5f;

        // Custom Behaviour Settings
        public bool loopedFireSound = false;
        public int ammoCapacity = 0;
        public bool allowCycleFireMode = false;
        public int fireMode = 1;
        public int[] allowedFireModes;
        public int burstNumber = 3;
        public int fireRate = 400;
        public float bulletForce = 10.0f;
        public float recoilMult = 1.0f;
        public float soundVolume;
        public float hapticForce = 4.0f;
        public float throwMult = 2.0f;
        public float[] recoilTorques = { 500f, 700f, 0f, 0f, 0f, 0f }; // x-min, x-max, y-min, y-max, z-min, z-max
        public float[] recoilForces = { 0f, 0f, 600f, 800f, -3000f, -2000f };  // x-min, x-max, y-min, y-max, z-min, z-max

        // Flintlock weapon settings
        public bool isFlintlock = false;        // Set weapon to Flintlock mode
        public bool waitForReloadAnim = false;  // Do not allow actions while Reload Animation is playing
        public bool waitForFireAnim = false;    // Wait for the Fire animation to finish before shooting the projectile/playing primary effects
        public string earlyFireSoundRef;        // First sound played (flintlock activation)
        public string earlyMuzzleFlashRef;      // First particle played (flintlock activation)
        public float flintlockDelay = 1.0f;     // Delay between PreFire effects and actual Fire (with main fire effects)

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSimpleFirearm>();
        }
    }
}

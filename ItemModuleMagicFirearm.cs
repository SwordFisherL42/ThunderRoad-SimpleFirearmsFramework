using ThunderRoad;

namespace SimpleBallistics
{
    public class ItemModuleMagicFirearm : ItemModule
    {
        // Unity prefab references
        public string projectileID;
        public string muzzlePositionRef;
        // NPC settings
        public string npcRaycastPositionRef;
        public float npcDistanceToFire = 10.0f;
        public bool npcMeleeEnableFlag = true;
        public float npcDamageToPlayer = 1.0f;
        //public string shellEjectionRef;
        public bool loopedFireSound = false;
        public string fireSoundRef;
        public string emptySoundRef;
        public string swtichSoundRef;
        public string reloadSoundRef;
        public string muzzleFlashRef;
        public string animatorRef;
        public string fireAnim;
        public string emptyAnim;
        public string reloadAnim;
        public string mainGripID;
        // Custom Behaviour Settings
        public int ammoCapacity = 0;
        public bool allowCycleFireMode = false;
        public int fireMode = 1;
        public int[] allowedFireModes;
        public int burstNumber = 3;
        public int fireRate = 400;
        public float bulletForce = 7.0f;
        public float recoilMult = 1.0f;
        public float soundVolume;
        public float hapticForce = 4.0f;
        public float throwMult = 2.0f;
        public float[] recoilTorques = { 500f, 700f, 0f, 0f, 0f, 0f }; // x-min, x-max, y-min, y-max, z-min, z-max
        public float[] recoilForces = { 0f, 0f, 600f, 800f, -3000f, -2000f };  // x-min, x-max, y-min, y-max, z-min, z-max

        // Settings added in 1.6.0 - 1.7.1
        public bool waitForReloadAnim = false;  // Do not allow actions while Reload Animation is playing
        public bool waitForFireAnim = false;    // Wait for the Fire animation to finish before shooting the projectile/playing primary effects
        public bool isFlintlock = false;        // Set weapon to Flintlock mode
        public string earlyFireSoundRef;        // First sound played (flintlock activation)
        public string earlyMuzzleFlashRef;      // First particle played (flintlock activation)
        public float flintlockDelay = 1.0f;     // Delay between PreFire effects and actual Fire (with main fire effects)

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemMagicFirearm>();
        }
    }
}

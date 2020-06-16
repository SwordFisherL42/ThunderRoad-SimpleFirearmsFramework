using ThunderRoad;

namespace SimpleBallistics
{
    public class ItemModuleMagicFirearm : ItemModule
    {
        //Unity prefab references
        public string projectileID;
        public string muzzlePositionRef;
        public string fireSoundRef; 
        public string emptySoundRef;
        public string swtichSoundRef;
        public string muzzleFlashRef;
        public string animatorRef;
        public string fireAnim;
        public string mainGripID;
        //Custom Behaviour Settings
        public bool allowCycleFireMode = false;
        public int fireMode = 1;
        public int burstNumber = 3;
        public int fireRate = 400;
        public float bulletForce = 7.0f;
        public float recoilMult = 1.0f;
        public float soundVolume = 1.0f;
        public float hapticForce = 4.0f;
        public float throwMult = 2.0f;
        public float[] recoilTorques = { 500f, 700f, 0f, 0f, 0f, 0f }; // x-min, x-max, y-min, y-max, z-min, z-max
        public float[] recoilForces = { 0f, 0f, 600f, 800f, -3000f, -2000f };  // x-min, x-max, y-min, y-max, z-min, z-max

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemMagicFirearm>();
        }
    }
}

using ThunderRoad;

namespace ImbuementController
{
    public class ItemModuleMagicFirearm : ItemModule
    {
        public string projectileID = "FisherMagicBullet";
        public string muzzlePositionRef = "MuzzlePoint";
        public string fireSoundRef = "FireSound";
        public string muzzleFlashRef = "MuzzleFlash";
        public string animatorRef = "Animations";
        public string fireAnim = "sw_fire";
        public float bulletForce = 7.0f;
        public float recoilMult = 1.0f;
        public float soundVolume = 1.0f;
        public string mainGripID = "Grip";
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

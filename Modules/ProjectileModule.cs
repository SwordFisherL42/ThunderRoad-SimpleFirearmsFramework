using ThunderRoad;
using static SimpleBallistics.FrameworkCore;

namespace SimpleBallistics
{
    public class ProjectileModule : ItemModule
    {
        private ProjectileType selectedType;
        public int projectileType = 1;
        public float lifetime = 1.5f;
        public bool allowFlyTime = true;

        public float throwMult = 1.0f;
        public float flyingAcceleration = 1.0f;
        public string lightRef;
        public string burstParticleRef;
        public string meshRef;
        public string colliderRef;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = (ProjectileType)weaponTypeEnums.GetValue(projectileType);
            if (selectedType.Equals(ProjectileType.Pierce)) item.gameObject.AddComponent<SimpleProjectile>();
            // TODO: Add additional projectile types in future versions
            //else if (selectedType.Equals(ProjectileType.Explosive)) item.gameObject.AddComponent<Weapons.SimpleExplosive>();
            //else if (selectedType.Equals(ProjectileType.Energy)) item.gameObject.AddComponent<Weapons.PlasmaBolt>();
        }
    }
}

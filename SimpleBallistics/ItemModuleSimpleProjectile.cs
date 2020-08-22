﻿using ThunderRoad;

namespace SimpleBallistics
{
    
    public class ItemModuleSimpleProjectile : ItemModule
    {
        public float lifetime = 10.0f;
        public bool allowFlyTime = true;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSimpleProjectile>();
        }
    }
}

using System.Collections;
using UnityEngine;
using ThunderRoad;

/* Description: An Item plugin for `ThunderRoad` which is required on any items
 * that are set up as a projectile. This class allows projectiles to be imbued 
 * via the AddChargeToQueue(...) method and defines an item lifetime for performance.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 06/15/2020
 * 
 */

namespace SimpleBallistics
{
    public class ItemSimpleProjectile : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleSimpleProjectile module;
        private float lifeDuration;
        private string spellQueue;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleSimpleProjectile>();
        }

        protected void Start()
        {
            lifeDuration = 0.0f;
            if (!string.IsNullOrEmpty(spellQueue))
            {
                TransferImbueCharge(spellQueue);
                spellQueue = null;
            }
        }

        private void LateUpdate()
        {
            if (!string.IsNullOrEmpty(spellQueue) && item.isActiveAndEnabled)
            {
                TransferImbueCharge(spellQueue);
                spellQueue = null;
            }
            lifeDuration += Time.deltaTime;
            if (lifeDuration >= module.lifetime)
            {
                item.Despawn();
            }
        }

        public void AddChargeToQueue(string SpellID)
        {
            spellQueue = SpellID;
        }

        public void TransferImbueCharge(string SpellID)
        {
            if (string.IsNullOrEmpty(SpellID)) return;
            SpellCastCharge transferedSpell = Catalog.GetData<SpellCastCharge>(SpellID, true).Clone();
            StartCoroutine(TransferDeltaEnergy(item.imbues[0], transferedSpell));
        }

        IEnumerator TransferDeltaEnergy(Imbue itemImbue, SpellCastCharge activeSpell, float energyDelta = 5.0f, int counts = 20)
        {
            for (int i = 0; i < counts; i++)
            {
                try
                {
                    itemImbue.Transfer(activeSpell, energyDelta);
                }
                catch { }
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;

        }
    }
}

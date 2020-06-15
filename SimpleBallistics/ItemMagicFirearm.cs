using UnityEngine;
using ThunderRoad;

/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 06/15/2020
 * 
 */

namespace ImbuementController
{
    public class ItemMagicFirearm : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleMagicFirearm module;

        private Transform muzzlePoint;
        private Item projectile;
        private Rigidbody projectileBody;
        private AudioSource fireSound;
        private ParticleSystem MuzzleFlash;
        private Animator Animations;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;
        private Handle gunGrip;

        public void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagicFirearm>();

            if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            else muzzlePoint = item.transform;

            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.definition.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.definition.GetCustomReference(module.animatorRef).GetComponent<Animator>();

            //Override SFX volume from JSON
            if (fireSound != null) fireSound.volume = module.soundVolume;

            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.definition.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }
        }

        public void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                Fire();
            }
        }


        public void SpawnProjectile(string projectileID, string currentSpell)
        {
            Debug.Log("[Magic-Guns] projectile currentSpell: " + currentSpell);
            var projectileData = Catalog.GetData<ItemPhysic>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Magic-Guns][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                projectile = projectileData.Spawn(true);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                item.IgnoreObjectCollision(projectile);
                //Set imbue charge on projectile using ItemSimpleProjectile subclass
                ItemSimpleProjectile projectileController = projectile.gameObject.GetComponent<ItemSimpleProjectile>();
                if (projectileController != null)
                {
                    projectileController.AddChargeToQueue(currentSpell);
                }
                
                projectile.transform.position = muzzlePoint.position;
                projectile.transform.rotation = Quaternion.Euler(muzzlePoint.rotation.eulerAngles);
                projectileBody = projectile.rb;
                projectileBody.velocity = item.rb.velocity;
                projectileBody.AddForce(projectileBody.transform.forward * 1000.0f * module.bulletForce);
                projectile.Throw(module.throwMult, Item.FlyDetection.CheckAngle);
            }

        }

        public void ApplyRecoil()
        {
            // Add angular + positional recoil to the gun
            if (module.recoilTorques != null)
            {
                item.rb.AddRelativeTorque(new Vector3(
                    Random.Range(module.recoilTorques[0], module.recoilTorques[1]) * module.recoilMult,
                    Random.Range(module.recoilTorques[2], module.recoilTorques[3]) * module.recoilMult,
                    Random.Range(module.recoilTorques[4], module.recoilTorques[5]) * module.recoilMult),
                    ForceMode.Impulse);
            }
            if (module.recoilForces != null)
            {
                item.rb.AddRelativeForce(new Vector3(
                    Random.Range(module.recoilForces[0], module.recoilForces[1]) * module.recoilMult,
                    Random.Range(module.recoilForces[2], module.recoilForces[3]) * module.recoilMult,
                    Random.Range(module.recoilForces[4], module.recoilForces[5]) * module.recoilMult));
            }
        }

        public string GetCurrentSpellChargeID()
        {
            string currentSpellID = "";
            foreach (Imbue itemImbue in item.imbues)
            {
                if (itemImbue.spellCastBase != null)
                {
                    currentSpellID = itemImbue.spellCastBase.id;
                }
            }
            return currentSpellID;
        }

        
        public void Fire()
        {
            PreFireEffects();
            SpawnProjectile(module.projectileID, GetCurrentSpellChargeID());
            ApplyRecoil();
        }

        //Effects/Actions to play before the projectile is spawned
        public void PreFireEffects()
        {
            if (MuzzleFlash!=null) MuzzleFlash.Play();
            if ((Animations != null)&&(!string.IsNullOrEmpty(module.fireAnim))) Animations.Play(module.fireAnim);
            if (fireSound != null) fireSound.Play();
            Haptics();
        }

        //Extended Interaction Functions, if a Grip is defined// 
        public void Haptics()
        {
            if (gunGripHeldRight) PlayerControl.handRight.HapticShort(module.hapticForce);
            if (gunGripHeldLeft) PlayerControl.handLeft.HapticShort(module.hapticForce);
        }

        public void OnMainGripGrabbed(Interactor interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

        }

        public void OnMainGripUnGrabbed(Interactor interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
        }

    }
}

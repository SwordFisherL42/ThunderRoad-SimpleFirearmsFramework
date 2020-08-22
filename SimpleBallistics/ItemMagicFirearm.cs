using UnityEngine;
using ThunderRoad;
using static SimpleBallistics.FirearmFunctions;

//using Firemode = SimpleBallistics.FirearmFunctions.FireMode;

/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 08/22/2020
 * 
 */

namespace SimpleBallistics
{
    public class ItemMagicFirearm : MonoBehaviour
    {
        //ThunderRoad references
        protected Item item;
        protected ItemModuleMagicFirearm module;
        private Handle gunGrip;
        //Unity references
        private Animator Animations;
        private Transform muzzlePoint;
        private ParticleSystem MuzzleFlash;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private AudioSource reloadSound;
        //Weapon logic references
        private FireMode fireModeSelection;
        private int remaingingAmmo;
        private bool infAmmo = false;
        private bool isEmpty = false;
        private bool triggerPressed;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;

        public void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagicFirearm>();

            if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            else muzzlePoint = item.transform;

            //Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.definition.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>(); 
            if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.definition.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.definition.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.definition.GetCustomReference(module.animatorRef).GetComponent<Animator>();

            //Setup ammo tracking 
            if (module.ammoCapacity > 0)
            {
                remaingingAmmo = (int)module.ammoCapacity;
            }
            else
            {
                infAmmo = true;
            }

            //TODO: BUG: Unable to set manually soundVolume due to issue with AudioMixerLinker script.
            //Override SFX volume from JSON
            //if ((module.soundVolume > 0.0f) && (module.soundVolume <= 1.0f))
            //{
            //    if (fireSound != null)
            //    {
            //        fireSound.volume = module.soundVolume;
            //    }
            //}

            //Get firemode based on numeric index of the enum
            fireModeSelection = (FireMode)fireModeEnums.GetValue(module.fireMode);

            //Handle interaction events
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
                triggerPressed = true;
                StartCoroutine(GeneralFire(Fire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound));
            }
            if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
            {
                //Stop Firing.
                triggerPressed = false;
            }
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (module.allowCycleFireMode && !isEmpty)
                {
                    if (emptySound != null) emptySound.Play();
                    fireModeSelection = CycleFireMode(fireModeSelection);
                }
                else
                {
                    //Reload the weapon is the empty flag has been set.
                    if (Animate(Animations, module.reloadAnim))
                    {
                        if (reloadSound != null) reloadSound.Play();
                    }
                    remaingingAmmo = module.ammoCapacity;
                    isEmpty = false;
                }

            }
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

        public bool TriggerIsPressed() { return triggerPressed; }

        public void PreFireEffects()
        {
            if (MuzzleFlash != null) MuzzleFlash.Play();
            // Last Shot
            if (remaingingAmmo == 1)
            {
                Animate(Animations, module.emptyAnim);
                isEmpty = true;
            }
            else
            {
                Animate(Animations, module.fireAnim);
            }
            if (fireSound != null) fireSound.Play();
        }

        public bool Fire()
        {
            //Returns 'true' if Fire was successful.
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                PreFireEffects();
                ShootProjectile(item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult);
                ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
                remaingingAmmo--;
                return true;
            }
            return false;
        }

    }
}


// WIP: HitScan (RayCast) functions for applying/calculating firearms damage //

/*protected void DamageCreatureCustom(Creature triggerCreature, float damageApplied, Vector3 hitPoint)
//{
//    try
//    {
//        if (triggerCreature.health.currentHealth > 0)
//        {
//            Debug.Log("[F-L42-RayCast] Damaging enemy: " + triggerCreature.name);
//            Debug.Log("[F-L42-RayCast] Setting MaterialData... ");
//            MaterialData sourceMaterial = Catalog.GetData<MaterialData>("Metal", true); //(MaterialData)null; 
//            MaterialData targetMaterial = Catalog.GetData<MaterialData>("Flesh", true); //(MaterialData)null;
//            Debug.Log("[F-L42-RayCast] Fetching MaterialEffectData... ");
//            MaterialEffectData daggerEffectData = Catalog.GetData<MaterialEffectData>("DaggerPierce", true);

//            //Damager daggerDamager = new Damager();
//            //DamagerData daggerDamagerData = Catalog.GetData<DamagerData>("DaggerPierce", true);
//            //daggerDamager.Load(daggerDamagerData);
//            Debug.Log("[F-L42-RayCast] Defining DamageStruct... ");
//            DamageStruct damageStruct = new DamageStruct(DamageType.Pierce, damageApplied)
//            {
//                materialEffectData = daggerEffectData
//            };
//            Debug.Log("[F-L42-RayCast] Defining CollisionStruct... ");
//            CollisionStruct collisionStruct = new CollisionStruct(damageStruct, (MaterialData)sourceMaterial, (MaterialData)targetMaterial)
//            {
//                contactPoint = hitPoint
//            };
//            Debug.Log("[F-L42-RayCast] Applying Damage to creature... ");
//            triggerCreature.health.Damage(ref collisionStruct);
//            Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

//            Debug.Log("[F-L42-RayCast] SpawnEffect... ");
//            if (collisionStruct.SpawnEffect(sourceMaterial, targetMaterial, false, out EffectInstance effectInstance))
//            {
//                effectInstance.Play();
//            }
//            Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

//        }
//    }
//    catch
//    {
//        Debug.Log("[F-L42-RayCast][ERROR] Unable to damage enemy!");
//    }
//}
*/

/*public void RayCastFire()
//{
//    Debug.Log("[F-L42-RayCastFire] Called RayCastFire ... ");
//    var rayCastHit = Physics.Raycast(muzzlePoint.position, muzzlePoint.TransformDirection(Vector3.forward), out RaycastHit hit);
//    if (rayCastHit)
//    {
//        Debug.Log("[F-L42-RayCastFire] Hit! " + hit.transform.gameObject.name);
//        Debug.DrawRay(muzzlePoint.position, muzzlePoint.TransformDirection(Vector3.forward) * hit.distance, Color.red);
//        if (hit.collider.attachedRigidbody != null)
//        {
//            Debug.Log("[F-L42-RayCastFire] Hit Attached RigidBody ");
//            Debug.Log("[F-L42-RayCastFire] Force Applied to RB! ");
//            hit.collider.attachedRigidbody.AddForceAtPosition(muzzlePoint.TransformDirection(Vector3.forward) * module.hitForce, hit.point);
//            var targetCreature = hit.collider.attachedRigidbody.gameObject.GetComponent<Creature>();
//            if (targetCreature != null)
//            {
//                //Creature triggerCreature = hitPart.ragdoll.creature;
//                Debug.Log("[F-L42-RayCastFire] Creature Hit: " + targetCreature.name);
//                if (targetCreature == Creature.player) return;
//                DamageCreatureCustom(targetCreature, 20f, hit.point);
//            }
//        }
//    }
//}
*/
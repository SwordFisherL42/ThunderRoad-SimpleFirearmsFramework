using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;

/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 08/15/2020
 * 
 */

namespace SimpleBallistics
{
    public enum FireMode
    {
        Misfire = 0,
        Single = 1,
        Burst = 2,
        Auto = 3
    }

    public class ItemMagicFirearm : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleMagicFirearm module;

        //Unity Prefab objects
        private Animator Animations;
        private Handle gunGrip;
        private Transform muzzlePoint;
        private ParticleSystem MuzzleFlash;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private AudioSource reloadSound;
        //Ammo counter logic
        private int remaingingAmmo;
        private bool infAmmo = false;
        private bool isEmpty = false;
        //Selection mode logic
        private FireMode fireModeSelection;
        private int selectionIndex;
        private readonly Array fireModeEnums = Enum.GetValues(typeof(FireMode));
        //Extendeded interaction logic
        private bool triggerPressed;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;
        //Projectile references
        private Item projectile;
        private Rigidbody projectileBody;

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

            //BUG: Unable to set manually soundVolume due to issue with AudioMixerLinker script.
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
                StartCoroutine(GeneralFire(fireModeSelection, module.fireRate, module.burstNumber));
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
                    CycleFireMode();
                }
                else
                {
                    //Reload the weapon is the empty flag has been set.
                    if (Animations != null && !string.IsNullOrEmpty(module.reloadAnim))
                    {
                        if (reloadSound != null) reloadSound.Play();
                        Animations.Play(module.reloadAnim);
                    }
                    remaingingAmmo = module.ammoCapacity;
                    isEmpty = false;
                }

            }
        }

        public void SpawnProjectile(string projectileID, string currentSpell)
        {
            //Debug.Log("[Magic-Guns] projectile currentSpell: " + currentSpell);
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
                    UnityEngine.Random.Range(module.recoilTorques[0], module.recoilTorques[1]) * module.recoilMult,
                    UnityEngine.Random.Range(module.recoilTorques[2], module.recoilTorques[3]) * module.recoilMult,
                    UnityEngine.Random.Range(module.recoilTorques[4], module.recoilTorques[5]) * module.recoilMult),
                    ForceMode.Impulse);
            }
            if (module.recoilForces != null)
            {
                item.rb.AddRelativeForce(new Vector3(
                    UnityEngine.Random.Range(module.recoilForces[0], module.recoilForces[1]) * module.recoilMult,
                    UnityEngine.Random.Range(module.recoilForces[2], module.recoilForces[3]) * module.recoilMult,
                    UnityEngine.Random.Range(module.recoilForces[4], module.recoilForces[5]) * module.recoilMult));
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

        public void CycleFireMode()
        {
            selectionIndex = (int)fireModeSelection;
            selectionIndex++;
            if (selectionIndex >= fireModeEnums.Length) selectionIndex = 0;
            fireModeSelection = (FireMode)fireModeEnums.GetValue(selectionIndex);
        }

        public bool Fire()
        {
            //Returns 'true' if Fire was successful.
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                PreFireEffects();
                SpawnProjectile(module.projectileID, GetCurrentSpellChargeID());
                ApplyRecoil();
                remaingingAmmo--;
                return true;
            }
            return false;
        }

        private IEnumerator GeneralFire(FireMode fireSelector = FireMode.Single, int fireRate = 60, int burstNumber = 3)
        {
            //Assuming fireRate as Rate-Per-Minute, convert to adequate deylay between shots, given by fD = 1/(fR/60) 
            float fireDelay = 60.0f / (float)fireRate;
            //Based on selection mode, perform the expected behaviours
            if (fireSelector == FireMode.Misfire)
            {
                if (emptySound != null) emptySound.Play();
                yield return null;
            }

            else if (fireSelector == FireMode.Single)
            {
                if (!Fire())
                {
                    StartCoroutine(GeneralFire(FireMode.Misfire));
                }
                yield return new WaitForSeconds(fireDelay);
            }

            else if (fireSelector == FireMode.Burst)
            {
                for (int i = 0; i < burstNumber; i++)
                {
                    if (!Fire())
                    {
                        StartCoroutine(GeneralFire(FireMode.Misfire));
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
                yield return null;
            }

            else if (fireSelector == FireMode.Auto)
            {
                while (triggerPressed) //triggerPressed is handled by OnHeldAction() events
                {
                    if (!Fire())
                    {
                        StartCoroutine(GeneralFire(FireMode.Misfire));
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
            }
            yield return null;
        }

        //Effects/Actions to play before the projectile is spawned
        public void PreFireEffects()
        {
            if (MuzzleFlash!=null) MuzzleFlash.Play();
            if ((Animations != null) && (!string.IsNullOrEmpty(module.fireAnim)))
            {
                // Last Shot
                if (remaingingAmmo == 1)
                {
                    Animations.Play(module.emptyAnim);
                    isEmpty = true;
                }
                else
                {
                    Animations.Play(module.fireAnim);
                }
            }

             
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
using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;

/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 06/15/2020
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
        private Transform muzzlePoint;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private ParticleSystem MuzzleFlash;
        private Animator Animations;
        private Handle gunGrip;
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

            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.definition.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.definition.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.definition.GetCustomReference(module.animatorRef).GetComponent<Animator>();

            //Override SFX volume from JSON
            if (fireSound != null) fireSound.volume = module.soundVolume;

            //var fireModeEnums = Enum.GetValues(typeof(FireMode));
            fireModeSelection = (FireMode)fireModeEnums.GetValue(module.fireMode);

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
            if (module.allowCycleFireMode && action == Interactable.Action.AlternateUseStart)
            {
                CycleFireMode();
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
            if (switchSound != null) switchSound.Play();
        }

        public void Fire()
        {
            PreFireEffects();
            SpawnProjectile(module.projectileID, GetCurrentSpellChargeID());
            ApplyRecoil();
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
                Fire();
                yield return new WaitForSeconds(fireDelay);
            }

            else if (fireSelector == FireMode.Burst)
            {
                for (int i = 0; i < burstNumber; i++)
                {
                    Fire();
                    yield return new WaitForSeconds(fireDelay);
                }
            }

            else if (fireSelector == FireMode.Auto)
            {
                while (triggerPressed) //triggerPressed is handled by OnHeldAction() events
                {
                    Fire();
                    yield return new WaitForSeconds(fireDelay);
                }
            }
            yield return null;
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

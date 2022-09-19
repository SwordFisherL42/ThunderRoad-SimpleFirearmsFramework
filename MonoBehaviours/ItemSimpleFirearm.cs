using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static SimpleBallistics.FrameworkCore;


/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * 
 */

namespace SimpleBallistics
{
    public class ItemSimpleFirearm : MonoBehaviour
    {
        // ThunderRoad references  //
        Item item;
        ItemModuleMagicFirearm module;
        Handle gunGrip;
        //  Unity references  //
        Animator Animations;
        Transform muzzlePoint;
        Transform npcRayCastPoint;
        ParticleSystem MuzzleFlash;
        ParticleSystem earlyMuzzleFlash;
        AudioSource fireSound;
        AudioSource emptySound;
        AudioSource switchSound;
        AudioSource reloadSound;
        AudioSource earlyFireSound;
        readonly string defaultHandleName = "Handle";
        //  Weapon logic references  //
        FireMode fireModeSelection;
        List<int> allowedFireModes;
        int remaingingAmmo;
        bool infAmmo = false;
        bool isEmpty = false;
        bool triggerPressed;
        bool gunGripHeldLeft;
        bool gunGripHeldRight;
        bool isFiring;
        bool useRaycast;
        float rayCastMaxDist;
        //  NPC control logic  //
        Creature thisNPC;
        BrainData thisNPCBrain;
        BrainModuleBow BrainBow;
        BrainModuleMelee BrainMelee;
        BrainModuleDefense BrainParry;
        float npcShootDelay;

        void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagicFirearm>();
            // Prioritize local settings, then fetch global settings //
            useRaycast = module.useHitscan ? true : Modules.LevelModuleBulletPierce.local.useHitscan;
            rayCastMaxDist = module.useHitscan ? module.hitscanMaxDistance : Modules.LevelModuleBulletPierce.local.hitscanMaxDistance;
            if (rayCastMaxDist <= 0f) rayCastMaxDist = Mathf.Infinity;

            try
            {
                if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
                else muzzlePoint = item.transform;
            }
            catch
            {
                Debug.LogError(string.Format("[SimpleFirearmsFramework] ERROR: '\"muzzlePositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzlePositionRef));
                muzzlePoint = item.transform;
            }
            //  Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.npcRaycastPositionRef)) npcRayCastPoint = item.GetCustomReference(module.npcRaycastPositionRef);
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>();
            if (!string.IsNullOrEmpty(module.earlyFireSoundRef)) earlyFireSound = item.GetCustomReference(module.earlyFireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.earlyMuzzleFlashRef)) earlyMuzzleFlash = item.GetCustomReference(module.earlyMuzzleFlashRef).GetComponent<ParticleSystem>();
            if (module.verbose_logging)
            {
                DebugMsg setupDebugger = new DebugMsg(module);
                setupDebugger.PrintDebugMessages();
            }
            if (npcRayCastPoint == null) { npcRayCastPoint = muzzlePoint; }
            // Setup ammo tracking
            if (module.ammoCapacity > 0) remaingingAmmo = module.ammoCapacity;
            else infAmmo = true;
            // Override SFX volume from JSON
            if ((fireSound != null) && (module.soundVolume > 0.0f) && (module.soundVolume <= 1.0f))
                fireSound.volume = module.soundVolume;
            if (module.loopedFireSound) fireSound.loop = true;
            // Get firemode based on numeric index of the enum
            fireModeSelection = (FireMode)fireModeEnums.GetValue(module.fireMode);
            if (module.allowedFireModes != null) allowedFireModes = new List<int>(module.allowedFireModes);
            // Handle interaction events //
            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip == null)
            {   // If not defined, get the first handle named "Handle", and if still not found try to get the first object with a Handle component
                gunGrip = item.transform.Find(defaultHandleName).GetComponent<Handle>();
                if (gunGrip == null) gunGrip = item.GetComponentInChildren<Handle>();
            }
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }
        }

        void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (!handle.Equals(gunGrip)) return;
            if (action == Interactable.Action.UseStart)
            {
                if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;
                triggerPressed = true;
                if (module.isFlintlock)
                    StartCoroutine(FlintlockLinkedFire());
                else if (!isFiring)
                    StartCoroutine(
                        GeneralFire(
                            TrackedFire,
                            TriggerIsPressed, 
                            fireModeSelection, 
                            module.fireRate, 
                            module.burstNumber, 
                            emptySound, 
                            SetFiringFlag));
            }
            if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
                triggerPressed = false;  // Stop Firing.
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;
                if (module.allowCycleFireMode && !isEmpty)
                {
                    if (emptySound != null) emptySound.Play();
                    fireModeSelection = CycleFireMode(fireModeSelection, allowedFireModes);
                }
                else if (isEmpty)
                    ReloadWeapon();
            }
        }

        void OnMainGripGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
            if (!gunGripHeldLeft && !gunGripHeldRight)
            {
                if (isEmpty)
                    ReloadWeapon();
                thisNPC = interactor.ragdoll.creature;
                thisNPCBrain = thisNPC.brain.instance;
                BrainBow = thisNPCBrain.GetModule<BrainModuleBow>();
                BrainMelee = thisNPCBrain.GetModule<BrainModuleMelee>();
                BrainParry = thisNPCBrain.GetModule<BrainModuleDefense>();
                thisNPC.brain.currentTarget = Player.local.creature;
                thisNPC.brain.isDefending = true;
                BrainMelee.meleeEnabled = module.npcMeleeEnableOverride;
            }
        }

        void OnMainGripUnGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
            if (thisNPC != null)
            {
                thisNPC = null;
                thisNPCBrain = null;
                BrainBow = null;
                BrainMelee = null;
                BrainParry = null;
            }
        }

        void LateUpdate()
        {
            if (module.loopedFireSound)
            {
                bool t = TriggerIsPressed();
                if (t && !fireSound.isPlaying)
                {
                    fireSound.Play();
                }
                else if ((!t && fireSound.isPlaying)||(isEmpty)) fireSound.Stop();
            }
            if (BrainParry != null)
            {
                if (thisNPC.brain.currentTarget != null) {
                    BrainParry.StartDefense();
                    if (!module.npcMeleeEnableOverride)
                    {
                        BrainMelee.meleeEnabled = Vector3.Distance(item.rb.position, thisNPC.brain.currentTarget.transform.position) <= module.npcMeleeEnableDistance;
                    }
                }
            }
            if (npcShootDelay > 0) npcShootDelay -= Time.deltaTime;
            if (npcShootDelay <= 0) { NPCFire(); }
        }

        void ReloadWeapon()
        {
            if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;
            reloadSound?.Play();
            Animate(Animations, module.reloadAnim);
            remaingingAmmo = module.ammoCapacity;
            isEmpty = false;
        }

        void SetFiringFlag(bool status) { isFiring = status; }

        bool TriggerIsPressed() { return triggerPressed; }

        bool TrackedFire()
        {   // Returns 'true' if Fire was successful.
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                Fire();
                remaingingAmmo--;
                return true;
            }
            return false;
        }

        void NPCFire()
        {
            if (thisNPC != null && thisNPCBrain != null && thisNPC.brain.currentTarget != null)
            {
                var npcAimAngle = NpcAimingAngle(BrainBow, npcRayCastPoint.TransformDirection(Vector3.forward), module.npcDistanceToFire);
                if (Physics.Raycast(npcRayCastPoint.position, npcAimAngle, out RaycastHit hit, module.npcDetectionRadius))
                {
                    Creature target = null;
                    if (hit.collider.transform.root.name.Contains("Player") || hit.collider.transform.root.name.Contains("Pool_Human"))
                        target = hit.collider.transform.root.GetComponentInChildren<Creature>();
                    if (target != null && thisNPC != target
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Passive
                        && target.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored
                        && (thisNPC.faction.attackBehaviour == GameData.Faction.AttackBehaviour.Agressive || thisNPC.factionId != target.factionId))
                    {
                        Fire(true);
                        DamageCreatureCustom(target, module.npcDamageToPlayer, hit.point);
                        npcShootDelay = UnityEngine.Random.Range(BrainBow.minMaxTimeBetweenAttack.x, BrainBow.minMaxTimeBetweenAttack.y);
                    }
                }
            }
        }

        void PreFireEffects()
        {
            MuzzleFlash?.Play();
            if (remaingingAmmo == 1)
            {   // Last Shot
                Animate(Animations, module.emptyAnim);
                isEmpty = true;
            }
            else
                Animate(Animations, module.fireAnim);
            if (!module.loopedFireSound) fireSound?.Play();
        }

        void Fire(bool firedByNPC = false, bool playEffects = true)
        {
            if (playEffects) PreFireEffects();
            if (firedByNPC) return;
            if (!useRaycast || !ShootRaycastDamage(muzzlePoint, module.bulletForce, rayCastMaxDist))
            {
                ShootProjectile(this.item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult, module.pooled);
            }
            ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce, module.recoilTorques);
        }

        IEnumerator FlintlockLinkedFire()
        {
            if (!isEmpty && (infAmmo || remaingingAmmo > 0))
            {   // Fire Success
                Animate(Animations, module.fireAnim);
                if (module.waitForFireAnim)
                {
                    do yield return null;
                    while (IsAnimationPlaying(Animations, module.fireAnim));
                }

                if (remaingingAmmo == 1)
                {
                    isEmpty = true;
                }
                if (earlyMuzzleFlash != null) earlyMuzzleFlash.Play();
                if (earlyFireSound != null) earlyFireSound.Play();
                yield return new WaitForSeconds(module.flintlockDelay);
                Fire();
                remaingingAmmo--;
            }
            else
            {   // Fire Failure
                if (emptySound != null) emptySound.Play();
                yield return null;
            }
            yield return null;
        }
    }

}

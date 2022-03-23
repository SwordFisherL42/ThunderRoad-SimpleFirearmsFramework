﻿using System.Collections;
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
        //  ThunderRoad references
        Item item;
        ItemModuleMagicFirearm module;
        Handle gunGrip;
        //  Unity references
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
        //  Weapon logic references
        FireMode fireModeSelection;
        List<int> allowedFireModes;
        int remaingingAmmo;
        bool infAmmo = false;
        bool isEmpty = false;
        bool triggerPressed;
        bool gunGripHeldLeft;
        bool gunGripHeldRight;
        bool isFiring;
        //  NPC control logic
        Creature thisNPC;
        BrainData thisNPCBrain;
        BrainModuleBow BrainBow;
        BrainModuleMelee BrainMelee;
        BrainModuleParry BrainParry;
        float npcShootDelay;

        void Awake()
        {   // TODO: clean up Awake references
            item = GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagicFirearm>();
            try
            {
                if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
                else muzzlePoint = item.transform;
            }
            catch
            {
                Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"muzzlePositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzlePositionRef));
                muzzlePoint = item.transform;
            }
            //  Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"fireSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.fireSoundRef));
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"emptySoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.emptySoundRef));
            if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"reloadSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.reloadSoundRef));
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"swtichSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.swtichSoundRef));
            if (!string.IsNullOrEmpty(module.npcRaycastPositionRef)) npcRayCastPoint = item.GetCustomReference(module.npcRaycastPositionRef);
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"npcRaycastPositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.npcRaycastPositionRef));
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"muzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzleFlashRef));
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"animatorRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.animatorRef));
            if (!string.IsNullOrEmpty(module.earlyFireSoundRef)) earlyFireSound = item.GetCustomReference(module.earlyFireSoundRef).GetComponent<AudioSource>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryFireSound\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyFireSoundRef));
            if (!string.IsNullOrEmpty(module.earlyMuzzleFlashRef)) earlyMuzzleFlash = item.GetCustomReference(module.earlyMuzzleFlashRef).GetComponent<ParticleSystem>();
            else if (module.verbose_logging) Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryMuzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyMuzzleFlashRef));
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
            // Handle interaction events
            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip == null)
            {   // If not defined, get the first handle named "Handle", and if still not found try to get the first object with a Handle component
                gunGrip = item.transform.Find("Handle").GetComponent<Handle>();
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
                BrainParry = thisNPCBrain.GetModule<BrainModuleParry>();
                thisNPC.brain.currentTarget = Player.local.creature;
                thisNPC.brain.isParrying = true;
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
                    BrainParry.StartParry(thisNPC.brain.currentTarget);
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
                        npcShootDelay = UnityEngine.Random.Range(BrainBow.bowAimMinMaxDelay.x, BrainBow.bowAimMinMaxDelay.y);
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
            ShootProjectile(this.item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult);
            ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce, module.recoilTorques);
        }

        IEnumerator FlintlockLinkedFire()
        { // TODO: Abstract FlintlockLinkedFire into FrameworkCore
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
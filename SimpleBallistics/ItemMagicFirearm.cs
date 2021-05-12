using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static SimpleBallistics.FirearmFunctions;


/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * 
 */

namespace SimpleBallistics
{
    public class ItemMagicFirearm : MonoBehaviour
    {
        //  ThunderRoad references
        protected Item item;
        protected ItemModuleMagicFirearm module;
        private Handle gunGrip;
        //  Unity references
        private Animator Animations;
        private Transform muzzlePoint;
        private Transform npcRayCastPoint;
        private ParticleSystem MuzzleFlash;
        private ParticleSystem earlyMuzzleFlash;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private AudioSource reloadSound;
        private AudioSource earlyFireSound;
        //  Weapon logic references
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;
        private int remaingingAmmo;
        private bool infAmmo = false;
        private bool isEmpty = false;
        private bool triggerPressed;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;
        public bool isFiring;
        public bool currentlySpawningProjectile;
        //  NPC control logic
        Creature thisNPC;
        BrainHuman thisNPCBrain;
        float npcShootDelay;
        bool npcPrevMeleeEnabled;
        float npcPrevMeleeDistMult;
        float npcPrevParryDetectionRadius;
        float npcPrevParryMaxDist;

        public void IgnoreProjectile(Item i, bool ignore=true) {
            foreach (ColliderGroup colliderGroup in this.item.colliderGroups)
            {
                foreach (Collider collider in colliderGroup.colliders)
                {
                    foreach (ColliderGroup colliderGroupProjectile in i.colliderGroups) {
                        foreach (Collider colliderProjectile in colliderGroupProjectile.colliders) {
                            Physics.IgnoreCollision(collider, colliderProjectile, ignore);
                        }
                    }
                }
            }
        }

        public void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagicFirearm>();
            try
            {
                if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
                else muzzlePoint = item.transform;
            }
            catch
            {
                Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"muzzlePositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzlePositionRef));
                muzzlePoint = item.transform;
            }

            //  Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)

            try { if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"fireSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.fireSoundRef)); }
            
            try { if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"emptySoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.emptySoundRef)); }

            try { if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"reloadSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.reloadSoundRef)); }

            try { if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"swtichSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.swtichSoundRef)); }

            try { if (!string.IsNullOrEmpty(module.npcRaycastPositionRef)) npcRayCastPoint = item.GetCustomReference(module.npcRaycastPositionRef); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"npcRaycastPositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.npcRaycastPositionRef)); }

            try { if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"muzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzleFlashRef)); }

            try { if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"animatorRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.animatorRef)); }

            try { if (!string.IsNullOrEmpty(module.earlyFireSoundRef)) earlyFireSound = item.GetCustomReference(module.earlyFireSoundRef).GetComponent<AudioSource>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"secondaryFireSound\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyFireSoundRef)); }

            try { if (!string.IsNullOrEmpty(module.earlyMuzzleFlashRef)) earlyMuzzleFlash = item.GetCustomReference(module.earlyMuzzleFlashRef).GetComponent<ParticleSystem>(); }
            catch { Debug.LogError(string.Format("[Fisher-SimpleBallistics] Exception: '\"secondaryMuzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyMuzzleFlashRef)); }

            if (npcRayCastPoint == null) { npcRayCastPoint = muzzlePoint; }

            // Setup ammo tracking 
            if (module.ammoCapacity > 0)
            {
                remaingingAmmo = (int)module.ammoCapacity;
            }
            else
            {
                infAmmo = true;
            }

            // Override SFX volume from JSON
            if ((module.soundVolume > 0.0f) && (module.soundVolume <= 1.0f))
            {
                if (fireSound != null)
                {
                    fireSound.volume = module.soundVolume;
                }
            }

            if (module.loopedFireSound)
            {
                fireSound.loop = true;
            }

            // Get firemode based on numeric index of the enum
            fireModeSelection = (FireMode)fireModeEnums.GetValue(module.fireMode);
            if (module.allowedFireModes != null)
            {
                allowedFireModes = new List<int>(module.allowedFireModes);
            }
            // Handle interaction events
            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip == null)
            {
                // If not defined, get the first handle named "Handle", and if still not found try to get the first object with a Handle component
                gunGrip = item.transform.Find("Handle").GetComponent<Handle>();
                if (gunGrip == null) gunGrip = item.GetComponentInChildren<Handle>();
            }
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(gunGrip))
            {
                if (action == Interactable.Action.UseStart)
                {
                    if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

                    triggerPressed = true;
                    if (module.isFlintlock)
                    {
                        StartCoroutine(FlintlockLinkedFire());
                    }
                    else if (!isFiring) StartCoroutine(GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag, ProjectileIsSpawning));
                }
                if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
                {
                    // Stop Firing.
                    triggerPressed = false;
                }
                if (action == Interactable.Action.AlternateUseStart)
                {
                    if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

                    if (module.allowCycleFireMode && !isEmpty)
                    {
                        if (emptySound != null) emptySound.Play();
                        fireModeSelection = FirearmFunctions.CycleFireMode(fireModeSelection, allowedFireModes);
                        //SetFireSelectionAnimator(Animations, fireModeSelection);
                    }
                    else if (isEmpty)
                    {
                        // Reload the weapon
                        ReloadWeapon();
                    }

                }
            }
        }

        public void OnMainGripGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

            if (!gunGripHeldLeft && !gunGripHeldRight)
            {
                if (isEmpty)
                {
                    ReloadWeapon();
                }
                
                thisNPC = interactor.ragdoll.creature;
                thisNPCBrain = (BrainHuman) thisNPC.brain.instance;
                npcPrevMeleeEnabled = thisNPCBrain.meleeEnabled;
                if (npcPrevMeleeEnabled)
                {
                    npcPrevMeleeDistMult = thisNPCBrain.meleeMax;
                    npcPrevParryDetectionRadius = thisNPCBrain.parryDetectionRadius;
                    npcPrevParryMaxDist = thisNPCBrain.parryMaxDistance;
                    thisNPCBrain.meleeEnabled = module.npcMeleeEnableFlag;
                    if (!module.npcMeleeEnableFlag)
                    {
                        thisNPCBrain.meleeDistMult = thisNPCBrain.bowDist * module.npcDistanceToFire;
                        thisNPCBrain.parryDetectionRadius = thisNPCBrain.bowDist * module.npcDistanceToFire;
                        thisNPCBrain.parryMaxDistance = thisNPCBrain.bowDist * module.npcDistanceToFire;
                    }
                }
            }
        }

        public void OnMainGripUnGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;

            if (thisNPC != null)
            {
                if (npcPrevMeleeEnabled)
                {
                    thisNPCBrain.meleeEnabled = npcPrevMeleeEnabled;
                    thisNPCBrain.meleeDistMult = npcPrevMeleeDistMult;
                    thisNPCBrain.parryMaxDistance = npcPrevParryMaxDist;
                }

                thisNPC = null;
            }

        }

        public void LateUpdate()
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
            if (npcShootDelay > 0) npcShootDelay -= Time.deltaTime;
            if (npcShootDelay <= 0) { NPCshoot(); }
        }

        private void ReloadWeapon()
        {
            if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

            if (reloadSound != null) reloadSound.Play();

            Animate(Animations, module.reloadAnim);

            remaingingAmmo = module.ammoCapacity;
            isEmpty = false;
        }

        public void SetFiringFlag(bool status)
        {
            isFiring = status;
        }

        private void NPCshoot()
        {
            if (thisNPC != null && thisNPCBrain != null && thisNPCBrain.targetCreature != null)
            {
                if (!module.npcMeleeEnableFlag)
                {
                    thisNPCBrain.meleeEnabled = Vector3.Distance(item.rb.position, thisNPCBrain.targetCreature.transform.position) <= (gunGrip.reach + 3f);
                }
                var npcAimAngle = NpcAimingAngle(thisNPCBrain, npcRayCastPoint.TransformDirection(Vector3.forward), module.npcDistanceToFire);

                if (Physics.Raycast(npcRayCastPoint.position, npcAimAngle, out RaycastHit hit, thisNPCBrain.detectionRadius))
                {
                    Creature target = null;
                    target = hit.collider.transform.root.GetComponent<Creature>();
                    if (target != null && thisNPC != target
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Passive
                        && target.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored && (thisNPC.faction.attackBehaviour == GameData.Faction.AttackBehaviour.Agressive || thisNPC.factionId != target.factionId))
                    {
                        Fire(true);
                        FirearmFunctions.DamageCreatureCustom(target, module.npcDamageToPlayer, hit.point);
                        //npcShootDelay = UnityEngine.Random.Range(thisNPCBrain.bowAimMinMaxDelay.x, thisNPCBrain.bowAimMinMaxDelay.y) * ((thisNPCBrain.bowDist / module.npcDistanceToFire + hit.distance / module.npcDistanceToFire) / thisNPCBrain.bowDist);
                        npcShootDelay = UnityEngine.Random.Range(thisNPCBrain.bowAimMinMaxDelay.x, thisNPCBrain.bowAimMinMaxDelay.y);
                    }
                }
            }
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public bool ProjectileIsSpawning() { return currentlySpawningProjectile; }

        public void PreFireEffects()
        {
            if (MuzzleFlash != null) MuzzleFlash.Play();
            //  Last Shot
            if (remaingingAmmo == 1)
            {
                Animate(Animations, module.emptyAnim);
                isEmpty = true;
            }
            else
            {
                Animate(Animations, module.fireAnim);
            }
            if (!module.loopedFireSound && fireSound != null) fireSound.Play();
        }

        private void Fire(bool firedByNPC = false, bool playEffects = true)
        {
            if (playEffects) PreFireEffects();
            if (firedByNPC) return;

            //ShootProjectile(item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult);

            ItemData spawnedItemData = Catalog.GetData<ItemData>(module.projectileID, true);
            String imbueSpell = GetItemSpellChargeID(item);

            if (spawnedItemData == null) return;
            currentlySpawningProjectile = true;
            spawnedItemData.SpawnAsync(i =>
            {
                // Debug.Log("[Fisher-Firearms] Time: " + Time.time + " Spawning projectile: " + i.name);
                try
                {
                    i.Throw(1f, Item.FlyDetection.Forced);
                    item.IgnoreObjectCollision(i);
                    i.IgnoreObjectCollision(item);
                    i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                    IgnoreProjectile(i, true);
                    i.transform.position = muzzlePoint.position;
                    i.transform.rotation = Quaternion.Euler(muzzlePoint.rotation.eulerAngles);
                    i.rb.velocity = item.rb.velocity;
                    i.rb.AddForce(i.rb.transform.forward * 1000.0f * module.bulletForce);

                    ItemSimpleProjectile projectileController = i.gameObject.GetComponent<ItemSimpleProjectile>();
                    if (projectileController != null) projectileController.SetShooterItem(this.item);
                    
                    if (!String.IsNullOrEmpty(imbueSpell))
                    {
                        //  Set imbue charge on projectile using ItemProjectileSimple subclass
                        if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                    }
                    ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
                    currentlySpawningProjectile = false;
                }
                catch
                {
                    Debug.Log("[Fisher-Firearms] EXCEPTION IN SPAWNING ");
                }
            },
            Vector3.zero,
            Quaternion.Euler(Vector3.zero),
            null,
            false);
        }

        public bool TrackedFlintlockFire()
        {
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                if (MuzzleFlash != null) MuzzleFlash.Play();
                if (fireSound != null) fireSound.Play();
                if (remaingingAmmo == 1)
                {
                    isEmpty = true;
                }
                Fire(false, false);
                remaingingAmmo--;
                return true;
            }
            return false;
        }

        public IEnumerator FlintlockLinkedFire()
        {
            // Fire Success
            if (!isEmpty && (infAmmo || remaingingAmmo > 0))
            {
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
            // Fire Failure
            else
            {
                if (emptySound != null) emptySound.Play();
                yield return null;
            }

            yield return null;

        }

        private IEnumerator FlintlockFireDelay(bool waitForFireAnim, float secondaryDelay)
        {

            yield return new WaitForSeconds(secondaryDelay);
        }

        public bool TrackedFire()
        {
            // Returns 'true' if Fire was successful.
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                Fire();
                remaingingAmmo--;
                return true;
            }
            return false;
        }

    }
}

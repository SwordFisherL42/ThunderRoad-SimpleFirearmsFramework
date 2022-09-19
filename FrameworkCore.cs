using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace SimpleBallistics
{
    public delegate bool TrackFiredDelegate();

    public delegate bool TriggerPressedDelegate();

    public delegate void IsFiringDelegate(bool status);

    public class FrameworkCore
    {
        public enum WeaponType
        {
            TestWeapon = 8,
            AutoMag = 0,
            SemiAuto = 1,
            Shotgun = 2,
            BoltAction = 3,
            Revolver = 4,
            Sniper = 5,
            HighYield = 6,
            Energy = 7
        }

        public enum AmmoType
        {
            Generic = 0,
            Magazine = 1,
            AmmoLoader = 2,
            SemiAuto = 3,
            ShotgunShell = 4,
            Revolver = 5,
            Battery = 6,
            Sniper = 7,
            Explosive = 8
        }

        public enum ProjectileType
        {
            Pierce = 1,
            Explosive = 2,
            Energy = 3,
            Blunt = 4,
            HitScan = 5,
            Sniper = 6
        }

        public enum AttachmentType
        {
            Flashlight = 1,
            Laser = 2,
            GrenadeLauncher = 3,
        }

        public enum FireMode
        {
            Misfire = 0,
            Single = 1,
            Burst = 2,
            Auto = 3
        }

        public static Array weaponTypeEnums = Enum.GetValues(typeof(WeaponType));
        public static Array ammoTypeEnums = Enum.GetValues(typeof(AmmoType));
        public static Array projectileTypeEnums = Enum.GetValues(typeof(ProjectileType));
        public static Array attachmentTypeEnums = Enum.GetValues(typeof(AttachmentType));
        public static Array fireModeEnums = Enum.GetValues(typeof(FireMode));

        static readonly string effectID1 = "HitBladeOnFlesh";
        static readonly string effectID2 = "PenetrationDeepFlesh";
        static readonly string effectID3 = "HitBladeDecalFlesh";
        static readonly string customEffectID = Modules.LevelModuleBulletPierce.local.customEffectID;
        static readonly LayerMask raycastMask = (1 << 27) | (1 << 13);
        static readonly MaterialData sourceMaterial = Catalog.GetData<MaterialData>("Metal", true);
        static readonly MaterialData targetMaterial = Catalog.GetData<MaterialData>("Flesh", true);
        static readonly Dictionary<RagdollPart.Type, float> ragdollDamageMap =  new Dictionary<RagdollPart.Type, float>(){
            { RagdollPart.Type.Head, 300f},
            { RagdollPart.Type.Neck, 50f},
            { RagdollPart.Type.Torso, 25f},
            { RagdollPart.Type.LeftWing, 15f},
            { RagdollPart.Type.RightWing, 15f},
            { RagdollPart.Type.Tail, 15f},
            { RagdollPart.Type.LeftArm, 10f},
            { RagdollPart.Type.RightArm, 10f},
            { RagdollPart.Type.LeftLeg, 10f},
            { RagdollPart.Type.RightLeg, 10f},
            { RagdollPart.Type.LeftFoot, 5},
            { RagdollPart.Type.RightFoot, 5},
            { RagdollPart.Type.LeftHand, 5},
            { RagdollPart.Type.RightHand, 5},
        };

        /// <summary>
        /// Take a given FireMode and return an increment/loop to the next enum value
        /// </summary>
        /// <param name="currentSelection"></param>
        /// <returns></returns>
        public static FireMode CycleFireMode(FireMode currentSelection, List<int> allowedFireModes = null)
        {
            int selectionIndex = (int)currentSelection;
            selectionIndex++;
            if (allowedFireModes != null)
            {
                foreach (var _ in Enumerable.Range(0, fireModeEnums.Length))
                {
                    if (allowedFireModes.Contains(selectionIndex)) return (FireMode)fireModeEnums.GetValue(selectionIndex);
                    selectionIndex++;
                    if (selectionIndex >= fireModeEnums.Length) selectionIndex = 0;
                }
                return currentSelection;
            }
            else
            {
                if (selectionIndex < fireModeEnums.Length) return (FireMode)fireModeEnums.GetValue(selectionIndex);
                else return (FireMode)fireModeEnums.GetValue(0);
            }
        }

        /// <summary>
        /// Play an animation state on the specified Animation controller
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public static bool Animate(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;
            animator.Play(animationName);
            return true;
        }

        /// <summary>
        /// Checks if an animation state is currently playing on the specified Animation controller 
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public static bool IsAnimationPlaying(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;
            try
            {
                if (animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(animationName)) return true;
                else return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleFirearmsFramework][ERROR][{Time.time}] Could not check animation '{animationName}': {e.StackTrace.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// Calculate an artifcial error quantity for NPC aiming
        /// </summary>
        /// <param name="NPCBrain"></param>
        /// <param name="initial"></param>
        /// <param name="npcDistanceToFire"></param>
        /// <returns></returns>
        public static Vector3 NpcAimingAngle(BrainModuleBow NPCBrain, Vector3 initial, float npcDistanceToFire = 10.0f)
        {
            if (NPCBrain == null) return initial;
            float aimSpread = UnityEngine.Random.Range(NPCBrain.minMaxTimeToAttackFromAim.x, NPCBrain.minMaxTimeToAttackFromAim.y);
            var inaccuracyMult = 0.2f * (aimSpread / npcDistanceToFire);
            return new Vector3(
                        initial.x + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.y + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.z);
        }

        /// <summary>
        /// Apply physics forces to an item and provide haptic player feedback
        /// </summary>
        /// <param name="itemRB"></param>
        /// <param name="recoilForces"></param>
        /// <param name="recoilMult"></param>
        /// <param name="leftHandHaptic"></param>
        /// <param name="rightHandHaptic"></param>
        /// <param name="hapticForce"></param>
        /// <param name="recoilTorque"></param>
        public static void ApplyRecoil(
            Rigidbody itemRB, 
            float[] recoilForces, 
            float recoilMult = 1.0f, 
            bool leftHandHaptic = false, 
            bool rightHandHaptic = false, 
            float hapticForce = 1.0f, 
            float[] recoilTorque = null)
        {
            if (rightHandHaptic) PlayerControl.handRight.HapticShort(hapticForce);
            if (leftHandHaptic) PlayerControl.handLeft.HapticShort(hapticForce);
            if (recoilForces == null) return;
            itemRB.AddRelativeForce(new Vector3(
                UnityEngine.Random.Range(recoilForces[0], recoilForces[1]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[2], recoilForces[3]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[4], recoilForces[5]) * recoilMult));
            if (recoilTorque == null) return;
            itemRB.AddRelativeTorque(new Vector3(
                UnityEngine.Random.Range(recoilTorque[0], recoilTorque[1]) * recoilMult,
                UnityEngine.Random.Range(recoilTorque[2], recoilTorque[3]) * recoilMult,
                UnityEngine.Random.Range(recoilTorque[4], recoilTorque[5]) * recoilMult));
        }

        /// <summary>
        /// Set the physics ignore matrix between the colliders of two items
        /// </summary>
        /// <param name="itemA"></param>
        /// <param name="itemB"></param>
        /// <param name="ignore"></param>
        public static void IgnoreCollisionsBetween(Item itemA, Item itemB, bool ignore = true)
        {
            foreach (ColliderGroup colliderGroupA in itemA.colliderGroups)
                foreach (Collider colliderA in colliderGroupA.colliders)
                    foreach (ColliderGroup colliderGroupBy in itemB.colliderGroups)
                        foreach (Collider colliderB in colliderGroupBy.colliders)
                            Physics.IgnoreCollision(colliderA, colliderB, ignore);
        }

        /// <summary>
        /// Simulate a Metal-Flesh collision if a target creature is in range.
        /// </summary>
        /// <param name="spawnPoint">Raycast position and rotational reference</param>
        /// <param name="bulletForce">Simulated force to apply</param>
        /// <param name="maxDistance">Creature detection range</param>
        /// <param name="damageMultiplier">(optional) Linear scalar for damage map</param>
        /// <returns>boolean representing the RayCast outcome</returns>
        public static bool ShootRaycastDamage(Transform spawnPoint, float bulletForce, float maxDistance, float damageMultiplier = 1f)
        {
            if (Physics.Raycast(spawnPoint.position, spawnPoint.forward, out RaycastHit hit, maxDistance, raycastMask))
            {
                RagdollPart hitRDP = hit.transform.GetComponentInChildren<RagdollPart>();
                if (hitRDP == null) return false;
                Creature target = hit.transform.root.GetComponentInChildren<Creature>();
                if (target == null) return false;
                float damageApplied = ragdollDamageMap[hitRDP.type] * damageMultiplier;
                ColliderGroup ragdollColliderGroup = hitRDP.colliderGroup;
                DamageStruct newDamageStruct = new DamageStruct(DamageType.Pierce, damageApplied)
                {
                    active = true,
                    damageType = DamageType.Pierce,
                    baseDamage = damageApplied,
                    damage = damageApplied,
                    hitRagdollPart = hitRDP,
                    penetration = DamageStruct.Penetration.Hit,
                };
                CollisionInstance newCollsionInstance = new CollisionInstance()
                {
                    active = true,
                    incomplete = true,
                    contactPoint = hit.point,
                    contactNormal = hit.normal,
                    sourceMaterial = sourceMaterial,
                    targetMaterial = targetMaterial,
                    damageStruct = newDamageStruct,
                    intensity = 2.0f,
                    hasEffect = true,
                    targetColliderGroup = ragdollColliderGroup,
                    sourceColliderGroup = ragdollColliderGroup
                };
                Catalog.GetData<EffectData>(effectID1).Spawn(hitRDP.transform).Play();
                Catalog.GetData<EffectData>(effectID2).Spawn(hitRDP.transform).Play();
                Catalog.GetData<EffectData>(effectID3).Spawn(
                        hit.point,
                        Quaternion.LookRotation(hit.normal, Vector3.up),
                        hitRDP.transform,
                        newCollsionInstance,
                        true,
                        ragdollColliderGroup,
                        true
                    ).Play();
                Catalog.GetData<EffectData>(customEffectID).Spawn(
                        hit.point,
                        Quaternion.LookRotation(hit.normal, Vector3.up),
                        hitRDP.transform,
                        newCollsionInstance,
                        true,
                        ragdollColliderGroup,
                        true
                    ).Play();
                hitRDP.rb.AddRelativeForce(spawnPoint.forward * bulletForce * 10f, ForceMode.Impulse);
                target.Damage(newCollsionInstance);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Spawn an item as a projectile, applying physics forces and game states once it is instantiated.
        /// </summary>
        /// <param name="shooterItem"></param>
        /// <param name="projectileID"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="imbueSpell"></param>
        /// <param name="forceMult"></param>
        /// <param name="throwMult"></param>
        /// <param name="pooled"></param>
        public static void ShootProjectile(
            Item shooterItem, 
            string projectileID, 
            Transform spawnPoint, 
            string imbueSpell = null, 
            float forceMult = 1.0f, 
            float throwMult = 1.0f, 
            bool pooled = false)
        {
            if ((spawnPoint == null) || (String.IsNullOrEmpty(projectileID))) return;
            var projectileData = Catalog.GetData<ItemData>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError($"[SimpleFirearmsFramework][ERROR][{Time.time}] No projectile named { projectileID.ToString()}");
                return;
            }
            else
            {
                Vector3 shootLocation = new Vector3(spawnPoint.position.x, spawnPoint.position.y, spawnPoint.position.z);
                Quaternion shooterAngles = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                Vector3 shootVelocity = new Vector3(shooterItem.rb.velocity.x, shooterItem.rb.velocity.y, shooterItem.rb.velocity.z);
                projectileData.SpawnAsync(i =>
                {
                    try
                    {
                        i.Throw(1f, Item.FlyDetection.Forced);
                        shooterItem.IgnoreObjectCollision(i); // TODO: Optimize projectile collision exclusion
                        i.IgnoreObjectCollision(shooterItem);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                        IgnoreCollisionsBetween(shooterItem, i, true);
                        SimpleProjectile projectileController = i.gameObject.GetComponent<SimpleProjectile>();
                        projectileController?.SetShooterItem(shooterItem);
                        i.transform.position = shootLocation;
                        i.transform.rotation = shooterAngles;
                        i.rb.velocity = shootVelocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * forceMult);
                        if (!string.IsNullOrEmpty(imbueSpell))
                            projectileController?.AddChargeToQueue(imbueSpell);
                    }
                    catch
                    {
                        Debug.LogError($"[SimpleFirearmsFramework][ERROR][{Time.time}] Exception in Spawning {projectileID}");
                    }
                },
                shootLocation,
                Quaternion.Euler(Vector3.zero),
                null,
                pooled);
            }
        }

        /// <summary>
        /// Get the current "Spell" charged onto the given item.
        /// </summary>
        /// <param name="interactiveObject"></param>
        /// <returns></returns>
        public static string GetItemSpellChargeID(Item interactiveObject)
        {
            foreach (Imbue itemImbue in interactiveObject.imbues)
                if (itemImbue.spellCastBase != null)
                    return itemImbue.spellCastBase.id;
            return null;
        }

        /// <summary>
        /// Transfer a given `SpellCastCharge` to a given `Imbue` over an incremental period asynchonously.
        /// </summary>
        /// <param name="itemImbue"></param>
        /// <param name="activeSpell"></param>
        /// <param name="energyDelta"></param>
        /// <param name="counts"></param>
        /// <returns></returns>
        public static IEnumerator TransferDeltaEnergy(
            Imbue itemImbue, 
            SpellCastCharge activeSpell, 
            float energyDelta = 20.0f, 
            int counts = 5, 
            float step_delay = 0.1f)
        {
            for (int i = 0; i < counts; i++)
            {
                try { itemImbue.Transfer(activeSpell, energyDelta); }
                catch { }
                yield return new WaitForSeconds(step_delay);
            }
            yield return null;
        }
        /// <summary>
        /// Create custom damage and collision structs, applying them to the given creature.
        /// </summary>
        /// <param name="triggerCreature"></param>
        /// <param name="damageApplied"></param>
        /// <param name="hitPoint"></param>
        public static void DamageCreatureCustom(Creature triggerCreature, float damageApplied, Vector3 hitPoint)
        {
            try
            {
                if (triggerCreature.currentHealth <= 0) return;
                DamageStruct damageStruct = new DamageStruct(DamageType.Pierce, damageApplied);
                CollisionInstance collisionStruct = new CollisionInstance(damageStruct, sourceMaterial, targetMaterial)
                {
                    contactPoint = hitPoint
                };
                triggerCreature.Damage(collisionStruct);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleFirearmsFramework][ERROR][{Time.time}] Unable to perform custom enemy damage: {e.Message.ToString()}");
            }
        }

        /// <summary>
        /// Force firearm states to the locked to the completion of animations.
        /// </summary>
        /// <param name="Animator"></param>
        /// <param name="Animation"></param>
        /// <param name="flintlockDelay"></param>
        /// <param name="remainingAmmo"></param>
        /// <param name="TrackedFire"></param>
        /// <param name="TriggerPressed"></param>
        /// <param name="emptySoundDriver"></param>
        /// <param name="secondaryFireSound"></param>
        /// <param name="secondaryMuzzleFlash"></param>
        /// <returns></returns>
        public static IEnumerator AnimationLinkedFire(
            Animator Animator, 
            string Animation, 
            float flintlockDelay, 
            int remainingAmmo, 
            TrackFiredDelegate TrackedFire, 
            TriggerPressedDelegate TriggerPressed, 
            AudioSource emptySoundDriver = null, 
            AudioSource secondaryFireSound = null, 
            ParticleSystem secondaryMuzzleFlash = null)
        {
            if (remainingAmmo >= 1)
            {
                Animate(Animator, Animation);
                do yield return null;
                while (IsAnimationPlaying(Animator, Animation));
            }
            if (TrackedFire())
            {   // Fire Success
                yield return new WaitForSeconds(flintlockDelay);
                secondaryFireSound?.Play();
                secondaryMuzzleFlash?.Play();
            }
            else
            {   // Fire Failure
                emptySoundDriver?.Play();
                yield return null;
            }
            yield return null;
        }

        /// <summary>
        /// Top level method for activation of firearms. Behaviour and states are determined by the given parameters.
        /// </summary>
        /// <param name="TrackedFire"></param>
        /// <param name="TriggerPressed"></param>
        /// <param name="fireSelector"></param>
        /// <param name="fireRate"></param>
        /// <param name="burstNumber"></param>
        /// <param name="emptySoundDriver"></param>
        /// <param name="WeaponIsFiring"></param>
        /// <returns></returns>
        public static IEnumerator GeneralFire(
            TrackFiredDelegate TrackedFire, 
            TriggerPressedDelegate TriggerPressed, 
            FireMode fireSelector = FireMode.Single, 
            int fireRate = 60, 
            int burstNumber = 3, 
            AudioSource emptySoundDriver = null, 
            IsFiringDelegate WeaponIsFiring = null)
        {
            WeaponIsFiring?.Invoke(true);
            float fireDelay = 60.0f / fireRate;
            switch (fireSelector)
            {
                case FireMode.Misfire:
                    emptySoundDriver?.Play();
                    yield return null;
                    break;

                case FireMode.Single:
                    if (!TrackedFire())
                    {
                        emptySoundDriver?.Play();
                        yield return null;
                    }
                    yield return new WaitForSeconds(fireDelay);
                    break;

                case FireMode.Burst:
                    for (int i = 0; i < burstNumber; i++)
                    {
                        if (!TrackedFire())
                        {
                            emptySoundDriver?.Play();
                            yield return null;
                            break;
                        }
                        yield return new WaitForSeconds(fireDelay);
                    }
                    yield return null;
                    break;

                case FireMode.Auto:
                    while (TriggerPressed())
                    {
                        if (!TrackedFire())
                        {
                            emptySoundDriver?.Play();
                            yield return null;
                            break;
                        }
                        yield return new WaitForSeconds(fireDelay);
                    }
                    break;

                default:
                    emptySoundDriver?.Play();
                    yield return null;
                    break;
            }
            WeaponIsFiring?.Invoke(false);
            yield return null;
        }

    }
}

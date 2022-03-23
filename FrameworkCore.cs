using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleBallistics
{
    public delegate bool TrackFiredDelegate();

    public delegate bool TriggerPressedDelegate();

    public delegate void IsFiringDelegate(bool status);

    public delegate bool IsSpawningDelegate();

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

        public static bool Animate(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;
            animator.Play(animationName);
            return true;
        }

        public static bool IsAnimationPlaying(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;

            try {
                if (animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(animationName)) return true;
                else return false;
            }
            catch (Exception e)
            {
                Debug.Log("[Fisher-Firearms] Could not check animation: " + e.StackTrace);
                return false;
            }

        }

        public static Vector3 NpcAimingAngle(BrainModuleBow NPCBrain, Vector3 initial, float npcDistanceToFire = 10.0f)
        {
            //BrainModuleBow aimingModule = NPCBrain.GetModule<BrainModuleBow>();
            if (NPCBrain == null) return initial;
            var inaccuracyMult = 0.2f * (NPCBrain.aimSpreadAngle / npcDistanceToFire);
            return new Vector3(
                        initial.x + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.y + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.z);
        }

        public static void ApplyRecoil(Rigidbody itemRB, float[] recoilForces, float recoilMult = 1.0f, bool leftHandHaptic = false, bool rightHandHaptic = false, float hapticForce = 1.0f, float[] recoilTorque = null)
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

        public static void IgnoreProjectile(Item item, Item i, bool ignore = true)
        {
            foreach (ColliderGroup colliderGroup in item.colliderGroups)
            {
                foreach (Collider collider in colliderGroup.colliders)
                {
                    foreach (ColliderGroup colliderGroupProjectile in i.colliderGroups)
                    {
                        foreach (Collider colliderProjectile in colliderGroupProjectile.colliders)
                        {
                            Physics.IgnoreCollision(collider, colliderProjectile, ignore);
                        }
                    }
                }
            }
        }

        public static void ShootProjectile(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell = null, float forceMult = 1.0f, float throwMult = 1.0f, bool pooled = false)
        {
            if ((spawnPoint == null) || (String.IsNullOrEmpty(projectileID))) return;
            var projectileData = Catalog.GetData<ItemData>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[SimpleFirearmsFramework][ERROR] No projectile named " + projectileID.ToString());
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
                        shooterItem.IgnoreObjectCollision(i);
                        i.IgnoreObjectCollision(shooterItem);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                        IgnoreProjectile(shooterItem, i, true);
                        SimpleProjectile projectileController = i.gameObject.GetComponent<SimpleProjectile>();
                        if (projectileController != null) projectileController.SetShooterItem(shooterItem);
                        i.transform.position = shootLocation;
                        i.transform.rotation = shooterAngles;
                        i.rb.velocity = shootVelocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * forceMult);
                        if (!String.IsNullOrEmpty(imbueSpell))
                        {
                            if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                        }
                    }
                    catch
                    {
                        Debug.Log("[SimpleFirearmsFramework] EXCEPTION IN SPAWNING ");
                    }
                },
                shootLocation,
                Quaternion.Euler(Vector3.zero),
                null,
                false);
            }
        }

        public static string GetItemSpellChargeID(Item interactiveObject)
        {
            foreach (Imbue itemImbue in interactiveObject.imbues)
            {
                if (itemImbue.spellCastBase != null)
                {
                    return itemImbue.spellCastBase.id;
                }
            }
            return null;
        }

        public static IEnumerator TransferDeltaEnergy(Imbue itemImbue, SpellCastCharge activeSpell, float energyDelta = 20.0f, int counts = 5)
        {
            for (int i = 0; i < counts; i++)
            {
                try { itemImbue.Transfer(activeSpell, energyDelta); }
                catch { }
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;
        }

        public static void DamageCreatureCustom(Creature triggerCreature, float damageApplied, Vector3 hitPoint)
        {
            try
            {
                if (triggerCreature.currentHealth > 0)
                {
                    MaterialData sourceMaterial = Catalog.GetData<MaterialData>("Metal", true);
                    MaterialData targetMaterial = Catalog.GetData<MaterialData>("Flesh", true);

                    DamageStruct damageStruct = new DamageStruct(DamageType.Pierce, damageApplied)
                    {
                        //materialEffectData = daggerEffectData
                    };

                    CollisionInstance collisionStruct = new CollisionInstance(damageStruct, (MaterialData)sourceMaterial, (MaterialData)targetMaterial)
                    {
                        contactPoint = hitPoint
                    };
                    triggerCreature.Damage(collisionStruct);

                    if (collisionStruct.SpawnEffect(sourceMaterial, targetMaterial, false, out EffectInstance effectInstance))
                    {
                        effectInstance.Play();
                    }
                }
            }
            catch
            {
                //Debug.Log("[F-L42-RayCast][ERROR] Unable to damage enemy!");
            }
        }

        public static IEnumerator AnimationLinkedFire(Animator Animator, string Animation, float flintlockDelay, int remainingAmmo, TrackFiredDelegate TrackedFire, TriggerPressedDelegate TriggerPressed, IsSpawningDelegate ProjectileIsSpawning = null, AudioSource emptySoundDriver = null, AudioSource secondaryFireSound = null, ParticleSystem secondaryMuzzleFlash = null)
        {
            if (remainingAmmo >= 1)
            {
                Animate(Animator, Animation);
                do yield return null;
                while (IsAnimationPlaying(Animator, Animation));
            }

            // wait for any previous projectiles
            do yield return null;
            while (ProjectileIsSpawning());

            // Fire Success
            if (TrackedFire())
            {
                yield return new WaitForSeconds(flintlockDelay);
                if (secondaryFireSound != null) secondaryFireSound.Play();
                if (secondaryMuzzleFlash != null) secondaryMuzzleFlash.Play();
            }
            // Fire Failure
            else
            {
                if (emptySoundDriver != null) emptySoundDriver.Play();
                yield return null;
            }

            yield return null;

        }

        public static IEnumerator GeneralFire(TrackFiredDelegate TrackedFire, TriggerPressedDelegate TriggerPressed, FireMode fireSelector = FireMode.Single, int fireRate = 60, int burstNumber = 3, AudioSource emptySoundDriver = null, IsFiringDelegate WeaponIsFiring = null, IsSpawningDelegate ProjectileIsSpawning = null)
        {
            WeaponIsFiring?.Invoke(true);
            float fireDelay = 60.0f / (float)fireRate;

            if (fireSelector == FireMode.Misfire)
            {
                if (emptySoundDriver != null) emptySoundDriver.Play();
                yield return null;
            }

            else if (fireSelector == FireMode.Single)
            {
                do yield return null;
                while (ProjectileIsSpawning());

                if (!TrackedFire())
                {
                    if (emptySoundDriver != null) emptySoundDriver.Play();
                    yield return null;
                }
                yield return new WaitForSeconds(fireDelay);
            }

            else if (fireSelector == FireMode.Burst)
            {
                for (int i = 0; i < burstNumber; i++)
                {

                    do yield return null;
                    while (ProjectileIsSpawning());

                    if (!TrackedFire())
                    {
                        if (emptySoundDriver != null) emptySoundDriver.Play();
                        yield return null;
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
                yield return null;
            }

            else if (fireSelector == FireMode.Auto)
            {
                // triggerPressed is handled in OnHeldAction(), so stop firing once the trigger/weapon is released
                while (TriggerPressed())
                {
                    do yield return null;
                    while (ProjectileIsSpawning());

                    if (!TrackedFire())
                    {
                        if (emptySoundDriver != null) emptySoundDriver.Play();
                        yield return null;
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
            }
            WeaponIsFiring?.Invoke(false);
            yield return null;
        }

    }
}

using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace SimpleBallistics
{
    public delegate bool TrackFiredDelegate();

    public delegate bool TriggerPressedDelegate();

    public delegate void IsFiringDelegate(bool status);

    public class FirearmFunctions
    {
        public enum FireMode
        {
            Misfire = 0,
            Single = 1,
            Burst = 2,
            Auto = 3
        }

        public static Array fireModeEnums = Enum.GetValues(typeof(FireMode));

        public static FireMode CycleFireMode(FireMode currentSelection)
        {
            int selectionIndex = (int)currentSelection;
            selectionIndex++;
            if (selectionIndex < fireModeEnums.Length) return (FireMode)fireModeEnums.GetValue(selectionIndex);
            else return (FireMode)fireModeEnums.GetValue(0);
        }

        public static bool Animate(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;
            animator.Play(animationName);
            return true;
        }

        public static Vector3 NpcAimingAngle(BrainHuman NPCBrain, Vector3 initial, float npcDistanceToFire = 10.0f)
        {
            if (NPCBrain == null) return initial;
            var inaccuracyMult = 0.2f * (NPCBrain.aimSpreadCone / npcDistanceToFire);
            return new Vector3(
                        initial.x + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.y + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.z);
        }

        public static void ApplyRecoil(Rigidbody itemRB, float[] recoilForces, float recoilMult = 1.0f, bool leftHandHaptic = false, bool rightHandHaptic = false, float hapticForce = 1.0f)
        {
            if (rightHandHaptic) PlayerControl.handRight.HapticShort(hapticForce);
            if (leftHandHaptic) PlayerControl.handLeft.HapticShort(hapticForce);

            if (recoilForces == null) return;
            itemRB.AddRelativeForce(new Vector3(
                UnityEngine.Random.Range(recoilForces[0], recoilForces[1]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[2], recoilForces[3]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[4], recoilForces[5]) * recoilMult));
        }

        public static void ShootProjectile(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell = null, float forceMult = 1.0f, float throwMult = 1.0f, bool pooled = false)
        {
            var projectileData = Catalog.GetData<ItemPhysic>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                Item projectile = projectileData.Spawn(pooled);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                shooterItem.IgnoreObjectCollision(projectile);
                if (!String.IsNullOrEmpty(imbueSpell))
                {
                    // Set imbue charge on projectile using ItemProjectileSimple subclass
                    ItemSimpleProjectile projectileController = projectile.gameObject.GetComponent<ItemSimpleProjectile>();
                    if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                }
                // Match the Position, Rotation, & Speed of the spawner item
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                projectile.rb.velocity = shooterItem.rb.velocity;
                projectile.rb.AddForce(projectile.rb.transform.forward * 1000.0f * forceMult);
                projectile.Throw(throwMult, Item.FlyDetection.CheckAngle);
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

        public static IEnumerator GeneralFire(TrackFiredDelegate TrackedFire, TriggerPressedDelegate TriggerPressed, FireMode fireSelector = FireMode.Single, int fireRate = 60, int burstNumber = 3, AudioSource emptySoundDriver = null, IsFiringDelegate WeaponIsFiring = null)
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

/*
protected void DamageCreatureCustom(Creature triggerCreature, float damageApplied, Vector3 hitPoint)
{
    try
    {
        if (triggerCreature.health.currentHealth > 0)
        {
            Debug.Log("[F-L42-RayCast] Damaging enemy: " + triggerCreature.name);
            Debug.Log("[F-L42-RayCast] Setting MaterialData... ");
            MaterialData sourceMaterial = Catalog.GetData<MaterialData>("Metal", true); //(MaterialData)null; 
            MaterialData targetMaterial = Catalog.GetData<MaterialData>("Flesh", true); //(MaterialData)null;
            Debug.Log("[F-L42-RayCast] Fetching MaterialEffectData... ");
            MaterialEffectData daggerEffectData = Catalog.GetData<MaterialEffectData>("DaggerPierce", true);

            //Damager daggerDamager = new Damager();
            //DamagerData daggerDamagerData = Catalog.GetData<DamagerData>("DaggerPierce", true);
            //daggerDamager.Load(daggerDamagerData);
            Debug.Log("[F-L42-RayCast] Defining DamageStruct... ");
            DamageStruct damageStruct = new DamageStruct(DamageType.Pierce, damageApplied)
            {
                materialEffectData = daggerEffectData
            };
            Debug.Log("[F-L42-RayCast] Defining CollisionStruct... ");
            CollisionStruct collisionStruct = new CollisionStruct(damageStruct, (MaterialData)sourceMaterial, (MaterialData)targetMaterial)
            {
                contactPoint = hitPoint
            };
            Debug.Log("[F-L42-RayCast] Applying Damage to creature... ");
            triggerCreature.health.Damage(ref collisionStruct);
            Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

            Debug.Log("[F-L42-RayCast] SpawnEffect... ");
            if (collisionStruct.SpawnEffect(sourceMaterial, targetMaterial, false, out EffectInstance effectInstance))
            {
                effectInstance.Play();
            }
            Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

        }
    }
    catch
    {
        Debug.Log("[F-L42-RayCast][ERROR] Unable to damage enemy!");
    }
}

public void RayCastFire()
{
    Debug.Log("[F-L42-RayCastFire] Called RayCastFire ... ");
    var rayCastHit = Physics.Raycast(muzzlePoint.position, muzzlePoint.TransformDirection(Vector3.forward), out RaycastHit hit);
    if (rayCastHit)
    {
        Debug.Log("[F-L42-RayCastFire] Hit! " + hit.transform.gameObject.name);
        Debug.DrawRay(muzzlePoint.position, muzzlePoint.TransformDirection(Vector3.forward) * hit.distance, Color.red);
        if (hit.collider.attachedRigidbody != null)
        {
            Debug.Log("[F-L42-RayCastFire] Hit Attached RigidBody ");
            Debug.Log("[F-L42-RayCastFire] Force Applied to RB! ");
            hit.collider.attachedRigidbody.AddForceAtPosition(muzzlePoint.TransformDirection(Vector3.forward) * module.hitForce, hit.point);
            var targetCreature = hit.collider.attachedRigidbody.gameObject.GetComponent<Creature>();
            if (targetCreature != null)
            {
                //Creature triggerCreature = hitPart.ragdoll.creature;
                Debug.Log("[F-L42-RayCastFire] Creature Hit: " + targetCreature.name);
                if (targetCreature == Creature.player) return;
                DamageCreatureCustom(targetCreature, 20f, hit.point);
            }
        }
    }
}
*/

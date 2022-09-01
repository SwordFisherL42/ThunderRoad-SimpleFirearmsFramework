using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using ThunderRoad;
using UnityEngine;


namespace SimpleBallistics.Modules
{
    public class LevelModuleBulletPierce : LevelModule
    {
        private Harmony harmony;

        private const string harmonyPatchName = "Fisher.U11.BulletPierce";

        private const string damagerID = "FisherBulletPierce";

        public override IEnumerator OnLoadCoroutine()
        {
            try
            {
                if (!Harmony.HasAnyPatches(harmonyPatchName))
                {
                    Debug.Log($"[Harmony][{harmonyPatchName}] Loading Patches ... ");
                    this.harmony = new Harmony(harmonyPatchName);
                    this.harmony.PatchAll(Assembly.GetExecutingAssembly());
                    Debug.Log($"[Harmony][{harmonyPatchName}] Patches Loaded !!! ");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Harmony][{harmonyPatchName}] [Exception] ERROR with patches: ");
                Debug.Log(ex.StackTrace);
            }
            yield return base.OnLoadCoroutine();
        }

        [HarmonyPatch]
        private static class DamagerCheckAnglesPrefixPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Damager), "CheckAngles", new Type[] {typeof(UnityEngine.Vector3), typeof(UnityEngine.Vector3)});
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result, ref Damager __instance, UnityEngine.Vector3 vector, UnityEngine.Vector3 normal)
            {
                try
                {
                    if (!__instance.data.id.Contains(damagerID)) return true;
                    __result = true;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Damager))]
        [HarmonyPatch("TryHit")]
        private static class DamagerTryHitPrefixPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref bool __result, ref Damager __instance, CollisionInstance collisionInstance)
            {
                try
                {
                    if (!__instance.data.id.Contains(damagerID)) return true;
                    __instance.Penetrate(collisionInstance, false);
                    __result = true;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Damager))]
        [HarmonyPatch("CheckPenetration")]
        private static class DamagerCheckPenetrationPrefixPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref bool __result, ref Damager __instance, CollisionInstance collisionInstance)
            {
                try
                {
                    if (!__instance.data.id.Contains(damagerID)) return true;
                    __result = true;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

    }
}

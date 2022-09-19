using UnityEngine;


namespace SimpleBallistics
{
    public class DebugMsg
    {
        readonly ItemModuleMagicFirearm module;
        readonly string FIRESOUND_NOT_SET;
        readonly string EMPTYSOUND_NOT_SET;
        readonly string RELOADSOUND_NOT_SET;
        readonly string SWITCHSOUND_NOT_SET;
        readonly string NPCRAYCAST_NOT_SET;
        readonly string MUZZLEFLASH_NOT_SET;
        readonly string ANIMATOR_NOT_SET;
        readonly string FIRESOUND2_NOT_SET;
        readonly string MUZZLEFLASH2_NOT_SET;

        public DebugMsg(ItemModuleMagicFirearm firearmModule)
        {
            module = firearmModule;
            FIRESOUND_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"fireSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.fireSoundRef);
            EMPTYSOUND_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"emptySoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.emptySoundRef);
            RELOADSOUND_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"reloadSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.reloadSoundRef);
            SWITCHSOUND_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"swtichSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.swtichSoundRef);
            NPCRAYCAST_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"npcRaycastPositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.npcRaycastPositionRef);
            MUZZLEFLASH_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"muzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzleFlashRef);
            ANIMATOR_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"animatorRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.animatorRef);
            FIRESOUND2_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryFireSound\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyFireSoundRef);
            MUZZLEFLASH2_NOT_SET = string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryMuzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyMuzzleFlashRef);
        }

        public void PrintDebugMessages()
        {
            if (string.IsNullOrEmpty(module.fireSoundRef)) Debug.LogError(FIRESOUND_NOT_SET);
            if (string.IsNullOrEmpty(module.emptySoundRef)) Debug.LogError(EMPTYSOUND_NOT_SET);
            if (string.IsNullOrEmpty(module.reloadSoundRef)) Debug.LogError(RELOADSOUND_NOT_SET);
            if (string.IsNullOrEmpty(module.swtichSoundRef)) Debug.LogError(SWITCHSOUND_NOT_SET);
            if (string.IsNullOrEmpty(module.npcRaycastPositionRef)) Debug.LogError(NPCRAYCAST_NOT_SET);
            if (string.IsNullOrEmpty(module.muzzleFlashRef)) Debug.LogError(MUZZLEFLASH_NOT_SET);
            if (string.IsNullOrEmpty(module.animatorRef)) Debug.LogError(ANIMATOR_NOT_SET);
            if (string.IsNullOrEmpty(module.earlyFireSoundRef)) Debug.LogError(FIRESOUND2_NOT_SET);
            if (string.IsNullOrEmpty(module.earlyMuzzleFlashRef)) Debug.LogError(MUZZLEFLASH2_NOT_SET);
        }
    }
}

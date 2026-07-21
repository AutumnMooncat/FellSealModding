using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Il2CppGame.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppApEngine;
#else
using Game;
using Game.Battle;
using Game.Data;
using Game.UI;
using ApEngine;
#endif

namespace FellSealAssetLoader.Patches
{
    [HarmonyPatch]
    public static class WeaponPatches
    {
        
        [HarmonyPatch(typeof(Weapons), nameof(Weapons.IsTwoHanded))]
        public static class IsTwoHandedPatch
        {
            public static void Postfix(ref bool __result, WeaponsType type)
            {
                if (type.IsExtendedType() && type.HasRegistry(out var reg))
                {
                    __result = reg.handedness == WeaponRegistry.Handedness.TwoHanded || reg.handedness == WeaponRegistry.Handedness.TrueTwoHanded;
                }
            }
        } 
        
        [HarmonyPatch(typeof(Weapons), nameof(Weapons.IsRealTwoHanded))]
        public static class IsRealTwoHandedPatch
        {
            public static void Postfix(ref bool __result, WeaponsType type)
            {
                if (type.IsExtendedType() && type.HasRegistry(out var reg))
                {
                    __result = reg.handedness == WeaponRegistry.Handedness.TrueTwoHanded;
                }
            }
        }

        [HarmonyPatch(typeof(BattleManager.ActionInformation), nameof(BattleManager.ActionInformation.FillAttack))]
        public static class FillAttackPatch
        {
            public static void Postfix(BattleManager.ActionInformation __instance, BattleCharacter caster)
            {
                var weapon = caster.character.GetWeapon(0);
                if (weapon.HasRegistry(out var reg))
                {
                    __instance.mTargettingMinRange = reg.minimumRange;
                    __instance.mTargettingExcludeSelf = !reg.canTargetSelf;
                    __instance.mTargettingAllowSelf = reg.canTargetSelf;
                }
            }
        }

        [HarmonyPatch(typeof(BattleManager.ActionInformation), nameof(BattleManager.ActionInformation.GetAttackName))]
        public static class GetAttackNamePatch
        {
            public static void Postfix(ref string __result, BattleCharacter caster, bool allowDW)
            {
                var weapon = caster.character.GetWeapon(0);
                if (weapon.HasRegistry(out var reg) && reg.attackHash?.Invoke(caster) is string hash)
                {
                    //Melon<AssetLoaderMod>.Logger.Msg($"Replaced attack hash {__result} with {hash}");
                    __result = hash;
                }
            }
        }
    }
}
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;

#if NET6_0
using Il2CppGame;
using Il2CppGame.Data;
using Il2CppGame.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppApEngine;
#else
using Game;
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
                
            }
        } 
        
        [HarmonyPatch(typeof(Weapons), nameof(Weapons.IsRealTwoHanded))]
        public static class IsRealTwoHandedPatch
        {
            
        }
    }
}
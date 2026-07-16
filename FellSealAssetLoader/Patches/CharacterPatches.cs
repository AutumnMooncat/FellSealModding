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
    public static class CharacterPatches
    {
        public static Context<BaseCharacter> GetWeaponEquipMaskCtx;
        public static Context<BaseCharacter> GetArmorEquipMaskCtx;
        
        [AssetInit]
        public static void Init()
        {
            GetWeaponEquipMaskCtx = ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.GetWeaponEquipMask))
                .WithReleaseReturn((instance, args, result) =>
                {
                    var job = instance.GetMainJob();
                    var valid = (WeaponsType)result[0];
                    foreach (var reg in RegistryTools.WeaponRegistries)
                    {
                        if ((job.weaponsAllowed & reg.type) == reg.type)
                        {
                            valid |= reg.type;
                        }
                    }

                    result[0] = valid;
                });
            
            GetArmorEquipMaskCtx = ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.GetArmorEquipMask))
                .WithReleaseReturn((instance, args, result) =>
                {
                    var job = instance.GetMainJob();
                    var valid = (ArmorType)result[0];
                    foreach (var reg in RegistryTools.ArmorRegistries)
                    {
                        if ((job.armorsAllowed & reg.type) == reg.type)
                        {
                            valid |= reg.type;
                        }
                    }

                    result[0] = valid;
                });
        }
    }
}
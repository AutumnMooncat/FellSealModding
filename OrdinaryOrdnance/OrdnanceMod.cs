using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using OrdinaryOrdnance;

#if NET6_0
using Il2Cpp;
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Constants = Il2CppSpriteEngine.Constants;
using Il2CppSystem.Collections.Generic;
#else
using Game;
using Game.Data;
using Game.Battle;
using Constants = SpriteEngine.Constants;
using System.Collections.Generic;
#endif

[assembly: MelonInfo(typeof(OrdnanceMod), "Ordinary Ordnance", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace OrdinaryOrdnance
{
    public class OrdnanceMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Ordinary Ordnance initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static WeaponRegistry VialWeapon;
        
        [AssetInit]
        public static void Init()
        {
            VialWeapon = RegistryTools.RegisterWeaponsType("Vial", "icon-vial-group", WeaponRegistry.Handedness.OneHanded)
                .WithJobs(
                    GameConstants.Jobs.Peddler, 
                    GameConstants.Jobs.Alchemystic, 
                    GameConstants.Jobs.Anatomist, 
                    GameConstants.Jobs.PlagueDoctor,
                    GameConstants.Jobs.Druid)
                .WithSounds(WeaponsType.kfMace);
        }

        [HarmonyPatch(typeof(BattleManager.ActionInformation), nameof(BattleManager.ActionInformation.GetAttackName))]
        public static class Test
        {
            public static bool Prefix(ref string __result, BattleCharacter caster, bool allowDW)
            {
                if (caster.character.GetWeapon(0).type == (int)VialWeapon.type)
                {
                    __result = "RANG-A4";
                    return false;
                }
                return true;
            }
        }
    }
}
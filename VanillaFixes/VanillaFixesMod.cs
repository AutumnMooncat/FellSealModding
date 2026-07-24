using System.Linq;
using FellSealAssetLoader;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using VanillaFixes;

#if NET6_0
using Il2CppGame.Data;
#else
using Game.Data; 
#endif

[assembly: MelonInfo(typeof(VanillaFixesMod), "VanillaFixes", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]

namespace VanillaFixes
{
    public class VanillaFixesMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("VanillaFixes Melon Initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        private static readonly string[] BrokenNormalAttacks = { "KNIG-A4", "GUNN-A7", "SAMU-A8", "VAR-COR-A2" };
        
        [AssetInit]
        public static void Init()
        {
            AssetLoaderEvents.DatabaseInit += db =>
            {
                Melon<VanillaFixesMod>.Logger.Msg("Fixing missing kWeapon from attacks that count as normal");
                foreach (var ability in db.GetAbilityDb().allAbilities)
                {
                    if (BrokenNormalAttacks.Contains(ability.abilityName))
                    {
                        ability.element = Elements.kWeapon;
                    }
                }
            };
        }
    }
}
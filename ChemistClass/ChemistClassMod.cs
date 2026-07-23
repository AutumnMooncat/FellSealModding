using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using ChemistClass;

[assembly: MelonInfo(typeof(ChemistClassMod), "Chemist Class", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]

namespace ChemistClass
{
    public class ChemistClassMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Chemist Class Melon Initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        [AssetInit]
        public static void Init()
        {
            Melon<ChemistClassMod>.Logger.Msg("Chemist Class Asset Initializing");
        }
    }
}
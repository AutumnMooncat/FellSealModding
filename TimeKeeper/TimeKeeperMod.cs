using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using TimeKeeper;

[assembly: MelonInfo(typeof(TimeKeeperMod), "TimeKeeper Mod", "0.0.1", "")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]

namespace TimeKeeper
{
    public class TimeKeeperMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("TimeKeeper initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        [AssetInit]
        public static void Init()
        {
        }
    }
}
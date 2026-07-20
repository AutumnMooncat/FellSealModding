using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using ModTemplate;

[assembly: MelonInfo(typeof(ModTemplateMod), "ModTemplate Mod", "0.0.1", "AuthorName")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace ModTemplate
{
    public class ModTemplateMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ModTemplate initializing");
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
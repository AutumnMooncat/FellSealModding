using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using MelonLoader;

[assembly: MelonInfo(typeof(AssetLoaderMod), "Fell Seal Asset Loader", "0.0.1", "Autumn Mooncat")]
namespace FellSealAssetLoader
{
    public class AssetLoaderMod : MelonMod
    {
        internal static readonly Dictionary<object, Dictionary<string, HashSet<string>>> CustomAttributes = new Dictionary<object, Dictionary<string, HashSet<string>>>();
        internal static readonly ConditionalWeakTable<object, Dictionary<string, object>> CustomFields =
            new ConditionalWeakTable<object, Dictionary<string, object>>();
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Asset Loader Melon Initializing");
            AttributeProcessor.Run<AssetInitAttribute>(LoggerInstance, HarmonyInstance);
        }
        
        public override void OnLateInitializeMelon()
        {
            AttributeProcessor.Run<AssetLateInitAttribute>(LoggerInstance, HarmonyInstance);
        }

        public override void OnDeinitializeMelon()
        {
            AttributeProcessor.Run<AssetDeinitAttribute>(LoggerInstance, HarmonyInstance);
        }
    }
}
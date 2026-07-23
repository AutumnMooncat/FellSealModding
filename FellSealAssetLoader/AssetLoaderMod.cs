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
        internal static readonly Dictionary<object, Dictionary<string, string>> CustomAttributes = new Dictionary<object, Dictionary<string, string>>();
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
        
        public static T RequestExtendedEnum<T>(string name) where T : struct, Enum
        {
            return EnumTools.RequestExtendedEnum<T>(name);
        }

        public static Context<T> RequestContext<T>(string methodName, params Type[] paramtypez) where T : class
        {
            return ContextTools.RequestContext<T>(methodName, paramtypez);
        }
        
        public static Context<T> RequestLateContext<T>(string methodName, params Type[] paramtypez) where T : class
        {
            return ContextTools.RequestLateContext<T>(methodName, paramtypez);
        }
    }
}
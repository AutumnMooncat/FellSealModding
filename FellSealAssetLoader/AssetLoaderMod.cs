using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FellSealAssetLoader;
using FellSealAssetLoader.Loaders;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using MelonLoader;

[assembly: MelonInfo(typeof(AssetLoaderMod), "Fell Seal Asset Loader", "0.0.1", "Autumn Mooncat")]
namespace FellSealAssetLoader
{
    public class AssetLoaderMod : MelonMod
    {
        internal static readonly Dictionary<object, Dictionary<string, object>> CustomAttributes = new Dictionary<object, Dictionary<string, object>>();
        internal static readonly ConditionalWeakTable<object, Dictionary<string, object>> CustomFields =
            new ConditionalWeakTable<object, Dictionary<string, object>>();
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Asset Loader initialized");
            ImageLoader.OnInit(LoggerInstance);
        }

        public override void OnDeinitializeMelon()
        {
            ImageLoader.OnDeinit(LoggerInstance);
        }

        public override void OnLateInitializeMelon()
        {
            HookTools.Init(HarmonyInstance);
        }
        
        public static T RequestExtendedEnum<T>(string name) where T : Enum
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
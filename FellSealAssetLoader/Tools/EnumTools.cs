using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2Cpp;
using Object = Il2CppSystem.Object;
#else
#endif

// Enum Ext (Wip, fully custom)
namespace FellSealAssetLoader.Tools
{
    [HarmonyPatch]
    public class EnumTools
    {
        private static readonly Dictionary<Type, Dictionary<string, Enum>> Extensions = new Dictionary<Type, Dictionary<string, Enum>>();
        
        public static T RequestExtendedEnum<T>(string name) where T : Enum
        {
            var type = typeof(T);
            if (!Extensions.ContainsKey(type))
            {
                Extensions[type] = new Dictionary<string, Enum>();
            }
            if (Extensions[type].TryGetValue(name, out var val))
            {
                return (T) val;
            }
            
            var limit = type.GetEnumValues().Length + Extensions[type].Count;
            var flags = Attribute.IsDefined(type, typeof(FlagsAttribute));
            if (flags)
            {
                Extensions[type][name] = (T) Enum.ToObject(type, 1<<limit);
            }
            else
            {
                Extensions[type][name] = (T) Enum.ToObject(type, limit);
            }
            Melon<AssetLoaderMod>.Logger.Msg($"Created Extended Enum {name} for {type} -> {Extensions[type][name]}");
            return (T) Extensions[type][name];
        }
    }
}
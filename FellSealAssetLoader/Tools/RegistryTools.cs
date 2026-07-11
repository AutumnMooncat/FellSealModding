using System.Collections.Generic;
using System.Reflection;
using FellSealAssetLoader.Patches;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2CppGame.Data;
#else
using Game.Data;
#endif

namespace FellSealAssetLoader.Tools
{
    [HarmonyPatch]
    public class RegistryTools
    {
        public static readonly List<WeaponRegistry> WeaponRegistries = new List<WeaponRegistry>();
        public static readonly List<ArmorRegistry> ArmorRegistries = new List<ArmorRegistry>();
        
        public static WeaponRegistry RegisterWeaponsType(string name, string sprite, WeaponRegistry.Handedness handedness)
        {
            Melon<AssetLoaderMod>.Logger.MsgPastel($"Creating WeaponRegistry {name}");
            MenuPatches.RebuildGearLists = true;
            var caller = Assembly.GetCallingAssembly();
            var info = caller.GetCustomAttribute(typeof(MelonInfoAttribute)) as MelonInfoAttribute;
            var ext = EnumTools.RequestExtendedEnum<WeaponsType>("kf"+name);
            var reg = new WeaponRegistry(info, ext, ext.ToString(), name, sprite, handedness);
            WeaponRegistries.Add(reg);
            return reg;
        }
    }
}
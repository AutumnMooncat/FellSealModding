using System;
using System.Collections.Generic;
using System.Linq;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;

#if NET6_0
using Il2CppGame.Data;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Object = Il2CppSystem.Object;
#else
using Game.Data;
#endif

namespace FellSealAssetLoader
{
    public static class AssetLoaderExtensions
    {
        public static T EditThis<T>(this T thing, Action<T> edit)
        {
            edit(thing);
            return thing;
        }
        
        public static bool GetCustomAttributes(this object o, out Dictionary<string, string> attr)
        {
            #if NET6_0
            if (o is Object obj)
            {
                foreach (var key in AssetLoaderMod.CustomAttributes.Keys)
                {
                    var maybe = key as Object;
                    if (obj.Equals(maybe))
                    {
                        attr = AssetLoaderMod.CustomAttributes[key];
                        return true;
                    }
                }
            }
            #endif
            return AssetLoaderMod.CustomAttributes.TryGetValue(o, out attr);
        }
        
        public static Dictionary<string, object> GetCustomFields(this object o)
        {
            return AssetLoaderMod.CustomFields.GetOrCreateValue(o);
        }

        public static WeaponRegistry GetRegistry(this WeaponsType type)
        {
            return RegistryTools.WeaponRegistries.FirstOrDefault(r => r.type.Equals(type));
        }

        public static ArmorRegistry GetRegistry(this ArmorType type)
        {
            return RegistryTools.ArmorRegistries.FirstOrDefault(r => r.type.Equals(type));
        }

        public static bool IsExtendedType(this Weapon wp, out WeaponsType type)
        {
            type = default;
            if (wp == null) return false;
            if (EnumTools.ExtensionBases.TryGetValue(typeof(WeaponsType), out var bases) && bases.TryGetKey(wp.type, out var key) && key is WeaponsType wt)
            {
                type = wt;
                return true;
            }
            return false;
        }
        
        public static bool IsExtendedType(this Armor ar, out ArmorType type)
        {
            type = default;
            if (ar == null) return false;
            if (EnumTools.ExtensionBases.TryGetValue(typeof(ArmorType), out var bases) && bases.TryGetKey(ar.type, out var key) && key is ArmorType at)
            {
                type = at;
                return true;
            }
            return false;
        }
        
        public static void SetCustomField<T>(this object o, string key, T data)
        {
            o.GetCustomFields()[key] = data;
        }
    
        public static bool GetCustomField<T>(this object o, string key, out T data)
        {
            if (o.GetCustomFields().TryGetValue(key, out var found) && found is T value)
            {
                data = value;
                return true;
            }

            data = default;
            return false;
        }

        public static T GetOrSetCustomField<T>(this object o, string key, T def = default)
        {
            if (o.GetCustomField(key, out T res))
            {
                return res;
            }
            o.SetCustomField(key, def);
            return def;
        }

        public static T WithCustomField<T, D>(this T o, string key, D data)
        {
            o.SetCustomField(key, data);
            return o;
        }
    
        public static T WithoutCustomField<T>(this T o, string key)
        {
            o.RemoveCustomField(key);
            return o;
        }

        public static bool RemoveCustomField(this object o, string key)
        {
            return o.GetCustomFields().Remove(key);
        }
        
        public static bool RemoveCustomField<T>(this object o, string key, out T data)
        {
            data = default;
            if (o.GetCustomFields().TryGetValue(key, out var found) && found is T value)
            {
                data = value;
            }

            return o.GetCustomFields().Remove(key);
        }
        
        public static bool TryGetKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue value, out TKey key)
        {
            key = default;
            foreach (var pair in dict)
            {
                if (pair.Value.Equals(value))
                {
                    key = pair.Key;
                    return true;
                }
            }
            return false;
        }

        public static string ToArrayString<T>(this T[] array)
        {
            return array == null ? "" : string.Join(", ", array.Select(t => t.ToString()));
        }

        #if NET6_0
        public static string ToArrayString(this Il2CppStringArray array)
        {
            return array == null ? "" : string.Join(", ", array.Select(t => t.ToString()));
        }
        #else
        public static bool IsAssignableTo(this Type thisType, Type targetType)
        {
            return (object) targetType != null && targetType.IsAssignableFrom(thisType);
        }
        #endif
    }
}
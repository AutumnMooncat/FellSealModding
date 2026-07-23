using System;
using System.Collections.Generic;
using System.Linq;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;

#if NET6_0
using Il2CppGame.Data;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
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
        
        public static bool GetCustomAttributes(this object o, out Dictionary<string, HashSet<string>> attr)
        {
            #if NET6_0
            foreach (var key in AssetLoaderMod.CustomAttributes.Keys)
            {
                if (o.Il2CppEquals(key))
                {
                    attr = AssetLoaderMod.CustomAttributes[key];
                    return true;
                }
            }
            #endif
            return AssetLoaderMod.CustomAttributes.TryGetValue(o, out attr);
        }

        public static HashSet<string> CustomEffects(this object o)
        {
            return o.GetOrSetCustomField(nameof(CustomEffects), o.AttributeCustomEffects());
        }
        
        public static HashSet<string> AttributeCustomEffects(this object o)
        {
            if (o.GetCustomAttributes(out var attr) && attr.TryGetValue(nameof(CustomEffects), out var effects))
            {
                return effects;
            }

            return new HashSet<string>();
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

        public static bool IsExtendedType(this WeaponsType type)
        {
            return EnumTools.ExtensionBases.TryGetValue(typeof(WeaponsType), out var bases) && bases.ContainsKey(type);
        }
        
        public static bool IsExtendedType(this ArmorType type)
        {
            return EnumTools.ExtensionBases.TryGetValue(typeof(ArmorType), out var bases) && bases.ContainsKey(type);
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
        
        public static bool HasRegistry(this WeaponsType type, out WeaponRegistry reg)
        {
            reg = type.GetRegistry();
            return reg != null;
        }

        public static bool HasRegistry(this ArmorType type, out ArmorRegistry reg)
        {
            reg = type.GetRegistry();
            return reg != null;
        }

        public static bool HasRegistry(this Weapon wp, out WeaponRegistry reg)
        {
            reg = ((WeaponsType)wp.type).GetRegistry();
            return reg != null;
        }

        public static bool HasRegistry(this Armor ar, out ArmorRegistry reg)
        {
            reg = ((ArmorType)ar.type).GetRegistry();
            return reg != null;
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

        public static bool NoShenanigans(this Abilities.Ability ability)
        {
            return !ability.targetsGround &&
                   !ability.onlyShowArea &&
                   !ability.isSystem &&
                   ability.petEffect == null &&
                   ability.special != Abilities.SpecialEffect.kSwapPositions &&
                   ability.special != Abilities.SpecialEffect.kDisplaceOther &&
                   ability.special != Abilities.SpecialEffect.kDisplaceOther1Third &&
                   ability.special != Abilities.SpecialEffect.kTeleportOther &&
                   ability.special != Abilities.SpecialEffect.kTeleportOtherIntoOther &&
                   ability.special != Abilities.SpecialEffect.kCanopicJar &&
                   ability.special != Abilities.SpecialEffect.kPandoraBox &&
                   ability.special != Abilities.SpecialEffect.kSummonAnyDemonTier1 &&
                   ability.special != Abilities.SpecialEffect.kSummonAnyDemonTier2 &&
                   ability.special != Abilities.SpecialEffect.kSummonAnyDemonTier3 &&
                   ability.special2 != Abilities.SpecialEffect2.kSacrifice &&
                   ability.special2 != Abilities.SpecialEffect2.kSacrificeAndPowerScale &&
                   ability.special2 != Abilities.SpecialEffect2.kSacrificeAndTransferBuffs &&
                   ability.hasExtraBox == Abilities.ExtraCommandBox.kNone;
        }

        #if NET6_0
        public static string ToArrayString<T>(this Il2CppArrayBase<T> array)
        {
            return array == null ? "" : string.Join(", ", array.Select(t => t.ToString()));
        }

        public static bool Il2CppEquals(this object thiz, object other)
        {
            if (thiz == other || thiz.Equals(other))
            {
                return true;
            }

            if (thiz is Il2CppSystem.Object a && other is Il2CppSystem.Object b && a.Equals(b))
            {
                return true;
            }
            return false;
        }
        #else
        public static bool IsAssignableTo(this Type thisType, Type targetType)
        {
            return (object) targetType != null && targetType.IsAssignableFrom(thisType);
        }
        #endif
    }
}
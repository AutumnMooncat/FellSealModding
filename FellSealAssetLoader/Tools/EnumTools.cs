using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using System.Runtime.InteropServices;
using MelonLoader.NativeUtils;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Runtime;
#else
#endif

// Enum Ext (Wip, fully custom)
namespace FellSealAssetLoader.Tools
{
    [HarmonyPatch]
    public class EnumTools
    {
        internal static readonly Dictionary<Type, Dictionary<Enum, string>> ExtensionNames = new Dictionary<Type, Dictionary<Enum, string>>();
        internal static readonly Dictionary<Type, Dictionary<Enum, int>> ExtensionBases = new Dictionary<Type, Dictionary<Enum, int>>();
        internal static readonly Dictionary<Type, Dictionary<string, ShadowField>> ExtensionFields = new Dictionary<Type, Dictionary<string, ShadowField>>();
        private static bool _performingExtension;
        
        //[AssetInit]
        public static unsafe void Init()
        {
            #if NET6_0
            // Getting the IntPtr for our target method with GetIl2CppMethodInfoPointerFieldForGeneratedMethod
            IntPtr originalMethod = *(IntPtr*) (IntPtr) Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(
                AccessTools.Method(typeof(Il2CppSystem.Enum), nameof(Il2CppSystem.Enum.ToString), new Type[]{})).GetValue(null);
            //IntPtr test = AccessTools.Method(typeof(Il2CppSystem.Enum), nameof(Il2CppSystem.Enum.ToString), new Type[] { }).MethodHandle.GetFunctionPointer();

            // Storing our patch method in one of the delegate fields
            _patchDelegate = ToString;

            // Getting the IntPtr from _patchDelegate
            IntPtr delegatePointer = Marshal.GetFunctionPointerForDelegate(_patchDelegate);

            // Creating the NativeHook with our target method' IntPtr and patch delegate' IntPtr
            NativeHook<ToStringDelegate> hook = new NativeHook<ToStringDelegate>(originalMethod, delegatePointer);

            // Very important part, actually telling it to attach and hook into the target method
            hook.Attach();

            // Storing the hook so we can use the trampoline in it to run the original method in our patch
            Hook = hook;
            #endif
        }

        #if NET6_0
        // Delegate for our patch, same number of parameters as our patch method
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ToStringDelegate(
            IntPtr instance,
            IntPtr methodInfo
        );

        // Two static fields with our delegate type
        private static NativeHook<ToStringDelegate> Hook;
        private static ToStringDelegate _patchDelegate;

        // The patch method, dealing with unmanaged to managed then back to unmanaged, so pointers galore
        public static unsafe IntPtr ToString(IntPtr instance, IntPtr methodInfo)
        {
            IntPtr result = Hook.Trampoline(instance, methodInfo);
            string name = IL2CPP.PointerToValueGeneric<string>(result, false, false);
            //Melon<AssetLoaderMod>.Logger.Msg($"Got {name}");
            Il2CppSystem.Enum maybe = Il2CppObjectPool.Get<Il2CppSystem.Enum>(instance);
            //Melon<AssetLoaderMod>.Logger.Msg($"From {maybe.GetIl2CppType().Name}");
            Type lookup = null;
            foreach (var key in ExtensionNames.Keys)
            {
                if (Il2CppType.From(key) == maybe.GetIl2CppType())
                {
                    lookup = key;
                    //Melon<AssetLoaderMod>.Logger.Msg($"Enum has extensions");
                    break;
                }
            }
            if (lookup != null && ExtensionNames.TryGetValue(lookup, out var names) && ExtensionBases.TryGetValue(lookup, out var bases))
            {
                foreach (var pair in names)
                {
                    //Melon<AssetLoaderMod>.Logger.Msg($"Found extension {pair.Key}");
                    if (bases.TryGetValue(pair.Key, out var val) && val.ToString().Equals(name))
                    {
                        Melon<AssetLoaderMod>.Logger.Msg($"Redirecting {maybe.GetIl2CppType().Name} enum name {name} to {pair.Value}");
                        return IL2CPP.ManagedStringToIl2Cpp(pair.Value);
                    }
                }
            }
            
            return result;
        }
        #endif
        
        public static T RequestExtendedEnum<T>(string name) where T : struct, Enum
        {
            var type = typeof(T);
            if (!ExtensionNames.ContainsKey(type))
            {
                ExtensionNames[type] = new Dictionary<Enum, string>();
                ExtensionBases[type] = new Dictionary<Enum, int>();
                ExtensionFields[type] = new Dictionary<string, ShadowField>();
            }

            if (ExtensionNames[type].TryGetKey(name, out var key))
            {
                return (T) key;
            }

            _performingExtension = true;
            var index = 0;
            var flags = Attribute.IsDefined(type, typeof(FlagsAttribute));
            T ext = default;
            var val = 0;
            while (index < int.MaxValue)
            {
                val = flags ? 1 << index : 1 + index;
                if (!Enum.IsDefined(type, Enum.ToObject(type, val)))
                {
                    ext = (T) Enum.ToObject(type, val);
                    break;
                }
                index++;
            }

            if (ext.Equals(default(T)))
            {
                Melon<AssetLoaderMod>.Logger.BigError($"Failed to create Enum Extension {name} for {type}");
                _performingExtension = false;
                return default;
            }

            var prototype = type.GetField(Enum.GetName(type, Enum.ToObject(type, 0)));
            ExtensionNames[type][ext] = name;
            ExtensionBases[type][ext] = val;
            ExtensionFields[type][name] = new ShadowField(name, prototype)
            {
                Value = val
            };
            #if NET6_0
            EnumInjector.InjectEnumValues(type, new Dictionary<string, object>
            {
                [name] = ext
            });
            #endif
            Melon<AssetLoaderMod>.Logger.Msg($"Created Extended Enum \"{name}\" for {type} at index {val} -> {ext}");
            _performingExtension = false;
            return ext;
        }
        
        [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues), typeof(Type))]
        public static class FixGetValues
        {
            public static void Postfix(ref Array __result, Type enumType)
            {
                //Melon<AssetLoaderMod>.Logger.Msg($"Got enum array {__result}({enumType.Name})");
                if (!ExtensionNames.TryGetValue(enumType, out var extension) || extension.Count == 0)
                {
                    return;
                }
                //Melon<AssetLoaderMod>.Logger.Msg($"Enum.GetValues extension found");
                foreach (var extensionValue in extension.Values)
                {
                    //Melon<AssetLoaderMod>.Logger.Msg($"-> {extensionValue}");
                }
                //Melon<AssetLoaderMod>.Logger.Msg($"Enum.GetValues({enumType}) got array of -> {__result.GetValue(0).GetType()}, trying to insert {extension.Keys.ToArray()[0].GetType()}");
                var vals = extension.Keys.ToList();
                var curr = __result.Length;
                var ret = Array.CreateInstance(enumType, curr + vals.Count);
                Array.Copy(__result, ret, curr);
                for (int i = 0; i < vals.Count; i++)
                {
                    ret.SetValue(vals[i], curr + i);
                }

                __result = ret;
            }
        }
        
        [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames), typeof(Type))]
        public static class FixGetNames
        {
            public static void Postfix(ref string[] __result, Type enumType)
            {
                //Melon<AssetLoaderMod>.Logger.Msg($"Got enum array {__result}({enumType.Name})");
                if (!ExtensionNames.TryGetValue(enumType, out var extension) || extension.Count == 0)
                {
                    return;
                }
                //Melon<AssetLoaderMod>.Logger.Msg($"Enum.GetValues extension found");
                foreach (var extensionValue in extension.Values)
                {
                    //Melon<AssetLoaderMod>.Logger.Msg($"-> {extensionValue}");
                }
                var vals = extension.Values.ToList();
                var curr = __result.Length;
                var ret = new string[curr + vals.Count];
                Array.Copy(__result, ret, curr);
                for (int i = 0; i < vals.Count; i++)
                {
                    ret.SetValue(vals[i], curr + i);
                }

                __result = ret;
            }
        }
        
        [HarmonyPatch(typeof(Enum), nameof(Enum.ToString), new Type[]{})]
        public static class FixToString
        {
            public static void Postfix(Enum __instance, ref string __result)
            {
                if (ExtensionNames.TryGetValue(__instance.GetType(), out var names) && names.TryGetValue(__instance, out var name))
                {
                    __result = name;
                }
            }
        }

        [HarmonyPatch(typeof(Enum), nameof(Enum.IsDefined), typeof(Type), typeof(object))]
        public static class FixIsDefined
        {
            public static void Postfix(ref bool __result, Type enumType, object value)
            {
                if (!__result && value is int i && ExtensionBases.TryGetValue(enumType, out var bases) && bases.ContainsValue(i))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Type), nameof(Type.GetField), typeof(string))]
        public static class FixGetField
        {
            public static void Postfix(Type __instance, ref FieldInfo __result, string name)
            {
                if (ExtensionFields.TryGetValue(__instance, out var fields) && fields.TryGetValue(name, out var field))
                {
                    __result = field;
                }
            }
        }
        
        #if NET6_0
        [HarmonyPatch(typeof(Il2CppSystem.Xml.Serialization.XmlReflectionImporter), nameof(Il2CppSystem.Xml.Serialization.XmlReflectionImporter.ImportEnumMapping))]
        public static class Test
        {
            public static bool Prefix(
                Il2CppSystem.Xml.Serialization.XmlReflectionImporter __instance, 
                ref Il2CppSystem.Xml.Serialization.XmlTypeMapping __result,
                Il2CppSystem.Xml.Serialization.TypeData typeData, 
                Il2CppSystem.Xml.Serialization.XmlRootAttribute root,
                string defaultNamespace)
            {
                //Melon<AssetLoaderMod>.Logger.Msg($"XmlReflectionImporter processing {typeData.type}, we might explode");
                var type = typeData.Type;
                var registeredClrType = __instance.helper.GetRegisteredClrType(type, __instance.GetTypeNamespace(typeData, root, defaultNamespace));
                if (registeredClrType != null)
                {
                    __result = registeredClrType;
                    return false;
                }
                if (!__instance.allowPrivateTypes)
                {
                    Il2CppSystem.Xml.Serialization.ReflectionHelper.CheckSerializableType(type, false);
                }
                var typeMapping = __instance.CreateTypeMapping(typeData, root, null, defaultNamespace);
                typeMapping.IsNullable = false;
                __instance.helper.RegisterClrType(typeMapping, type, typeMapping.XmlTypeNamespace);
                var arrayList = new Il2CppSystem.Collections.ArrayList();
                foreach (var name in Il2CppSystem.Enum.GetNames(type))
                {
                    var field = type.GetField(name);
                    if (field == null)
                    {
                        foreach (var pair in ExtensionNames)
                        {
                            if (pair.Key.Il2CppEquals(type) && ExtensionNames[pair.Key].TryGetKey(name, out var ext))
                            {
                                arrayList.Add(new Il2CppSystem.Xml.Serialization.EnumMap.EnumMapMember(name, name, ExtensionBases[pair.Key][ext]));
                                break;
                            }
                        }
                    } else if (!field.IsDefined(Il2CppType.Of<Il2CppSystem.Xml.Serialization.XmlIgnoreAttribute>(), false))
                    {
                        string xmlName = null;
                        object[] customAttributes = field.GetCustomAttributes(Il2CppType.Of<Il2CppSystem.Xml.Serialization.XmlEnumAttribute>(), false);
                        if (customAttributes.Length != 0)
                            xmlName = ((Il2CppSystem.Xml.Serialization.XmlEnumAttribute) customAttributes[0]).Name;
                        if (xmlName == null)
                            xmlName = name;
                        long int64 = field.GetValue(null).Cast<Il2CppSystem.IConvertible>().ToInt64(Il2CppSystem.Globalization.CultureInfo.InvariantCulture.Cast<Il2CppSystem.IFormatProvider>());
                        arrayList.Add(new Il2CppSystem.Xml.Serialization.EnumMap.EnumMapMember(xmlName, name, int64));
                    }
                }

                var stuff = new Il2CppSystem.Xml.Serialization.EnumMap.EnumMapMember[arrayList.Count];
                for (var i = 0; i < arrayList.Count; i++)
                {
                    stuff[i] = arrayList[i].Cast<Il2CppSystem.Xml.Serialization.EnumMap.EnumMapMember>();
                }
                var isFlags = type.IsDefined(Il2CppType.Of<Il2CppSystem.FlagsAttribute>(), false);
                typeMapping.ObjectMap = new Il2CppSystem.Xml.Serialization.EnumMap(stuff, isFlags);
                __instance.ImportTypeMapping(Il2CppType.Of<Il2CppSystem.Object>()).DerivedTypes.Add(typeMapping);
                __result = typeMapping;
                return false;
            }
        }
        #endif

        [HarmonyPatch(typeof(Enum), "GetCachedValuesAndNames")]
        public static class FixGetCachedValuesAndNames
        {
            private static readonly List<object> Patched = new List<object>();
            
            public static void Postfix(object __result, object enumType, bool getNames)
            {
                if (_performingExtension || Patched.Contains(__result))
                {
                    return;
                }
                
                var maybe = enumType as Type;
                if (maybe == null)
                {
                    return;
                }
                if (ExtensionNames.TryGetValue(maybe, out var extNames) && ExtensionBases.TryGetValue(maybe, out var extVals))
                {
                    Melon<AssetLoaderMod>.Logger.Msg($"GetCachedValuesAndNames -> found extension on type: {maybe}");
                    Patched.Add(__result);
                    var valNamesType = __result.GetType();
                    var valsField = valNamesType.GetField("Values");
                    var namesField = valNamesType.GetField("Names");
                    var vals = valsField.GetValue(__result) as ulong[];
                    var names = namesField.GetValue(__result) as string[];
                    var newVals = vals.AddRangeToArray(extVals.Values.Select(i => (ulong)i).Where(l => !vals.Contains(l)).ToArray());
                    var newNames = names.AddRangeToArray(extNames.Values.Where(s => !names.Contains(s)).ToArray());
                    valsField.SetValue(__result, newVals);
                    namesField.SetValue(__result, newNames);
                }
            }
        }
    }
}
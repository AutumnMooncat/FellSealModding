using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FellSealAssetLoader;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using Action = System.Action;
using Sprite = UnityEngine.Sprite;

#if NET6_0
using Il2Cpp;
using Il2CppApEngine;
using Il2CppGame;
using Il2CppGame.Data;
using Il2CppGame.Data.DLC1;
using IniFile = Il2CppApEngine.IniFile;
using Il2CppSpriteEngine;
using Il2CppTMPro;
using Il2CppSystem.Xml.Serialization;
using XmlLoader = Il2CppApEngine.XmlLoader;
using Object = Il2CppSystem.Object;
#else
using ApEngine;
using Game;
using Game.Data;
using Game.Data.DLC1;
using IniFile = ApEngine.IniFile;
using SpriteEngine;
using TMPro;
using System.Xml.Serialization;
#endif

[assembly: MelonInfo(typeof(AssetLoaderMod), "Fell Seal Asset Loader", "0.0.1", "Autumn Mooncat")]
namespace FellSealAssetLoader
{
    public class AssetLoaderMod : MelonMod
    {
        internal static readonly Dictionary<string, Sprite> UnitySprites = new Dictionary<string, Sprite>();
        internal static readonly Dictionary<string, Texture2D> UnityTextures = new Dictionary<string, Texture2D>();
        internal static readonly Dictionary<object, Dictionary<string, object>> CustomAttributes = new Dictionary<object, Dictionary<string, object>>();
        internal static readonly ConditionalWeakTable<object, Dictionary<string, object>> CustomData =
            new ConditionalWeakTable<object, Dictionary<string, object>>();
        internal static readonly Dictionary<Type, Dictionary<string, Enum>> Extensions = new Dictionary<Type, Dictionary<string, Enum>>();
        internal static readonly Dictionary<string, object> Contexts = new Dictionary<string, object>(); 
        
        private static bool _earlyHooked;
        private static bool _lateHooked;
        private static readonly List<Action> ToEarlyHook = new List<Action>();
        private static readonly List<Action> ToLateHook = new List<Action>();
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Asset Loader initialized");
            LoggerInstance.Msg("Loading custom sprites");
            Patches.FrozenWalk(MelonEnvironment.ModsDirectory, ".png", png =>
            {
                var name = png.Substring(0, png.Length-4).Split('\\', '/').Last();
                var rawByes = File.ReadAllBytes(png);
                var tex2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                if (tex2D.LoadImage(rawByes, false))
                {
                    //LoggerInstance.Msg("Loading sprite "+name);
                    if (UnitySprites.ContainsKey(name))
                    {
                        LoggerInstance.Warning("Sprite name collision on "+name);
                    }
                    
                    var spr = Sprite.Create(tex2D, new Rect(0f, 0f, tex2D.width, tex2D.height),
                        new Vector2(tex2D.width / 2f, tex2D.height / 2f));
                    spr.name = name;
                    UnitySprites[name] = spr; 
                    UnityTextures[name] = tex2D;
                    tex2D.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    spr.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
                else
                {
                    LoggerInstance.Error("Failed to load sprite at "+png);
                }
            });
            //LoggerInstance.Msg("Loading custom sounds");
        }

        public override void OnDeinitializeMelon()
        {
            foreach (var tex in UnityTextures.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }
            foreach (var tex in UnitySprites.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }

            Patches.LoadImages.TMPStitching.Stitched = false;
        }

        public override void OnLateInitializeMelon()
        {
            EarlyHook();

            HarmonyInstance.Patch(AccessTools.Method(typeof(Platform), nameof(Platform.UpdateLate)),
                new HarmonyMethod(AccessTools.Method(typeof(AssetLoaderMod), nameof(LateHook))));
        }

        private static void EarlyHook()
        {
            if (!_earlyHooked)
            {
                _earlyHooked = true;
                Melon<AssetLoaderMod>.Logger.Msg($"Running {ToEarlyHook.Count} hook action{(ToEarlyHook.Count == 1 ? "" : "s")}");
                foreach (var a in ToEarlyHook)
                {
                    a();
                }
                ToEarlyHook.Clear();
            }
        }
        
        private static void LateHook()
        {
            if (!_lateHooked)
            {
                _lateHooked = true;
                Melon<AssetLoaderMod>.Logger.Msg($"Running {ToLateHook.Count} late hook action{(ToLateHook.Count == 1 ? "" : "s")}");
                foreach (var a in ToLateHook)
                {
                    a();
                }
                ToLateHook.Clear();
            }
        }

        public static Dictionary<string, object> GetCustomData(object o)
        {
            return CustomData.GetOrCreateValue(o);
        }

        public static bool GetCustomAttributes(object o, out Dictionary<string, object> attr)
        {
            #if NET6_0
            if (o is Object obj)
            {
                foreach (var key in CustomAttributes.Keys)
                {
                    var maybe = key as Object;
                    if (obj.Equals(maybe))
                    {
                        attr = CustomAttributes[key];
                        return true;
                    }
                }
            }
            #endif
            return CustomAttributes.TryGetValue(o, out attr);
        }
        
        public static void DoOnHook(Action a)
        {
            if (DidHook())
            {
                a();
            }
            else
            {
                ToEarlyHook.Add(a);
            }
        }

        public static bool DidHook()
        {
            return _earlyHooked;
        }

        public static void DoOnLateHook(Action a)
        {
            if (DidLateHook())
            {
                a();
            }
            else
            {
                ToLateHook.Add(a);
            }
        }

        public static bool DidLateHook()
        {
            return _lateHooked;
        }
        
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

        public static Context<T> RequestContext<T>(string methodName, params Type[] paramtypez) where T : class
        {
            var key = ContextKey(typeof(T).Name, methodName, paramtypez);
            if (Contexts.TryGetValue(key, out var found))
            {
                return found as Context<T>;
            }

            var ctx = new Context<T>();
            Melon<AssetLoaderMod>.Logger.Msg($"Creating Context for {key}");
            Contexts[key] = ctx;
            DoOnHook(() =>
            {
                ctx.Register(AccessTools.Method(typeof(T), methodName, paramtypez));
                HookContext(ctx);
            });
            return ctx;
        }
        
        public static Context<T> RequestLateContext<T>(string methodName, params Type[] paramtypez) where T : class
        {
            var key = ContextKey(typeof(T).Name, methodName, paramtypez);
            if (Contexts.TryGetValue(key, out var found))
            {
                return found as Context<T>;
            }

            Melon<AssetLoaderMod>.Logger.Msg($"Creating Late Context for {key}");
            var ctx = new Context<T>();
            Contexts[key] = ctx;
            DoOnLateHook(() =>
            {
                ctx.Register(AccessTools.Method(typeof(T), methodName, paramtypez));
                HookContext(ctx);
            });
            return ctx;
        }

        public static string ContextKey(MethodBase method)
        {
            return ContextKey(method.DeclaringType?.Name, method.Name, method.GetParameters().Types());
        }

        public static string ContextKey(string typeName, string methodName, params Type[] paramtypez)
        {
            return $"{typeName}.{methodName}[{(paramtypez == null ? "" : string.Join(", ", paramtypez.Select(t => t.ToString())))}]";
        }

        private static void HookContext<T>(Context<T> context) where T: class
        {
            if (context.IsRegistered(out var hook))
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Hooking Context for {hook.DeclaringType?.Name}.{hook.Name}");
                if (hook is MethodInfo info && info.ReturnType != typeof(void))
                {
                    Melon<AssetLoaderMod>.Instance.HarmonyInstance.Patch(hook,
                        new HarmonyMethod(AccessTools.Method(typeof(AssetLoaderMod), nameof(HoldReturnContext))),
                        new HarmonyMethod(AccessTools.Method(typeof(AssetLoaderMod), nameof(ReleaseReturnContext)))
                    );
                }
                else
                {
                    Melon<AssetLoaderMod>.Instance.HarmonyInstance.Patch(hook,
                        new HarmonyMethod(AccessTools.Method(typeof(AssetLoaderMod), nameof(HoldContext))),
                        new HarmonyMethod(AccessTools.Method(typeof(AssetLoaderMod), nameof(ReleaseContext)))
                    );
                }
            }
            else
            {
                Melon<AssetLoaderMod>.Logger.Error($"Context has no hook registered");
            }
        }

        private static bool HoldContext(object __instance, MethodBase __originalMethod, object[] __args)
        {
            bool[] run = { true };
            var type = __originalMethod.IsStatic ? __originalMethod.DeclaringType : __instance.GetType();
            /*Melon<AssetLoaderMod>.Logger.Msg($"Checking for Context for {type}.{__originalMethod.Name}");
            foreach (var pair in Contexts)
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Has {pair.Key} -> {pair.Value}");
            }*/
            if (Contexts.TryGetValue(ContextKey(__originalMethod), out var val))
            {
                //Melon<AssetLoaderMod>.Logger.Msg("Context found, handling");
                typeof(Context<>).MakeGenericType(type).GetMethod("Hold")?.Invoke(val, new []{__instance, __args, run, new object[]{null}});
            }

            return run[0];
        }

        private static void ReleaseContext(object __instance, MethodBase __originalMethod, object[] __args)
        {
            var type = __originalMethod.IsStatic ? __originalMethod.DeclaringType : __instance.GetType();
            if (Contexts.TryGetValue(ContextKey(__originalMethod), out var val))
            {
                typeof(Context<>).MakeGenericType(type).GetMethod("Release")?.Invoke(val, new []{__instance, __args, new object[]{null}});
            }
        }
        
        private static bool HoldReturnContext(object __instance, MethodBase __originalMethod, object[] __args, ref object __result)
        {
            bool[] run = { true };
            object[] ret = { __result };
            var type = __originalMethod.IsStatic ? __originalMethod.DeclaringType : __instance.GetType();
            if (Contexts.TryGetValue(ContextKey(__originalMethod), out var val))
            {
                typeof(Context<>).MakeGenericType(type).GetMethod("Hold")?.Invoke(val, new []{__instance, __args, run, ret});
            }

            __result = ret[0];
            return run[0];
        }
        
        private static void ReleaseReturnContext(object __instance, MethodBase __originalMethod, object[] __args, ref object __result)
        {
            object[] ret = { __result };
            var type = __originalMethod.IsStatic ? __originalMethod.DeclaringType : __instance.GetType();
            if (Contexts.TryGetValue(ContextKey(__originalMethod), out var val))
            {
                typeof(Context<>).MakeGenericType(type).GetMethod("Release")?.Invoke(val, new []{__instance, __args, ret});
            }

            __result = ret[0];
        }
    }

    public class Context<T> where T : class
    {
        public delegate void HoldDel(T __instance, object[] __args);
        public delegate void HoldReturnDel(T __instance, object[] __args, bool[] __doRun, object[] __result);
        
        public delegate void ReleaseDel(T __instance, object[] __args, object __result);
        public delegate void ReleaseReturnDel(T __instance, object[] __args, object[] __result);
        
        public T instance;
        public object[] args;
        private MethodBase _hook;
        private bool _held;

        public event HoldReturnDel OnHold;
        public event ReleaseReturnDel OnRelease;

        public void Register(MethodBase hook)
        {
            if (IsRegistered())
            {
                Melon<AssetLoaderMod>.Logger.Error($"Context has already registered {_hook}, cannot register {hook}");
                return;
            }

            _hook = hook;
        }

        public bool IsRegistered()
        {
            return _hook != null;
        }
        
        public bool IsRegistered(out MethodBase hook)
        {
            hook = _hook;
            return _hook != null;
        }

        public bool Get()
        {
            return IsRegistered() && _held;
        }

        public bool Get(out T ctx)
        {
            ctx = instance;
            return IsRegistered() && _held;
        }

        public bool Get(out T ctx, out object[] __args)
        {
            ctx = instance;
            __args = args;
            return IsRegistered() && _held;
        }

        public void Hold(T __instance, object[] __args, bool[] __doRunOrig, object[] __result)
        {
            instance = __instance;
            args = __args;
            _held = true;
            OnHold?.Invoke(__instance, __args, __doRunOrig, __result);
        }

        public void Release(T __instance, object[] __args, object[] __result)
        {
            OnRelease?.Invoke(__instance, __args, __result);
            _held = false;
            instance = null;
            args = null;
        }

        public Context<T> WithHold(HoldDel del)
        {
            OnHold += (__instance, __args, orig, result) => del.Invoke(__instance, __args);
            return this;
        }

        public Context<T> WithRelease(ReleaseDel del)
        {
            OnRelease += (__instance, __args, __result) => del.Invoke(__instance, __args, __result[0]);
            return this;
        }
        
        public Context<T> WithHoldReturn(HoldReturnDel del)
        {
            OnHold += del;
            return this;
        }

        public Context<T> WithReleaseReturn(ReleaseReturnDel del)
        {
            OnRelease += del;
            return this;
        }
    }

    // Enum Ext (Wip, fully custom)
    
    // XML Ext (Wip, fully custom) 
    #if NET6_0
    [HarmonyPatch(typeof(XmlSerializer), nameof(XmlSerializer.OnUnknownAttribute))]
    public class XmlRectifier
    {
        public static void Prefix(XmlAttributeEventArgs e)
        {
            Melon<AssetLoaderMod>.Logger.Msg($"Got unknown attribute {e.attr.Name}:{e.attr.Value} when Deserializing {e.o}");
            if (!AssetLoaderMod.CustomAttributes.ContainsKey(e.o))
            {
                AssetLoaderMod.CustomAttributes[e.o] = new Dictionary<string, object>();
            }
            AssetLoaderMod.CustomAttributes[e.o][e.attr.Name] = e.attr.Value;
        }
    }
    #else
    [HarmonyPatch(typeof(XmlSerializer), nameof(XmlSerializer.Deserialize), typeof(TextReader))]
    public class XmlRectifier
    {
        public static void Prefix(XmlSerializer __instance)
        {
            __instance.UnknownAttribute += (sender, args) =>
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Got unknown attribute {args.Attr.Name}:{args.Attr.Value} when Deserializing {args.ObjectBeingDeserialized}");
                if (!AssetLoaderMod.CustomAttributes.ContainsKey(args.ObjectBeingDeserialized))
                {
                    AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized] =
                        new Dictionary<string, object>();
                }

                AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized][args.Attr.Name] =
                    args.Attr.Value;
            };
        }
    }
    #endif

    [HarmonyPatch]
    public static class Patches
    {
        public static void FrozenWalk(string curr, string target, Action<string> callback)
        {
            CheckCustomFile.Frozen = true;
            Walk(curr, target, callback);
            CheckCustomFile.Frozen = false;
        }
        
        private static void Walk(string curr, string target, Action<string> callback)
        {
            //Melon<ModFile>.Logger.Msg("Walking "+curr+" for "+target);
            foreach (var dir in Directory.GetDirectories(curr))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (file.EndsWith(target))
                    {
                        Melon<AssetLoaderMod>.Logger.Msg("Processing "+file);
                        callback(file);
                    }
                }
                Walk(dir, target, callback);
            }
        }

        #if NET6_0
        [HarmonyPatch(typeof(Il2CppSystem.IO.File), nameof(Il2CppSystem.IO.File.Exists))]
        #else
        [HarmonyPatch(typeof(File), nameof(File.Exists))]
        #endif
        public static class CheckCustomFile
        {
            public static readonly List<Func<string, bool, bool>> Checks = new List<Func<string, bool, bool>>();
            public static bool Frozen;
            
            public static void Postfix(bool __result, string path)
            {
                if (Checks.Count > 0 && !Frozen)
                {
                    //Melon<ModFile>.Logger.Msg("Checked if file "+path+" exists, returned "+__result+", processing "+Checks.Count+" checks");
                    Checks.RemoveAll(c => c(path, __result));
                }
            }
        }

        // Localization
        [HarmonyPatch]
        public static class LoadLoc
        {
            private static IniFile _iniContext;
            private static LocManager _locContext;
            private static LocManager.LocInformation[] _files;
            private static int _index;
            private static bool _needsLoading;

            private static void LoadFromContext()
            {
                if (_iniContext != null && _locContext != null && _files != null && _files.Length > _index)
                {
                    _needsLoading = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom "+_files[_index].mFile.Split('.').FirstOrDefault()+" localization");
                    FrozenWalk(MelonEnvironment.ModsDirectory, Path.Combine("languages",_locContext.mLanguageCode,_files[_index].mFile), txt =>
                    {
                        IniFile other = new IniFile();
                        other.TheFile = txt;
                        _iniContext.UpdateWith(other);
                    });
                    _index++;
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom localization");
                }
            }
            
            [HarmonyPatch(typeof(LocManager), nameof(LocManager.LoadLanguage))]
            public static class Prep
            {
                public static void Prefix(LocManager __instance)
                {

                    //Melon<ModFile>.Logger.Msg("LocManager.LoadLanguage begins, hold context");
                    _locContext = __instance;
                    _files = __instance.mFiles;
                    _index = 0;
                }

                public static void Finalizer()
                {
                    //Melon<ModFile>.Logger.Msg("LocManager.LoadLanguage ends, release context");
                    _iniContext = null;
                    _locContext = null;
                    _files = null;
                }
            }

            [HarmonyPatch(typeof(LocManager), nameof(LocManager.LoadBaseFile))]
            public static class GrabIni
            {
                public static void Postfix(IniFile file)
                {
                    if (_locContext != null && _files != null)
                    {
                        _iniContext = file;
                        _needsLoading = true;
                        CheckCustomFile.Checks.Add((path, result) =>
                        {
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            return true;
                        });
                    }
                }
            }

            [HarmonyPatch(typeof(IniFile), nameof(IniFile.UpdateWith))]
            public static class UpdateIfNotHandled
            {
                public static void Postfix(IniFile __instance)
                {
                    if (__instance == _iniContext && _needsLoading)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Game Options
        
        // Encounters

        // Monsters
        [HarmonyPatch]
        public static class LoadMonsters
        {
            private static Monsters _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom monsters");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Monsters.xml", xml =>
                    {
                        _context.UpdateMonstersFromFile(null, xml, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom monsters");
                }
            }
            
            [HarmonyPatch(typeof(Monsters), nameof(Monsters.Load))]
            public static class Prep
            {
                public static void Prefix(Monsters __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Monsters.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Monsters.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Monsters customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Monsters.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Monsters), nameof(Monsters.UpdateMonstersFromFile))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }

        // Jobs + Abilities
        [HarmonyPatch]
        public static class LoadAbilities
        {
            private static Abilities _abilityContext;
            private static bool _needLoadJobs;
            private static bool _needLoadAbilities;
            private static bool _loadingDLC;

            private static void AbilitiesFromContext()
            {
                if (_abilityContext != null)
                {
                    _needLoadAbilities = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom abilities");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Abilities.xml", xml =>
                    {
                        _abilityContext.LoadExtraAbilities(null, xml, true, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom abilities");
                }
            }

            private static void JobsFromContext()
            {
                if (_abilityContext != null)
                {
                    _needLoadJobs = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom jobs");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Jobs.xml", xml =>
                    {
                        _abilityContext.LoadExtraJobs(null, xml, true, ServiceProvider.GetInstance().Get<TermsDictionary>(), true, false);
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom jobs");
                }
            }
            
            [HarmonyPatch(typeof(Abilities), nameof(Abilities.Load))]
            public static class Prep
            {
                public static void Prefix(Abilities __instance)
                {
                    _abilityContext = __instance;
                    //Melon<ModFile>.Logger.Msg("Abilities.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Abilities.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                AbilitiesFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Abilities customdata exists, defer loader");
                                _needLoadAbilities = true;
                            }
                            return true;
                        }

                        if (_abilityContext == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Ability loading failed");
                            return true;
                        }
                        return false;
                    });
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Jobs.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                JobsFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Jobs customdata exists, defer loader");
                                _needLoadJobs = true;
                            }
                            return true;
                        }
                        
                        if (_abilityContext == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Job loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _abilityContext = null;
                    _needLoadAbilities = false;
                    _needLoadJobs = false;
                    //Melon<ModFile>.Logger.Msg("Abilities.Load ends, release context");
                }
            }

            [HarmonyPatch(typeof(Abilities), nameof(Abilities.LoadExtraAbilities))]
            public static class GetAbilities
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;
                }
                
                public static void Postfix()
                {
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Abilities.LoadExtraAbilities called, need load? "+_needLoadAbilities);
                    if (_needLoadAbilities)
                    {
                        AbilitiesFromContext();
                    }
                }
            }

            [HarmonyPatch(typeof(Abilities), nameof(Abilities.LoadExtraJobs))]
            public static class GetJobs
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;
                }
                
                public static void Postfix()
                {
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Abilities.LoadExtraJobs called, need load? "+_needLoadJobs);
                    if (_needLoadJobs)
                    {
                        JobsFromContext();
                    }
                }
            }
        }
        
        // Spell Effects (Maybe, fully custom)
        
        // Weapons
        [HarmonyPatch]
        public static class LoadWeapons
        {
            private static Weapons _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom weapons");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Weapons.xml", xml =>
                    {
                        _context.UpdateWeaponsFromFile(null, xml, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom weapons");
                }
            }
            
            [HarmonyPatch(typeof(Weapons), nameof(Weapons.Load))]
            public static class Prep
            {
                public static void Prefix(Weapons __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Weapons.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Weapons.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Weapons customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Weapons.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Weapons), nameof(Weapons.UpdateWeaponsFromFile))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Armors
        [HarmonyPatch]
        public static class LoadArmors
        {
            private static Armors _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom armors");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Armors.xml", xml =>
                    {
                        _context.UpdateArmorsFromFile(null, xml, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom armors");
                }
            }
            
            [HarmonyPatch(typeof(Armors), nameof(Armors.Load))]
            public static class Prep
            {
                public static void Prefix(Armors __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Armors.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Armors.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Armors customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Armors.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Armors), nameof(Armors.UpdateArmorsFromFile))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Accessories
        [HarmonyPatch]
        public static class LoadAccessories
        {
            private static Accessories _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom accessories");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Accessories.xml", xml =>
                    {
                        _context.UpdateAccsFromFile(null, xml, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom accessories");
                }
            }
            
            [HarmonyPatch(typeof(Accessories), nameof(Accessories.Load))]
            public static class Prep
            {
                public static void Prefix(Accessories __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Accessories.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Accessories.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Accessories customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Accessories.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Accessories), nameof(Accessories.UpdateAccsFromFile))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Recipes + Ingredients + Badges + Gadgets
        [HarmonyPatch]
        public static class LoadCrafting
        {
            private static Ingredients _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom crafting");
                    #if NET6_0
                    var dict = new Il2CppSystem.Collections.Generic.Dictionary<string, Recipe>();
                    #else
                    var dict = new Dictionary<string, Recipe>();
                    #endif
                    foreach (var recipe in _context.mRecipes)
                    {
                        dict[recipe.mItemHash.ToUpperInvariant()] = recipe;
                    }
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Crafting.xml", xml =>
                    {
                        _context.LoadAddedData(null, xml, ServiceProvider.GetInstance().Get<LocManager>(), dict, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom crafting");
                }
            }
            
            [HarmonyPatch(typeof(Ingredients), nameof(Ingredients.Load))]
            public static class Prep
            {
                public static void Prefix(Ingredients __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Ingredients.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Crafting.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Crafting customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Ingredients.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Ingredients), nameof(Ingredients.LoadAddedData))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Consumables
        [HarmonyPatch]
        public static class LoadConsumables
        {
            [HarmonyPatch(typeof(Consumables), nameof(Consumables.Load))]
            public static class Prep
            {
                public static void Postfix(Consumables __instance)
                {
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom crafting");
                    TermsDictionary termsDictionary = ServiceProvider.GetInstance().Get<TermsDictionary>();
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Consumables.xml", xml =>
                    {
                        try
                        {
                            XMLConsumables xmlConsumables = XmlLoader.LoadFromFile<XMLConsumables>(xml);
                            xmlConsumables.FixUp();
                            for (int index = 0; index < xmlConsumables.mConsumables.Count; ++index)
                            {
                                Consumable con = xmlConsumables.mConsumables[index];
                                con.name = __instance.mLocManager.GetTermNoColors(con.description);
                                con.names = __instance.GetNames(con.description);
                                con.description = __instance.mLocManager.GetTerm(con.description + "-desc");
                                string upperInvariant = con.hashName.ToUpperInvariant();
                                if (__instance.mConsumableDict.TryGetValue(upperInvariant, out var found))
                                    __instance.mAllConsumables.Remove(found);
                                __instance.mAllConsumables.Add(con);
                                __instance.mConsumableDict[upperInvariant] = con;
                            }
                        }
                        catch (Exception ex)
                        {
                            Melon<AssetLoaderMod>.Logger.Error($"There was an error parsing file: {xml} with exception: {ex}");
                            __instance.mErrorLoadingFiles = $"{__instance.mErrorLoadingFiles}\n{termsDictionary.ParseStringWithArgs(__instance.mLocManager.GetTermNoColors("options-error-file"), xml)}";
                        }
                    });
                }
            }
        }
        
        // Stores
        [HarmonyPatch]
        public static class LoadStores
        {
            private static Stores _context;
            private static bool _needLoad;
            private static bool _loadingDLC;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom stores");
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Stores.xml", xml =>
                    {
                        _context.LoadAddedData(null, xml, ServiceProvider.GetInstance().Get<LocManager>(), ServiceProvider.GetInstance().Get<TermsDictionary>());
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom stores");
                }
            }
            
            [HarmonyPatch(typeof(Stores), nameof(Stores.Load))]
            public static class Prep
            {
                public static void Prefix(Stores __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _loadingDLC = false;
                    //Melon<ModFile>.Logger.Msg("Stores.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Stores.xml"))
                        {
                            if (_loadingDLC)
                            {
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Stores customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Stores.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(Stores), nameof(Stores.LoadAddedData))]
            public static class GetIfNeeded
            {
                public static void Prefix(IFileSystem fileSystem)
                {
                    _loadingDLC = fileSystem != null;

                }

                public static void Finalizer()
                {
                    _loadingDLC = false;
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }
                }
            }
        }
        
        // Loot Lists (Maybe, fully custom)
        
        // Treasures (Maybe, fully custom)
        
        // World Map (Maybe, fully custom)
        
        // Missions 
        [HarmonyPatch]
        public static class LoadMissions
        {
            private static Missions _context;
            private static bool _needLoad;
            private static bool _firstCheck;

            private static void LoadFromContext()
            {
                if (_context != null)
                {
                    _needLoad = false;
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom missions");
                    TermsDictionary termsDictionary = ServiceProvider.GetInstance().Get<TermsDictionary>();
                    FrozenWalk(MelonEnvironment.ModsDirectory, "Missions.xml", xml =>
                    {
                        try
                        {
                            XMLMissions xmlMissions = XmlLoader.LoadFromFile<XMLMissions>(xml);
                            Missions.AddOrFuse(xmlMissions.mZones, _context.mZones);
                            Missions.AddOrFuse(xmlMissions.mMissions, _context.mMissions);
                            Missions.AddOrFuse(xmlMissions.mUpgrades, _context.mUpgrades);
                            Missions.AddOrFuse(xmlMissions.mHunts, _context.mHunts);
                            Missions.AddOrFuse(xmlMissions.mRecruits, _context.mRecruits);
                            Missions.AddOrFuse(xmlMissions.mGroups, _context.mDurationGroups);
                        }
                        catch (Exception ex)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg($"There was an error parsing file: {xml} with exception: {ex}");
                            _context.mErrorLoadingFiles = $"{_context.mErrorLoadingFiles}\n{termsDictionary.ParseStringWithArgs(_context.mLocManager.GetTermNoColors("options-error-file"), xml)}";
                        }
                    });
                }
                else
                {
                    Melon<AssetLoaderMod>.Logger.Msg("No context for custom missions");
                }
            }
            
            [HarmonyPatch(typeof(Missions), nameof(Missions.Load))]
            public static class Prep
            {
                public static void Prefix(Missions __instance)
                {
                    _context = __instance;
                    _needLoad = false;
                    _firstCheck = true;
                    //Melon<ModFile>.Logger.Msg("Missions.Load begins, hold context");
                    CheckCustomFile.Checks.Add((path, result) =>
                    {
                        if (path.EndsWith("Missions.xml"))
                        {
                            if (_firstCheck)
                            {
                                _firstCheck = false;
                                return false;
                            }
                            if (!result)
                            {
                                LoadFromContext();
                            }
                            else
                            {
                                Melon<AssetLoaderMod>.Logger.Msg("Missions customdata exists, defer loader");
                                _needLoad = true;
                            }
                            return true;
                        }

                        if (_context == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Msg("Loading failed");
                            return true;
                        }
                        return false;
                    });
                }

                public static void Finalizer()
                {
                    _context = null;
                    //Melon<ModFile>.Logger.Msg("Missions.Load ends, release context");
                }
            }
            
            [HarmonyPatch(typeof(WorldmapZone), nameof(WorldmapZone.Fixup))]
            public static class GetIfNeeded
            {
                public static void Prefix()
                {
                    if (_needLoad)
                    {
                        LoadFromContext();
                    }

                }
            }
        }
        
        // Images (Fully custom)
        [HarmonyPatch]
        public static class LoadImages
        {
            [HarmonyPatch(typeof(Loader), nameof(Loader.LoadSprite), typeof(string))]
            public static class SpriteAsset
            {
                public static bool Prefix(Loader __instance, ref Sprite __result, string assetName)
                {
                    //Melon<ModFile>.Logger.Msg("Loading sprite asset "+assetName);
                    if (AssetLoaderMod.UnitySprites.TryGetValue(assetName, out var spr))
                    {
                        //Melon<ModFile>.Logger.Msg("Got custom sprite asset "+assetName);
                        __result = spr;
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(Loader), nameof(Loader.LoadSprite), typeof(string), typeof(string))]
            public static class SpriteAssetAction
            {
                public static bool Prefix(Loader __instance, ref Sprite __result, string assetName, string actionName)
                {
                    //Melon<ModFile>.Logger.Msg("Loading sprite asset action "+assetName+"."+actionName);
                    if (AssetLoaderMod.UnitySprites.TryGetValue(actionName, out var spr))
                    {
                        //Melon<ModFile>.Logger.Msg("Got custom sprite asset action "+assetName+"."+actionName);
                        __result = spr;
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(Loader), nameof(Loader.LoadRawSprite))]
            public static class SpriteAssetRaw
            {
                public static bool Prefix(Loader __instance, ref Sprite __result, string assetName)
                {
                    //Melon<ModFile>.Logger.Msg("Loading raw sprite asset "+assetName);
                    if (AssetLoaderMod.UnitySprites.TryGetValue(assetName, out var spr))
                    {
                        //Melon<ModFile>.Logger.Msg("Got custom raw sprite asset "+assetName);
                        __result = spr;
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(TMP_SpriteAsset), nameof(TMP_SpriteAsset.UpdateLookupTables))]
            public static class TMPStitching
            {
                public static bool Stitched;

                public static void Prefix(ref TMP_SpriteAsset __instance)
                {
                    if (!Stitched)
                    {
                        Stitched = true;
                        if (!AssetLoaderMod.UnitySprites.Any())
                        {
                            return;
                        }
                        Melon<AssetLoaderMod>.Logger.Msg("Stitching TMP sprite atlas");
                        var activeBackup = RenderTexture.active;
                        var spriteSheet = __instance.spriteSheet;
                        var targetRender = new RenderTexture(spriteSheet.width, spriteSheet.height, 32);
                        Graphics.Blit(spriteSheet, targetRender);
                        var origTex = new Texture2D(spriteSheet.width, spriteSheet.height, TextureFormat.ARGB32, false);
                        origTex.ReadPixels(new Rect(0f, 0f, targetRender.width, targetRender.height), 0, 0);
                        origTex.Apply();
                        RenderTexture.active = activeBackup;
                        var textures = new List<Texture2D>();
                        foreach (var info in __instance.spriteInfoList)
                        {
                            //Melon<ModFile>.Logger.Msg($"Found info {info.name} x,y,w,h = {{{info.x}, {info.y}, {info.width}, {info.height}}} xo,yo,xa = {{{info.xOffset}, {info.yOffset}, {info.xAdvance}}}");
                            var tex = new Texture2D((int)info.width, (int)info.height, TextureFormat.ARGB32, false);
                            Graphics.CopyTexture_Region(origTex, 0, 0, (int)info.x, (int)info.y, (int)info.width, (int)info.height, tex, 0, 0, 0, 0);
                            textures.Add(tex);
                        }
                        foreach (var spr in AssetLoaderMod.UnitySprites.Values)
                        {
                            textures.Add(spr.texture);
                            var tmpSpr = new TMP_Sprite
                            {
                                name = spr.name,
                                sprite = spr,
                                id = __instance.spriteInfoList.Count,
                                hashCode = TMP_TextUtilities.GetSimpleHashCode(spr.name),
                                pivot = new Vector2(-spr.texture.width/2f, spr.texture.height/2f),
                                width = spr.texture.width,
                                height = spr.texture.height,
                                scale = 1f,
                                yOffset = spr.texture.height <= 16f ? 14f : 28f,
                                xAdvance = spr.texture.width,
                                x = -1,
                                y = -1
                            };
                            __instance.spriteInfoList.Add(tmpSpr);
                        }
                        var stitchedTex = new Texture2D(spriteSheet.width, spriteSheet.height, TextureFormat.ARGB32, false);
                        var rects = stitchedTex.PackTextures(textures.ToArray(), 0, 8192, false);
                        for (var i = 0; i < __instance.spriteInfoList.Count; i++)
                        {
                            //Melon<ModFile>.Logger.Msg("Got Rect "+rects[i]);
                            __instance.spriteInfoList[i].x = rects[i].x * stitchedTex.width;
                            __instance.spriteInfoList[i].y = rects[i].y * stitchedTex.height;
                        }
                        __instance.spriteSheet = stitchedTex;
                        __instance.material.mainTexture = stitchedTex;
                        File.WriteAllBytes("ExportedAtlas.png", ImageConversion.EncodeToPNG(stitchedTex));
                    }
                }
            }
        }
        
        // Sounds (Maybe, fully custom)
    }
}
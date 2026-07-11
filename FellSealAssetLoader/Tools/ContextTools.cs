using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

namespace FellSealAssetLoader.Tools
{
    public static class ContextTools
    {
        private static readonly Dictionary<string, object> Contexts = new Dictionary<string, object>(); 
        
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
            HookTools.DoOnHook(() =>
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
            HookTools.DoOnLateHook(() =>
            {
                ctx.Register(AccessTools.Method(typeof(T), methodName, paramtypez));
                HookContext(ctx);
            });
            return ctx;
        }

        private static string ContextKey(MethodBase method)
        {
            return ContextKey(method.DeclaringType?.Name, method.Name, method.GetParameters().Types());
        }

        private static string ContextKey(string typeName, string methodName, params Type[] paramtypez)
        {
            return $"{typeName}.{methodName}({paramtypez.ToArrayString()})";
        }
        
        private static void HookContext<T>(Context<T> context) where T: class
        {
            if (context.IsRegistered(out var hook))
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Hooking Context for {hook.DeclaringType?.Name}.{hook.Name}");
                if (hook is MethodInfo info && info.ReturnType != typeof(void))
                {
                    Melon<AssetLoaderMod>.Instance.HarmonyInstance.Patch(hook,
                        new HarmonyMethod(AccessTools.Method(typeof(ContextTools), nameof(HoldReturnContext))),
                        new HarmonyMethod(AccessTools.Method(typeof(ContextTools), nameof(ReleaseReturnContext)))
                    );
                }
                else
                {
                    Melon<AssetLoaderMod>.Instance.HarmonyInstance.Patch(hook,
                        new HarmonyMethod(AccessTools.Method(typeof(ContextTools), nameof(HoldContext))),
                        new HarmonyMethod(AccessTools.Method(typeof(ContextTools), nameof(ReleaseContext)))
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
}
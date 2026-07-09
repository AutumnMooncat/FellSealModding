using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace FellSealAssetLoader.Util
{
    public static class AttributeProcessor
    {
        public static void Run<T>(params object[] paramz) where T : Attribute
        {
            Melon<AssetLoaderMod>.Logger.Msg($"AttributeProcessor running {typeof(T)}");
            foreach (var method in ValidMethods().Where(m => Attribute.IsDefined(m, typeof(T))))
            {
                TryInvoke(method, paramz);
            }
        }

        private static void TryInvoke(MethodInfo method, params object[] paramz)
        {
            var paramPool = paramz.ToList();
            var neededTypes = method.GetParameters().Types();
            var foundParams = new List<object>();
            foreach (var neededType in neededTypes)
            {
                foreach (var param in paramPool.Where(param => param.GetType().IsAssignableTo(neededType)))
                {
                    foundParams.Add(param);
                    paramPool.Remove(param);
                    break;
                }
            }

            if (foundParams.Count != neededTypes.Length)
            {
                Melon<AssetLoaderMod>.Logger.Error($"AttributeProcessor failed to call {method.DeclaringType}.{method.Name} due to missing parameters");
                return;
            }
            
            method.Invoke(null, foundParams.ToArray());
            Melon<AssetLoaderMod>.Logger.Msg($"AttributeProcessor invoked {method.DeclaringType}.{method.Name}");
        }

        private static IEnumerable<Type> ValidTypes() => 
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => !Attribute.IsDefined(t, typeof(ProcessorIgnoreAttribute)));
        
        private static IEnumerable<MethodInfo> ValidMethods() => 
            ValidTypes().SelectMany(t => t.GetMethods(AccessTools.all))
                .Where(m => !Attribute.IsDefined(m, typeof(ProcessorIgnoreAttribute)));
        
        private static IEnumerable<MethodInfo> ValidMethods(Type t) => 
            t.GetMethods(AccessTools.all)
                .Where(m => !Attribute.IsDefined(m, typeof(ProcessorIgnoreAttribute)));
        
    }
    
    public class ProcessorIgnoreAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method)]
    public class AssetInitAttribute : Attribute {}
    
    [AttributeUsage(AttributeTargets.Method)]
    public class AssetLateInitAttribute : Attribute {}
    
    [AttributeUsage(AttributeTargets.Method)]
    public class AssetDeinitAttribute : Attribute {}
}
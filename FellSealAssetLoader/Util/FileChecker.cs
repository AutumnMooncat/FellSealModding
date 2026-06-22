using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2CppSystem.IO;
#else
using System.IO;
#endif

namespace FellSealAssetLoader.Util
{
    [HarmonyPatch]
    public static class FileChecker
    {
        public static void Add(Func<string, bool, bool> check)
        {
            Processor.Checks.Add(check);    
        }
        
        public static void FrozenWalk(string curr, string target, Action<string> callback)
        {
            Processor.Frozen = true;
            Walk(curr, target, callback);
            Processor.Frozen = false;
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

        [HarmonyPatch(typeof(File), nameof(File.Exists))]
        public static class Processor
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
    }
}
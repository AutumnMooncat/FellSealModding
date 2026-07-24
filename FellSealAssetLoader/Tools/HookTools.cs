using System.Collections.Generic;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using Action = System.Action;

#if NET6_0
using Il2Cpp;
#else
#endif

namespace FellSealAssetLoader.Tools
{
    public static class HookTools
    {
        private static bool _earlyHooked;
        private static bool _lateHooked;
        private static readonly List<Action> ToEarlyHook = new List<Action>();
        private static readonly List<Action> ToLateHook = new List<Action>();

        [AssetLateInit]
        public static void Init(HarmonyLib.Harmony harmony)
        {
            EarlyHook();

            harmony.Patch(AccessTools.Method(typeof(Platform), nameof(Platform.UpdateLate)),
                new HarmonyMethod(AccessTools.Method(typeof(HookTools), nameof(LateHook))));
        }
        
        private static void EarlyHook()
        {
            if (!_earlyHooked)
            {
                _earlyHooked = true;
                Melon<AssetLoaderMod>.Logger.WriteSpacer();
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
                Melon<AssetLoaderMod>.Logger.WriteSpacer();
                Melon<AssetLoaderMod>.Logger.Msg($"Running {ToLateHook.Count} late hook action{(ToLateHook.Count == 1 ? "" : "s")}");
                foreach (var a in ToLateHook)
                {
                    a();
                }
                ToLateHook.Clear();
            }
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
    }
}
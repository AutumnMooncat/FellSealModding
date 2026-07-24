using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;

#if NET6_0
using Il2CppApEngine;
using Il2CppGame;
using Il2CppGame.Data;
#else
using ApEngine;
using Game;
using Game.Data;
#endif

namespace FellSealAssetLoader.Loaders
{
    [HarmonyPatch]
    public static class WeaponLoader
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
                var loaded = 0;
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Weapons.xml", xml =>
                {
                    _context.UpdateWeaponsFromFile(null, xml, ServiceProvider.GetInstance().Get<TermsDictionary>());
                    loaded++;
                });
                Melon<AssetLoaderMod>.Logger.Msg($"Loaded {loaded} file{(loaded == 1 ? "" : "s")}");
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
                FileChecker.Add((path, result) =>
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
}
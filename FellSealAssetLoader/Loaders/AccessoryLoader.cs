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
    public static class AccessoryLoader
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
                    FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Accessories.xml", xml =>
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
                    FileChecker.Add((path, result) =>
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
}
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;

#if NET6_0
using Il2CppSystem.Collections.Generic;
using Il2CppApEngine;
using Il2CppGame;
using Il2CppGame.Data;
#else
using System.Collections.Generic;
using ApEngine;
using Game;
using Game.Data;
#endif

namespace FellSealAssetLoader.Loaders
{
    [HarmonyPatch]
    public static class CraftingLoader
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
                var dict = new Dictionary<string, Recipe>();
                foreach (var recipe in _context.mRecipes)
                {
                    dict[recipe.mItemHash.ToUpperInvariant()] = recipe;
                }
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Crafting.xml", xml =>
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
                FileChecker.Add((path, result) =>
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
}
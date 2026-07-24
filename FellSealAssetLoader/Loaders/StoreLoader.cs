using System.Collections.Generic;
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
    public static class StoreLoader
    {
        internal static string CopyToLaterStoreNumbers => nameof(CopyToLaterStoreNumbers);
        internal static string CopyToLaterStoreIndices => nameof(CopyToLaterStoreIndices);
        private static readonly List<Store> ToCopyStore = new List<Store>();
        private static readonly List<Store> ToCopyIndex = new List<Store>();
        private static Stores _context;
        private static bool _needLoad;
        private static bool _loadingDLC;

        [AssetInternalInit]
        public static void Init()
        {
            Melon<AssetLoaderMod>.Logger.Msg("Hooking StoreNumber and StoreIndex copier");
            AssetLoaderEvents.DatabaseInit += db =>
            {
                foreach (var source in ToCopyStore)
                {
                    var alsoIndex = ToCopyIndex.Contains(source);
                    foreach (var target in db.GetStoresDb().mStores)
                    {
                        if (target.mMapNode > source.mMapNode && (target.mStoryIndex == source.mStoryIndex || (alsoIndex && target.mStoryIndex > source.mStoryIndex)))
                        {
                            if (source.add)
                            {
                                db.GetStoresDb().FuseStrings(target, source);
                            }
                            else
                            {
                                target.mItems = source.mItems;
                            }
                        }
                    }
                }
                
                foreach (var source in ToCopyIndex)
                {
                    foreach (var target in db.GetStoresDb().mStores)
                    {
                        if (target.mMapNode == source.mMapNode && target.mStoryIndex > source.mStoryIndex)
                        {
                            if (source.add)
                            {
                                db.GetStoresDb().FuseStrings(target, source);
                            }
                            else
                            {
                                target.mItems = source.mItems;
                            }
                        }
                    }
                }

                ToCopyIndex.Clear();
                ToCopyStore.Clear();
            };
        }

        private static void LoadFromContext()
        {
            if (_context != null)
            {
                _needLoad = false;
                Melon<AssetLoaderMod>.Logger.Msg("Loading custom stores");
                var loaded = 0;
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Stores.xml", xml =>
                {
                    _context.LoadAddedData(null, xml, ServiceProvider.GetInstance().Get<LocManager>(), ServiceProvider.GetInstance().Get<TermsDictionary>());
                    loaded++;
                });
                Melon<AssetLoaderMod>.Logger.Msg($"Loaded {loaded} file{(loaded == 1 ? "" : "s")}");
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
                FileChecker.Add((path, result) =>
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

        [HarmonyPatch(typeof(Store), nameof(Store.FixFromXml))]
        public static class PrepareToCopy
        {
            public static void Postfix(Store __instance)
            {
                if (__instance.GetCustomAttributes(out var attr))
                {
                    if (attr.TryGetValue(CopyToLaterStoreIndices, out var val1) && val1.Equals("true"))
                    {
                        ToCopyIndex.Add(__instance);
                    }
                    if (attr.TryGetValue(CopyToLaterStoreNumbers, out var val2) && val2.Equals("true"))
                    {
                        ToCopyStore.Add(__instance);
                    }
                }
            }
        }
    }
}
using System;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;

#if NET6_0
using Il2CppApEngine;
using Il2CppGame;
using Il2CppGame.Data.DLC1;
#else
using ApEngine;
using Game;
using Game.Data.DLC1;
#endif

namespace FellSealAssetLoader.Loaders
{
    [HarmonyPatch]
    public static class MissionLoader
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
                var loaded = 0;
                TermsDictionary termsDictionary = ServiceProvider.GetInstance().Get<TermsDictionary>();
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Missions.xml", xml =>
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
                        loaded++;
                    }
                    catch (Exception ex)
                    {
                        Melon<AssetLoaderMod>.Logger.Msg($"There was an error parsing file: {xml} with exception: {ex}");
                        _context.mErrorLoadingFiles = $"{_context.mErrorLoadingFiles}\n{termsDictionary.ParseStringWithArgs(_context.mLocManager.GetTermNoColors("options-error-file"), xml)}";
                    }
                });
                Melon<AssetLoaderMod>.Logger.Msg($"Loaded {loaded} file{(loaded == 1 ? "" : "s")}");
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
                FileChecker.Add((path, result) =>
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
}
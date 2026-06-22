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
    public static class AbilityLoader
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
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Abilities.xml", xml =>
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
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Jobs.xml", xml =>
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
                FileChecker.Add((path, result) =>
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
                FileChecker.Add((path, result) =>
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
}
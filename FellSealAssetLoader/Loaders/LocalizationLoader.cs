using System.IO;
using System.Linq;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;

#if NET6_0
using Il2CppGame;
using IniFile = Il2CppApEngine.IniFile;
#else
using Game;
using IniFile = ApEngine.IniFile;
#endif

namespace FellSealAssetLoader.Loaders
{
    [HarmonyPatch]
    public static class LocalizationLoader
    {
        private static IniFile _iniContext;
        private static LocManager _locContext;
        private static LocManager.LocInformation[] _files;
        private static int _index;
        private static bool _needsLoading;

        private static void LoadFromContext()
        {
            if (_iniContext != null && _locContext != null && _files != null && _files.Length > _index)
            {
                _needsLoading = false;
                Melon<AssetLoaderMod>.Logger.Msg("Loading custom "+_files[_index].mFile.Split('.').FirstOrDefault()+" localization");
                var loaded = 0;
                FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, Path.Combine("languages",_locContext.mLanguageCode,_files[_index].mFile), txt =>
                {
                    var other = new IniFile
                    {
                        TheFile = txt
                    };
                    _iniContext.UpdateWith(other);
                    loaded++;
                });
                Melon<AssetLoaderMod>.Logger.Msg($"Loaded {loaded} file{(loaded == 1 ? "" : "s")}");
                _index++;
            }
            else
            {
                Melon<AssetLoaderMod>.Logger.Msg("No context for custom localization");
            }
        }
            
        [HarmonyPatch(typeof(LocManager), nameof(LocManager.LoadLanguage))]
        public static class Prep
        {
            public static void Prefix(LocManager __instance)
            {

                //Melon<ModFile>.Logger.Msg("LocManager.LoadLanguage begins, hold context");
                _locContext = __instance;
                _files = __instance.mFiles;
                _index = 0;
            }

            public static void Finalizer()
            {
                //Melon<ModFile>.Logger.Msg("LocManager.LoadLanguage ends, release context");
                _iniContext = null;
                _locContext = null;
                _files = null;
            }
        }

        [HarmonyPatch(typeof(LocManager), nameof(LocManager.LoadBaseFile))]
        public static class GrabIni
        {
            public static void Postfix(IniFile file)
            {
                if (_locContext != null && _files != null)
                {
                    _iniContext = file;
                    _needsLoading = true;
                    FileChecker.Add((path, result) =>
                    {
                        if (!result)
                        {
                            LoadFromContext();
                        }
                        return true;
                    });
                }
            }
        }

        [HarmonyPatch(typeof(IniFile), nameof(IniFile.UpdateWith))]
        public static class UpdateIfNotHandled
        {
            public static void Postfix(IniFile __instance)
            {
                if (__instance == _iniContext && _needsLoading)
                {
                    LoadFromContext();
                }
            }
        }
    }
}
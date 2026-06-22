using System;
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
    public static class ConsumableLoader
    {
        [HarmonyPatch(typeof(Consumables), nameof(Consumables.Load))]
            public static class Prep
            {
                public static void Postfix(Consumables __instance)
                {
                    Melon<AssetLoaderMod>.Logger.Msg("Loading custom crafting");
                    TermsDictionary termsDictionary = ServiceProvider.GetInstance().Get<TermsDictionary>();
                    FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, "Consumables.xml", xml =>
                    {
                        try
                        {
                            XMLConsumables xmlConsumables = XmlLoader.LoadFromFile<XMLConsumables>(xml);
                            xmlConsumables.FixUp();
                            for (int index = 0; index < xmlConsumables.mConsumables.Count; ++index)
                            {
                                Consumable con = xmlConsumables.mConsumables[index];
                                con.name = __instance.mLocManager.GetTermNoColors(con.description);
                                con.names = __instance.GetNames(con.description);
                                con.description = __instance.mLocManager.GetTerm(con.description + "-desc");
                                string upperInvariant = con.hashName.ToUpperInvariant();
                                if (__instance.mConsumableDict.TryGetValue(upperInvariant, out var found))
                                    __instance.mAllConsumables.Remove(found);
                                __instance.mAllConsumables.Add(con);
                                __instance.mConsumableDict[upperInvariant] = con;
                            }
                        }
                        catch (Exception ex)
                        {
                            Melon<AssetLoaderMod>.Logger.Error($"There was an error parsing file: {xml} with exception: {ex}");
                            __instance.mErrorLoadingFiles = $"{__instance.mErrorLoadingFiles}\n{termsDictionary.ParseStringWithArgs(__instance.mLocManager.GetTermNoColors("options-error-file"), xml)}";
                        }
                    });
                }
            }
    }
}
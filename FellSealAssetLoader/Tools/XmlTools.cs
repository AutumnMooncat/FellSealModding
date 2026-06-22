using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2CppSystem.Xml.Serialization;
#else
using System.IO;
using System.Xml.Serialization;
#endif

// XML Ext (Wip, fully custom) 
namespace FellSealAssetLoader.Tools
{
    #if NET6_0
    [HarmonyPatch(typeof(XmlSerializer), nameof(XmlSerializer.OnUnknownAttribute))]
    public static class XmlTools
    {
        public static void Prefix(XmlAttributeEventArgs e)
        {
            Melon<AssetLoaderMod>.Logger.Msg($"Got unknown attribute {e.attr.Name}:{e.attr.Value} when Deserializing {e.o}");
            if (!AssetLoaderMod.CustomAttributes.ContainsKey(e.o))
            {
                AssetLoaderMod.CustomAttributes[e.o] = new Dictionary<string, object>();
            }
            AssetLoaderMod.CustomAttributes[e.o][e.attr.Name] = e.attr.Value;
        }
    }
    #else
    [HarmonyPatch(typeof(XmlSerializer), nameof(XmlSerializer.Deserialize), typeof(TextReader))]
    public static class XmlTools
    {
        public static void Prefix(XmlSerializer __instance)
        {
            __instance.UnknownAttribute += (sender, args) =>
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Got unknown attribute {args.Attr.Name}:{args.Attr.Value} when Deserializing {args.ObjectBeingDeserialized}");
                if (!AssetLoaderMod.CustomAttributes.ContainsKey(args.ObjectBeingDeserialized))
                {
                    AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized] =
                        new Dictionary<string, object>();
                }

                AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized][args.Attr.Name] =
                    args.Attr.Value;
            };
        }
    }
    #endif
}

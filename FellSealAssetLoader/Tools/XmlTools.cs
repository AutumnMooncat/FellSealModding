using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using System.Linq;
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
            Melon<AssetLoaderMod>.Logger.Msg($"Got custom attribute {e.attr.Name}:{e.attr.Value} when Deserializing {e.o}");
            if (!AssetLoaderMod.CustomAttributes.ContainsKey(e.o))
            {
                AssetLoaderMod.CustomAttributes[e.o] = new Dictionary<string, HashSet<string>>();
            }
            AssetLoaderMod.CustomAttributes[e.o][e.attr.Name] = e.attr.Value.Split(' ').ToHashSet();
            AssetLoaderEvents.GotXmlCustomAttribute(e.o, e);
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
                Melon<AssetLoaderMod>.Logger.Msg($"Got custom attribute {args.Attr.Name}:{args.Attr.Value} when Deserializing {args.ObjectBeingDeserialized}");
                if (!AssetLoaderMod.CustomAttributes.ContainsKey(args.ObjectBeingDeserialized))
                {
                    AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized] =
                        new Dictionary<string, HashSet<string>>();
                }

                AssetLoaderMod.CustomAttributes[args.ObjectBeingDeserialized][args.Attr.Name] = args.Attr.Value.Split(' ').ToHashSet();
                AssetLoaderEvents.GotXmlCustomAttribute(args.ObjectBeingDeserialized, args);
            };
            __instance.UnknownElement += (sender, args) =>
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Got unknown element {args.Element.Name}:{args.Element} but expected {args.ExpectedElements} when Deserializing {args.ObjectBeingDeserialized}");
            };
            __instance.UnknownNode += (sender, args) =>
            {
                Melon<AssetLoaderMod>.Logger.Msg($"Got unknown node {args.Name}:{args.Text} when Deserializing {args.ObjectBeingDeserialized}");
            };
            __instance.UnreferencedObject += (sender, args) =>
            {
                Melon<AssetLoaderMod>.Logger.BigError($"Got unreferenced object {args.UnreferencedId} -> {args.UnreferencedObject}, we might explode now");
            };
        }
    }
    #endif
}

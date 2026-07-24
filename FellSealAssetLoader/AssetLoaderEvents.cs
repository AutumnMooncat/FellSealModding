using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2Cpp;
using Il2CppGame.Data;
using Il2CppSystem.Xml.Serialization;
#else
using Game.Data;
using System.Xml.Serialization;
#endif

namespace FellSealAssetLoader
{
    [HarmonyPatch]
    public class AssetLoaderEvents
    {
        public delegate void OnDatabaseInit(Database db);
        public static event OnDatabaseInit DatabaseInit;

        public delegate void OnXmlCustomAttribute(object o, XmlAttributeEventArgs args);
        public static event OnXmlCustomAttribute XmlCustomAttribute;

        public static void GotXmlCustomAttribute(object o, XmlAttributeEventArgs args)
        {
            XmlCustomAttribute?.Invoke(o, args);
        }
        
        [HarmonyPatch(typeof(Database), nameof(Database.CreateInstance))]
        public static class PostDatabaseCreate
        {
            public static void Postfix(Database __result)
            {
                Melon<AssetLoaderMod>.Logger.WriteSpacer();
                Melon<AssetLoaderMod>.Logger.Msg("Running PostDatabaseCreate");
                DatabaseInit?.Invoke(__result);
            }
        }
    }
}
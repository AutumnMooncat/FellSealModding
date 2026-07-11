using HarmonyLib;

#if NET6_0
using Il2Cpp;
using Il2CppGame.Data;
#else
using Game.Data;
#endif

namespace FellSealAssetLoader
{
    [HarmonyPatch]
    public class AssetLoaderEvents
    {
        public delegate void OnDatabaseInit(Database db);

        public static event OnDatabaseInit DatabaseInit;
        
        [HarmonyPatch(typeof(Database), nameof(Database.CreateInstance))]
        public static class PostDatabaseCreate
        {
            public static void Postfix(Database __result)
            {
                DatabaseInit?.Invoke(__result);
            }
        }
    }
}
using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using OrdinaryOrdnance;

#if NET6_0
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Constants = Il2CppSpriteEngine.Constants;
#else
using Game;
using Game.Data;
using Game.Battle;
using Constants = SpriteEngine.Constants;
#endif

[assembly: MelonInfo(typeof(OrdnanceMod), "Ordinary Ordnance", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace OrdinaryOrdnance
{
    public class OrdnanceMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Ordinary Ordnance initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static string VialAttack = "VIAL-ATK-";
        public static WeaponRegistry VialWeapon;
        
        [AssetInit]
        public static void Init()
        {
            VialWeapon = RegistryTools.RegisterWeaponsType("Vial", "icon-vial-group", WeaponRegistry.Handedness.TrueTwoHanded)
                .WithJobs(
                    GameConstants.Jobs.Peddler, 
                    GameConstants.Jobs.Alchemystic, 
                    GameConstants.Jobs.Anatomist, 
                    GameConstants.Jobs.PlagueDoctor,
                    GameConstants.Jobs.Druid)
                .WithSounds(Sounds.Weapons.kStaff_Miss, 0.4f, Sounds.Weapons.kStaff_Miss, 0.4f)
                .WithMinimumRange(0)
                .WithSelfTargeting()
                .WithAttackHash(caster =>
                {
                    var weapon = caster.character.GetWeapon(0);
                    var hash = VialAttack + weapon.icon.ToUpperInvariant();
                    //Melon<OrdnanceMod>.Logger.Msg($"Looking for attack hash {hash}");
                    if (Database.GetInstance() is Database db && db.GetSpellEffects().mAllSpellsByHash.ContainsKey(hash))
                    {
                        return hash;
                    }
                    return null;
                });

            AssetLoaderEvents.DatabaseInit += db =>
            {
                Melon<OrdnanceMod>.Logger.Msg($"Creating spell effects");
                db.GetSpellEffects().AddSpell(VialAttack+"icon-elixir", MakeUseEffect("icon-elixir"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-solvent", MakeThrowEffect("icon-solvent"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-bottledsmoke", MakeThrowEffect("icon-bottledsmoke"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-iceessence", MakeThrowEffect("icon-iceessence"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-flameessence", MakeThrowEffect("icon-flameessence"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-stoneessence", MakeThrowEffect("icon-stoneessence"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-boltessence", MakeThrowEffect("icon-boltessence"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-mysteryglue", MakeThrowEffect("icon-mysteryglue"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-holywater", MakeThrowEffect("icon-holywater"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-unholywater", MakeThrowEffect("icon-unholywater"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-ingot", MakeThrowEffect("icon-ingot"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-clearlysafeliquid", MakeThrowEffect("icon-clearlysafeliquid"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-chemx", MakeThrowEffect("icon-chemx"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-slimegoo", MakeThrowEffect("icon-slimegoo"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-tarjar", MakeThrowEffect("icon-tarjar"));
                db.GetSpellEffects().AddSpell(VialAttack+"icon-sweetdreams", MakeThrowEffect("icon-sweetdreams"));
            };
        }

        public static SpellEffect MakeThrowEffect(string icon)
        {
            return new SpellEffect()
                .LoadEffectName("MenuGeneric", icon)
                .PlaySpell("IT-OPENER2", true)
                .ShowDamage(0)
                .CleanupTarget()
                .CleanupCaster();
        }

        public static SpellEffect MakeUseEffect(string icon)
        {
            return new SpellEffect()
                .LoadEffectName("MenuGeneric", icon)
                .PlaySpell("IT-OPENER", true)
                .LoadEffect("sparkles", "flash", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true)
                .PlaySound(Sounds.Spells.kGlitterFlash, volume: 0.5f)
                .ShowFrame(SpellEffect.Target.kTarget, Constants.Action.kStand)
                .Wait(0.4f)
                .ShowDamage(0)
                .WaitOn(SpellEffect.Target.kBank0)
                .CleanupTarget()
                .CleanupCaster();
        }
    }
}
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
            LoggerInstance.Msg("Ordinary Ordnance Melon Initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        private const string VialAttack = "VIAL-ATK-";
        private static readonly WeaponRegistry VialWeapon = RegistryTools.RegisterWeaponsType("Vial", "icon-vial-group", WeaponRegistry.Handedness.TrueTwoHanded);
        
        [AssetInit]
        public static void Init()
        {
            VialWeapon
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
                var spellEffects = db.GetSpellEffects();
                spellEffects.AddSpell(VialAttack+"icon-elixir", MakeUseEffect("icon-elixir"));
                spellEffects.AddSpell(VialAttack+"icon-solvent", 
                    ThrowEffectOpener("icon-solvent")
                        .GettingHit()
                        .LoadEffect("WrathfulBlowGastricAcid", "target", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3)
                        .PlaySound(Sounds.Spells.kGastricHit)
                        .Wait(0.3f)
                        .ShowDamage(0)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-bottledsmoke", 
                    ThrowEffectOpener("icon-bottledsmoke")
                        .LoadEffect("powderbomb", "powder", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .ColorEffect(spellEffects.kColorDarkener, SpellEffect.Target.kBank2)
                        .ScaleSpriteNow(SpellEffect.Target.kBank2, 0.6f, 0.6f)
                        .PlaySound(Sounds.Spells.kSmallSmoke)
                        .GettingHit()
                        .ShowNonBlockingDamage()
                        .WaitOn(SpellEffect.Target.kBank2)
                        .ResetFrames(SpellEffect.Target.kCaster)
                        .WaitDamage()
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-iceessence", 
                    ThrowEffectOpener("icon-iceessence")
                        .PlaySound(Sounds.Spells.kWaterBurst)
                        .LoadEffect("WaterBurst", "secondaryfront", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .LoadEffect("WaterBurst", "secondaryfront", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3, loopMode: Constants.LoopType.kReverseOnce)
                        .GettingHit()
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-flameessence", 
                    ThrowEffectOpener("icon-flameessence")
                        .PlaySound(Sounds.Spells.kFireBurst)
                        .LoadEffect("FireBurst", "ring", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank3)
                        .LoadEffect("FireBurst", "oldfire", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlusBank, true, bank: SpellEffect.Target.kBank4)
                        .LoadEffect("FireBurst", "spark", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlusBank, true, bank: SpellEffect.Target.kBank5)
                        .LoadEffect("FireBurst", "explosion3", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlusBank, true, bank: SpellEffect.Target.kBank6)
                        .LoadEffect("FireBurst", "explosion2", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlusBank, true, bank: SpellEffect.Target.kBank7)
                        .LoadEffect("FireBurst", "explosion1", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlusBank, true, bank: SpellEffect.Target.kBank8)
                        .PlaySound(Sounds.Spells.kFireBoltExplosion)
                        .GettingHit()
                        .Wait(0.7f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .WaitOn(SpellEffect.Target.kBank4)
                        .WaitOn(SpellEffect.Target.kBank5)
                        .WaitOn(SpellEffect.Target.kBank6)
                        .WaitOn(SpellEffect.Target.kBank7)
                        .WaitOn(SpellEffect.Target.kBank8)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-stoneessence", 
                    ThrowEffectOpener("icon-stoneessence")
                        .PlaySound(Sounds.Spells.kEarthBurst)
                        .LoadEffect("EarthBurst", "secondaryfront", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("EarthBurst", "secondaryBack", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank3)
                        .GettingHit()
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-boltessence", 
                    ThrowEffectOpener("icon-boltessence")
                        .PlaySound(Sounds.Spells.kThunderBurst)
                        .LoadEffect("LightingBurst", "secondaryfront", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("LightingBurst", "secondaryBack", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank3)
                        .GettingHit()
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-mysteryglue", 
                    ThrowEffectOpener("icon-mysteryglue")
                        .LoadEffect("Root", "RootBack", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("Root", "RootFront", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3)
                        .PlaySound(Sounds.Spells.kRooting, 0.1f)
                        .Wait(0.4f)
                        .GettingHit()
                        .Wait(0.8f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-holywater", 
                    ThrowEffectOpener("icon-holywater")
                        .LoadEffect("holy1", "back", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("holy1", "front", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3)
                        .PlaySound(Sounds.Spells.kHoly1)
                        .Wait(0.5f)
                        .GettingHit()
                        .Wait(0.4f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-unholywater", 
                    ThrowEffectOpener("icon-unholywater")
                        .LoadEffect("All-dark", "Dark01back01", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("All-dark", "Dark01front01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3)
                        .PlaySound(Sounds.Spells.kDark1)
                        .Wait(1f)
                        .GettingHit()
                        .Wait(0.4f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-ingot", 
                    new SpellEffect()
                        .LoadEffectName("MenuGeneric", "icon-ingot")
                        .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kAtk, Constants.LoopType.kOnce)
                        .LookAt(SpellEffect.Target.kCaster)
                        .CenterBetween(SpellEffect.Target.kCaster, SpellEffect.Target.kTarget, 1f)
                        .Wait(0.5f)
                        .Projectile(string.Empty, string.Empty, SpellEffect.Target.kCaster, SpellEffect.Target.kTarget, 74f, 74f, 88f, 202f, 190f, useLoadedEffect: 0)
                        .ProjectileMove(SpellEffect.Target.kCaster, SpellEffect.Target.kTarget, 0, 0.0f, 120f)
                        .PlaySound(Sounds.Spells.kThrowLight)
                        .WaitOnMovement(SpellEffect.Target.kBankProjectile)
                        .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kStand)
                        .HideFrame(SpellEffect.Target.kBankProjectile, 0.1f)
                        .PlaySound(Sounds.Spells.kSlamIntoThing)
                        .LoadEffect("DustCloudAA", "back", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank0, durationMultiplier: 0.5f)
                        .LoadEffect("DustCloudAA", "front", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigherPlus1, true, bank: SpellEffect.Target.kBank1, durationMultiplier: 0.5f)
                        .GettingHit()
                        .ShowDamage(0)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-clearlysafeliquid", 
                    ThrowEffectOpener("icon-clearlysafeliquid")
                        .LoadEffect("poison", "poison01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .PlaySound(Sounds.Spells.kPoison)
                        .Wait(0.4f)
                        .GettingHit()
                        .Wait(1.1f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-chemx", 
                    ThrowEffectOpener("icon-chemx")
                        .LoadEffectOnTile("Barrage", "Explosion", 0, 0, 0, 0, 0.0f, SpellEffect.RelativePosition.kSpellCenter, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .PlaySound(Sounds.Spells.kBigExplosion)
                        .GettingHit()
                        .Wait(0.4f)
                        .ShowDamage(0)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-slimegoo", 
                    ThrowEffectOpener("icon-slimegoo")
                        .LoadEffect("slow", "slow01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .PlaySound(Sounds.Spells.kSlow)
                        .Wait(1.1f)
                        .GettingHit()
                        .Wait(0.8f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-tarjar", 
                    ThrowEffectOpener("icon-tarjar")
                        .LoadEffect("weaken", "weaken", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank2)
                        .PlaySound(Sounds.Spells.kWeaken)
                        .Wait(0.6f)
                        .GettingHit()
                        .Wait(0.2f)
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
                spellEffects.AddSpell(VialAttack+"icon-sweetdreams", 
                    ThrowEffectOpener("icon-sweetdreams")
                        .LoadEffect("Sleeppowder", "Sleep PowderBack01", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank2)
                        .LoadEffect("Sleeppowder", "Sleep PowderFront01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, bank: SpellEffect.Target.kBank3)
                        .PlaySound(Sounds.Spells.kSleepButterfly)
                        .GettingHit()
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank2)
                        .WaitOn(SpellEffect.Target.kBank3)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                );
            };
        }

        public static SpellEffect ThrowEffectOpener(string icon)
        {
            return new SpellEffect()
                .LoadEffectName("MenuGeneric", icon)
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kAtk, Constants.LoopType.kOnce)
                .LookAt(SpellEffect.Target.kCaster)
                .CenterBetween(SpellEffect.Target.kCaster, SpellEffect.Target.kTarget, 1f)
                .Wait(0.5f)
                .Projectile(string.Empty, string.Empty, SpellEffect.Target.kCaster, SpellEffect.Target.kSpellCenter, 74f, 74f, 88f, 202f, 190f, useLoadedEffect: 0)
                .ProjectileMove(SpellEffect.Target.kCaster, SpellEffect.Target.kSpellCenter, 0, 0.0f, 120f)
                .PlaySound(Sounds.Spells.kThrowLight)
                .WaitOnMovement(SpellEffect.Target.kBankProjectile)
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kStand)
                .HideFrame(SpellEffect.Target.kBankProjectile, 0.1f)
                .PlaySound(Sounds.Spells.kHourglassBreak)
                .LoadEffect("DustCloudAA", "back", SpellEffect.Target.kSpellCenter, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank0, durationMultiplier: 0.5f)
                .LoadEffect("DustCloudAA", "front", SpellEffect.Target.kSpellCenter, SpellEffect.Priority.kHigherPlus1, true, bank: SpellEffect.Target.kBank1, durationMultiplier: 0.5f);
        }

        public static SpellEffect MakeUseEffect(string icon)
        {
            return new SpellEffect()
                .LoadEffectName("MenuGeneric", icon)
                .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kJumpMidair, 0)
                .LookAt(SpellEffect.Target.kCaster)
                .CenterCameraOn(SpellEffect.Target.kCaster, extraHeight: 1f)
                .WaitOnCamera()
                .LoadEffectOnTile(string.Empty, string.Empty, 0, 0, 0, 0, 3f, SpellEffect.RelativePosition.kCaster, SpellEffect.Priority.kHigher, false, useLoadedEffect: 0)
                .MoveEffect(SpellEffect.Target.kBank0, 0, 0, 4.5f, SpellEffect.RelativePosition.kCaster, 0.4f)
                .PlaySound(Sounds.Spells.kItemMoveUp, volume: 0.8f)
                .FadeFrameAndWait(SpellEffect.Target.kBank0, 0.0f, 1f, 0.2f)
                .Wait(0.35f)
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kStand)
                .FadeFrameAndWait(SpellEffect.Target.kBank0, 1f, 0.0f, 0.2f)
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
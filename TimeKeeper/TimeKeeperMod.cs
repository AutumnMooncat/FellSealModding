using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using TimeKeeper;

#if NET6_0
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Constants = Il2CppSpriteEngine.Constants;
using Il2CppSystem.Collections.Generic;

#else
using Game;
using Game.Data;
using Game.Battle;
using Constants = SpriteEngine.Constants;
using System.Collections.Generic;
#endif

[assembly: MelonInfo(typeof(TimeKeeperMod), "TimeKeeper Mod", "0.0.1", "")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace TimeKeeper
{
    public class TimeKeeperMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("TimeKeeper initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static ExtraBoxRegistry LightspeedBox;
        private static Abilities.Ability _lightspeedBase;
        
        [AssetInit]
        public static void Init()
        {
            LightspeedBox = RegistryTools.RegisterExtraBox(nameof(LightspeedBox))
                .WithOnSpawn(manager =>
                {
                    manager.mCustomSpells = FindAllLightspeedSpells(manager.mCurrentActor);
                    if (manager.mCustomSpells.Count == 0)
                    {
                        //Melon<TimeKeeperMod>.Logger.Msg($"No Valid Spells Found :(");
                        return false;
                    }
                    //Melon<TimeKeeperMod>.Logger.Msg($"Found {manager.mCustomSpells.Count} valid spells for Lightspeed");
                    _lightspeedBase = manager.mCurrentAction.mAbility;
                    manager.SpawnAbilitiesWithMpCostRemoved(manager.mCustomSpells, CommandBox.AbilityType.kCustom, manager.mCurrentAction.mAbility.manaCost);
                    return true;
                })
                .WithOnProcess((manager, index) =>
                {
                    var original = Database.GetInstance().GetAbility(index);
                    var lightspeeded = original.Clone("WORK-LIGHTSPEED" + index);
                    lightspeeded.effectName = original.effectName;
                    lightspeeded.name = $"{_lightspeedBase.name} + {original.name}";
                    lightspeeded.shape = _lightspeedBase.shape;
                    lightspeeded.size = _lightspeedBase.size;
                    lightspeeded.range = _lightspeedBase.range;
                    lightspeeded.minRange = _lightspeedBase.minRange;
                    lightspeeded.rangeHeight = _lightspeedBase.rangeHeight;
                    lightspeeded.manaCost += _lightspeedBase.manaCost;
                    manager.mCurrentAction = BattleManager.ActionInformation.LoadAbility(manager.mCurrentActor, lightspeeded);
                });
            AssetLoaderEvents.DatabaseInit += db =>
            {
                Melon<TimeKeeperMod>.Logger.Msg($"Creating spell effects");
                var spellEffects = db.GetSpellEffects();
                spellEffects.AddSpell("TIME-A4", new SpellEffect()
                        .Casting()
                        .ShowFrame(SpellEffect.Target.kTarget, Constants.Action.kStand)
                        .LoadEffect("HourglassBreak", "HourglassBreakFront01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true)
                        .LoadEffect("HourglassBreak", "HourglassBreakBack01", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, bank: SpellEffect.Target.kBank1)
                        .PlaySound(Sounds.Spells.kHourglassBreak, 0.3f)
                        .Wait(0.5f)
                        .GettingHit()
                        .ShowDamage(0)
                        .WaitOn(SpellEffect.Target.kBank0)
                        .WaitOn(SpellEffect.Target.kBank1)
                        .ResetFrames(SpellEffect.Target.kCaster)
                        .Wait(0.1f)
                        .CleanupTarget()
                        .CleanupCaster()
                    );
                spellEffects.AddSpell("TIME-A5", new SpellEffect()
                    .Casting()
                    .PlaySound(Sounds.Battle.kJump)
                    .ShowFrame(SpellEffect.Target.kTarget, Constants.Action.kStand)
                    .MoveSprite(SpellEffect.Target.kTarget, 0, -9999, 1f)
                    .SetFlyingFlag(SpellEffect.Target.kTarget, true)
                    .ScaleSpriteShadow(SpellEffect.Target.kTarget, 0.0f, 1f - 0.1f, true)
                    .Wait(1f)
                    .HideFrame(SpellEffect.Target.kTarget)
                    .CenterCameraOn(SpellEffect.Target.kTarget)
                    .WaitOnCamera()
                    .PlaySound(Sounds.Spells.kSkyjack)
                    .MoveSprite(SpellEffect.Target.kTarget, 0, -9999, 0.0f)
                    .ShowFrame(SpellEffect.Target.kTarget, Constants.Action.kGetHit)
                    .MoveSprite(SpellEffect.Target.kTarget, 0, 0, 0.65f)
                    .ScaleSpriteShadow(SpellEffect.Target.kTarget, 1f, 0.65f - 0.1f, true)
                    .Wait(0.65f)
                    .StopSound(0.1f)
                    .PlaySound(Sounds.Events.kLandFromHigh)
                    .GettingHit()
                    .MoveSprite(SpellEffect.Target.kTarget, 0, -44, 0.16f)
                    .Wait(0.16f)
                    .MoveSprite(SpellEffect.Target.kTarget, 0, 0, 0.12f)
                    .Wait(0.12f)
                    .SetFlyingFlag(SpellEffect.Target.kTarget, false)
                    .ShowDamage(0)
                    .CenterCameraOn(SpellEffect.Target.kCaster)
                    .ResetFrames(SpellEffect.Target.kCaster)
                    .ResetFrames(SpellEffect.Target.kTarget)
                    .WaitOnCamera()
                    .CleanupTarget()
                    .CleanupCaster()
                );
            };
        }

        public static List<BattleCharacter.AbilityDisplayData> FindAllLightspeedSpells(BattleCharacter character)
        {
            var ret = new List<BattleCharacter.AbilityDisplayData>();
            var allData = character.GetAbilitiesDisplayList();
            foreach (var data in allData)
            {
                var ability = Database.GetInstance().GetAbility(data.index);
                if (SpellIsLightspeedable(ability))
                {
                    ret.Add(data);
                }
            }
            return ret;
        }

        public static bool SpellIsLightspeedable(Abilities.Ability ability)
        {
            //Melon<TimeKeeperMod>.Logger.Msg($"Checking ability {ability.name}: No Shenanigans? {ability.NoShenanigans()}, Type? {ability.isASpell}, Min Range? {ability.minRange}");
            return ability.NoShenanigans() && ability.IsASpell() == Abilities.SpellType.kSpell && ability.range >= 1;
        }
    }
}
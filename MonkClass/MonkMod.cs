using FellSealAssetLoader;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MonkClass;

#if NET6_0
using Il2Cpp;
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

[assembly: MelonInfo(typeof(MonkMod), "Monk Class", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace MonkClass
{
    public class MonkMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Monk Class Melon Initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static readonly string FlurryOfBlows = nameof(FlurryOfBlows);
        public static readonly string ExtraTurn = nameof(ExtraTurn);

        private static readonly Context<DamageCalculations> DamageCalcApplyCtx =
            ContextTools.RequestLateContext<DamageCalculations>(nameof(DamageCalculations.Apply), typeof(List<BattleCharacter>), typeof(BattleCharacter), typeof(DamageCalculations.DamageResult));

        private static readonly CommandRegistry FlurryOfBlowsCmd = 
            RegistryTools.RegisterCommandOption(FlurryOfBlows, $"ability-{FlurryOfBlows}", $"ability-{FlurryOfBlows}-desc");

        private static readonly CommandRegistry FlurryOfBlowsSelectionCmd = 
            RegistryTools.RegisterSubCommandOption($"{FlurryOfBlows}Selection");
        
        [AssetInit]
        public static void Init()
        {
            FlurryOfBlowsCmd
                .WithShouldAppear(box => box.mCharacter.character.CustomEffects().Contains(FlurryOfBlows))
                .WithOnSelect(manager =>
                {
                    var allRegularAttacks = manager.FindAllRegularAttacks(manager.mCurrentActor);
                    if (allRegularAttacks.Count == 0)
                    {
                        return false;
                    }
                    manager.SpawnAbilitiesWithMpCostRemoved(allRegularAttacks, FlurryOfBlowsSelectionCmd.type, 10);
                    return true;
                });
            
            FlurryOfBlowsSelectionCmd
                .WithOnSelect(manager => {
                    var index = manager.mCommandBox.GetAbilityIndex();
                    if (index < 0)
                    {
                        var fob = Database.GetInstance().GetAbility("WORK-FLURRYOFBLOWS");
                        manager.mCurrentAction = BattleManager.ActionInformation.LoadAbility(manager.mCurrentActor, fob);
                    }
                    else
                    {
                        manager.CreateAction(CommandBox.AbilityType.kAbility, index);
                        var clone = manager.mCurrentAction.mAbility.Clone("WORK-FLURRYOFBLOWS-" + index);
                        clone.name = $"{manager.mLocManager.GetTermNoColors($"ability-{FlurryOfBlows}")} + {clone.name}";
                        clone.manaCost += 10;
                        clone.GetCustomFields()[ExtraTurn] = true;
                        manager.mCurrentAction.mAbility = clone;
                    }
                    if (manager.mCurrentAction.mExtraCommandBox != Abilities.ExtraCommandBox.kNone)
                    {
                        manager.SpawnExtraBox(manager.mCurrentAction.mExtraCommandBox);
                        return true;
                    }
                    manager.QueueAbilityProcess();
                    return true;
                });
            
            DamageCalcApplyCtx 
                    .WithRelease((instance, args, result) =>
                    {
                        var targets = (List<BattleCharacter>)args[0];
                        var caster = (BattleCharacter)args[1];
                        var dmg = (DamageCalculations.DamageResult)args[2];
                        if (dmg.mAbility == null)
                        {
                            return;
                        }
                        if (dmg.mAbility.CustomEffects().Contains(ExtraTurn))
                        {
                            caster.GetCustomFields()[ExtraTurn] = true;
                        }

                        if (dmg.mAbility.GetCustomFields().TryGetValue(ExtraTurn, out var val2) && val2 is bool b && b)
                        {
                            caster.GetCustomFields()[ExtraTurn] = true;
                        }
                    });

            AssetLoaderEvents.DatabaseInit += db =>
            {
                Melon<MonkMod>.Logger.Msg($"Creating spell effects");
                var spellEffects = db.GetSpellEffects();
                spellEffects.AddSpell("MONK-A1", new SpellEffect()
                    .Skilling()
                    .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kAtk, Constants.LoopType.kOnce, 2)
                    .PlaySound(Sounds.Spells.kSiegeRamHit)
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
                    .CleanupCaster());
                spellEffects.AddSpell("MONK-A2", new SpellEffect()
                    .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kJumpCrouch, 0)
                    .LookAt(SpellEffect.Target.kCaster)
                    .Wait(0.25f)
                    .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kAtkSpear, 3)
                    .MoveSprite(SpellEffect.Target.kCaster, 0.6f, 0.15f)
                    .Wait(0.15f)
                    .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kAtkSpear, 4)
                    .PlaySound(Sounds.Spells.kPunt, 0f, Sounds.Spells.kGenericWhoosh)
                    .GettingHit()
                    .ShowNonBlockingDamage()
                    .SpinFrameToFaceCaster(SpellEffect.Target.kTarget, 0.15f, 0, SpellEffect.SpinDirection.kBest)
                    .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kStand)
                    .MoveSprite(SpellEffect.Target.kCaster, 0, 0, 0.2f)
                    .WaitOn(SpellEffect.Target.kCaster)
                    .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kIdle)
                    .Wait(0.15f)
                    .WaitDamage()
                    .Wait(0.1f)
                    .CleanupTarget()
                    .CleanupCaster());
            };
        }
        
        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.EndTurn))]
        public static class EndTurnPatch
        {
            public static void Prefix(BattleManager __instance, out bool __state)
            {
                __state = false;
                var data = __instance.mCurrentActor.GetCustomFields();
                if (data.TryGetValue(ExtraTurn, out var val) && val is bool b && b)
                {
                    __state = true;
                    __instance.mCleaved = true;
                    __instance.mCleavedDone = false;
                    data[ExtraTurn] = false;
                }
            }
            
            public static void Postfix(BattleManager __instance, bool __state)
            {
                if (__state)
                {
                    __instance.mCleavedDone = false;
                }
            }
        }
    }
}
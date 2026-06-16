using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FellSealAssetLoader;
using HarmonyLib;

using MelonLoader;
using MelonLoader.Utils;
using MonkClass;


#if NET6_0
using Il2Cpp;
using Il2CppApEngine;
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Il2CppGame.Data.DLC1;
using IniFile = Il2CppApEngine.IniFile;
using Il2CppSpriteEngine;
using Constants = Il2CppSpriteEngine.Constants;
using Il2CppSystem.Xml.Serialization;
using XmlLoader = Il2CppApEngine.XmlLoader;
#else
using ApEngine;
using Game;
using Game.Data;
using Game.Battle;
using Game.Data.DLC1;
using IniFile = ApEngine.IniFile;
using SpriteEngine;
using Constants = SpriteEngine.Constants;
using System.Xml.Serialization;
#endif

[assembly: MelonInfo(typeof(MonkMod), "Monk Class", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace MonkClass
{
    public class MonkMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Monk Class initializing");
            Patches.Init();
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static void Init()
        {
            Melon<MonkMod>.Logger.Msg("Patch statics initialized");
        }
        
        public static readonly string MonkEffect = nameof(MonkEffect);
        public static readonly string FlurryOfBlows = nameof(FlurryOfBlows);
        public static readonly string ExtraTurn = nameof(ExtraTurn);
        public static readonly CommandBox.AbilityType kFlurryOfBlows = AssetLoaderMod.RequestExtendedEnum<CommandBox.AbilityType>(nameof(kFlurryOfBlows));
        public static readonly CommandBox.AbilityType kFlurryOfBlowsSelection = AssetLoaderMod.RequestExtendedEnum<CommandBox.AbilityType>(nameof(kFlurryOfBlowsSelection));
        public static CommandBox.AbilityType GottenChoice;
        public static CommandBox.Command GottenCommand;
        
        public static readonly Context<BaseCharacter> UpdatePassivesAndGear =
            AssetLoaderMod.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.UpdatePassivesAndGear))
                .WithHold((instance, args) =>
                {
                    AssetLoaderMod.GetCustomData(instance)[FlurryOfBlows] = false;
                });
        
        public static readonly Context<BaseCharacter> EquipPassive =
            AssetLoaderMod.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.EquipPassive), typeof(string))
                .WithRelease((instance, args, result) =>
                {
                    var passive = args[0] as string;
                    if (string.IsNullOrEmpty(passive))
                        return;
                    Abilities.Ability ability = Database.GetInstance().GetAbility(passive);
                    if (AssetLoaderMod.CustomAttributes.TryGetValue(ability, out var attr))
                    {
                        if (attr.TryGetValue(MonkEffect, out var val))
                        {
                            if (val is string s && s == FlurryOfBlows)
                            {
                                Melon<MonkMod>.Logger.Msg($"BaseCharacter.EquipPassive Postfix, applied {FlurryOfBlows}");
                                AssetLoaderMod.GetCustomData(instance)[FlurryOfBlows] = true;
                            }
                        }
                    }
                });
        
        public static readonly Context<BattleManager> ProcessInput =
            AssetLoaderMod.RequestContext<BattleManager>(nameof(BattleManager.ProcessInput))
                .WithRelease((instance, args, result) =>
                {
                    if (GottenChoice == CommandBox.AbilityType.kNone)
                    {
                        return;
                    }
                    Melon<MonkMod>.Logger.Msg($"BattleManger ProcessInput got choice {GottenChoice}");
                    if (GottenChoice == kFlurryOfBlows)
                    {
                        GottenChoice = CommandBox.AbilityType.kNone;
                        var allRegularAttacks = instance.FindAllRegularAttacks(instance.mCurrentActor);
                        if (allRegularAttacks.Count == 0)
                        {
                            instance.mSoundManager.SfxPlay(2, Sounds.UI.kErrorButton);
                            instance.mCommandBox.Show();
                            return;
                        }
                        instance.SpawnAbilitiesWithMpCostRemoved(allRegularAttacks, kFlurryOfBlowsSelection, 10);
                        //instance.mCommandBox.LoadAbilitiesList(allRegularAttacks, kFlurryOfBlowsSelection);
                        //instance.mCommandBox.Show();
                    } 
                    else if (GottenChoice == kFlurryOfBlowsSelection)
                    {
                        GottenChoice = CommandBox.AbilityType.kNone;
                        int index = instance.mCommandBox.GetAbilityIndex();
                        if (index < 0)
                        {
                            instance.mCurrentAction = BattleManager.ActionInformation.LoadAttack(instance.mCurrentActor);
                            var abi = new Abilities.Ability
                            {
                                abilityName = null,
                                effectName = null,
                                name = null,
                                manaCost = 10
                            };
                            AssetLoaderMod.GetCustomData(abi)[ExtraTurn] = true;
                            instance.mCurrentAction.mAbility = abi;
                        }
                        else
                        {
                            instance.CreateAction(CommandBox.AbilityType.kAbility, index);
                            var clone = instance.mCurrentAction.mAbility.Clone("WORK-FB-" + index);
                            clone.name = $"{instance.mLocManager.GetTermNoColors($"ability-{FlurryOfBlows}")} + {clone.name}";
                            clone.manaCost += 10;
                            AssetLoaderMod.GetCustomData(clone)[ExtraTurn] = true;
                            instance.mCurrentAction.mAbility = clone;
                        }
                        if (instance.mCurrentAction.mExtraCommandBox != Abilities.ExtraCommandBox.kNone)
                        {
                            instance.SpawnExtraBox(instance.mCurrentAction.mExtraCommandBox);
                            return;
                        }
                        instance.QueueAbilityProcess();
                    }
                });

        /*public static readonly Context<BattleManager> ConfirmAction =
            AssetLoaderMod.RequestContext<BattleManager>(nameof(BattleManager.ConfirmAction));*/
        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.GetChoice))]
        public static class GetChoice
        {
            public static void Prefix(CommandBox __instance)
            {
                //Melon<MonkMod>.Logger.Msg($"GetChoice called");
                if (ProcessInput.Get())
                {
                    GottenChoice = __instance.mCurrentAbility;
                    if (GottenChoice != CommandBox.AbilityType.kNone)
                    {
                        Melon<MonkMod>.Logger.Msg($"Storing choice {GottenChoice}");
                    }
                }
            }
        }
        
        /*public static readonly Context<CommandBox> GetChoice =
            AssetLoaderMod.RequestContext<CommandBox>(nameof(CommandBox.GetChoice))
                .WithHold(((instance, args) =>
                {
                    Melon<MonkMod>.Logger.Msg($"GetChoice prefix");
                }))
                .WithRelease((instance, args, result) =>
                {
                    Melon<MonkMod>.Logger.Msg($"GetChoice postfix");
                    if (ProcessInput.Get())
                    {
                        GottenChoice = (CommandBox.AbilityType)result;
                        Melon<MonkMod>.Logger.Msg($"Storing choice {GottenChoice}");
                    }
                });*/

        public static readonly Context<CommandBox> OnCommandBoxSelect =
            AssetLoaderMod.RequestContext<CommandBox>(nameof(CommandBox.OnCommandBoxSelect), typeof(int), typeof(GamePadInput.Button))
                .WithHold(((instance, args) =>
                {
                    Melon<MonkMod>.Logger.Msg($"Clearing command");
                    GottenCommand = null;
                }))
                .WithRelease(((instance, args, result) =>
                {
                    if (GottenCommand == null || !GottenCommand.enabled)
                    {
                        return;
                    }

                    Melon<MonkMod>.Logger.Msg($"Did command {GottenCommand.abilityType}");
                    if (/*GottenCommand.abilityType == kFlurryOfBlows ||*/
                        GottenCommand.abilityType == kFlurryOfBlowsSelection)
                    {
                        Melon<MonkMod>.Logger.Msg($"Hiding custom box");
                        GottenCommand = null;
                        instance.Hide();
                    }
                }));
        
        public static readonly Context<CommandBox.CommandPage> GetCommand =
            AssetLoaderMod.RequestContext<CommandBox.CommandPage>(nameof(CommandBox.CommandPage.GetCommand), typeof(int))
                .WithRelease((instance, args, result) =>
                {
                    if (OnCommandBoxSelect.Get())
                    {
                        GottenCommand = (CommandBox.Command)result;
                        Melon<MonkMod>.Logger.Msg($"Storing command for {GottenCommand.abilityType}");
                    }
                });

        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.AddRazorWindCommand))]
        public static class AddCommandBoxes
        {
            public static void Postfix(CommandBox __instance, bool isActive)
            {
                if (AssetLoaderMod.GetCustomData(__instance.mCharacter.character).TryGetValue(FlurryOfBlows, out var val))
                {
                    if (val is bool b && b)
                    {
                        __instance.mCommandsList[__instance.mDepth].AddCommand(
                            new CommandBox.Command(
                                __instance.mLocManager.GetTermNoColors($"ability-{FlurryOfBlows}"), 
                                __instance.GetCommandDescription(__instance.mLocManager.GetTerm($"ability-{FlurryOfBlows}-desc")), 
                                kFlurryOfBlows, 
                                0, 
                                isActive, 
                                CommandBox.KeyCodeIndex.kRazorWind
                                )
                            );
                    }
                }
            }
        }

        #if NET6_0
        public static readonly Context<DamageCalculations> DamageCalcApply =
            AssetLoaderMod.RequestLateContext<DamageCalculations>(nameof(DamageCalculations.Apply), typeof(Il2CppSystem.Collections.Generic.List<BattleCharacter>), typeof(BattleCharacter), typeof(DamageCalculations.DamageResult))
        #else
        public static readonly Context<DamageCalculations> DamageCalcApply =
            AssetLoaderMod.RequestLateContext<DamageCalculations>(nameof(DamageCalculations.Apply), typeof(List<BattleCharacter>), typeof(BattleCharacter), typeof(DamageCalculations.DamageResult))
        #endif
                .WithRelease((instance, args, result) =>
                {
                    var targets = (List<BattleCharacter>)args[0];
                    var caster = (BattleCharacter)args[1];
                    var dmg = (DamageCalculations.DamageResult)args[2];
                    if (dmg.mAbility == null)
                    {
                        return;
                    }
                    if (AssetLoaderMod.CustomAttributes.TryGetValue(dmg.mAbility, out var attr))
                    {
                        if (attr.TryGetValue(MonkEffect, out var val) && val is string s && s == ExtraTurn)
                        {
                            AssetLoaderMod.GetCustomData(caster)[ExtraTurn] = true;
                        }
                    }

                    if (AssetLoaderMod.GetCustomData(dmg.mAbility).TryGetValue(ExtraTurn, out var val2) &&
                        val2 is bool b && b)
                    {
                        AssetLoaderMod.GetCustomData(caster)[ExtraTurn] = true;
                    }
                });

        /*[HarmonyPatch(typeof(DamageCalculations), nameof(DamageCalculations.Apply))]
        public static class DamageCalcTransfer
        {
            public static void Postfix(DamageCalculations __instance, List<BattleCharacter> targets,
                BattleCharacter caster, DamageCalculations.DamageResult dmg)
            {
                if (dmg.mAbility == null)
                {
                    return;
                }
                if (AssetLoaderMod.CustomAttributes.TryGetValue(dmg.mAbility, out var attr))
                {
                    if (attr.TryGetValue(MonkEffect, out var val) && val is string s && s == ExtraTurn)
                    {
                        AssetLoaderMod.GetCustomData(caster)[ExtraTurn] = true;
                    }
                }

                if (AssetLoaderMod.GetCustomData(dmg.mAbility).TryGetValue(ExtraTurn, out var val2) &&
                    val2 is bool b && b)
                {
                    AssetLoaderMod.GetCustomData(caster)[ExtraTurn] = true;
                }
            }
        }*/

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.EndTurn))]
        public static class EndTurnPatch
        {
            public static void Prefix(BattleManager __instance, out bool __state)
            {
                __state = false;
                var data = AssetLoaderMod.GetCustomData(__instance.mCurrentActor);
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
    
    [HarmonyPatch(typeof(SpellEffects), nameof(SpellEffects.Load))]
    public static class CustomVFX
    {
        public static void Postfix(SpellEffects __instance)
        {
            __instance.AddSpell("MONK-A1", new SpellEffect(false).Skilling()
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kAtk, Constants.LoopType.kOnce, 2)
                .PlaySound(Sounds.Spells.kSiegeRamHit, 0f, null, 0f, -1f, false)
                .LoadEffect("HourglassBreak", "HourglassBreakFront01", SpellEffect.Target.kTarget, SpellEffect.Priority.kHigher, true, 1f, SpellEffect.Target.kBank0, Constants.LoopType.kOnce, 1f, SpellEffect.UseDirection.kNone, -1, false)
                .LoadEffect("HourglassBreak", "HourglassBreakBack01", SpellEffect.Target.kTarget, SpellEffect.Priority.kLower, true, 1f, SpellEffect.Target.kBank1, Constants.LoopType.kOnce, 1f, SpellEffect.UseDirection.kNone, -1, false)
                .PlaySound(Sounds.Spells.kHourglassBreak, 0.3f, null, 0f, -1f, false)
                .Wait(0.5f)
                .GettingHit(false, false, true)
                .ShowDamage(0, SpellEffect.Target.kTarget, false)
                .WaitOn(SpellEffect.Target.kBank0)
                .WaitOn(SpellEffect.Target.kBank1)
                .ResetFrames(SpellEffect.Target.kCaster)
                .Wait(0.1f)
                .CleanupTarget()
                .CleanupCaster(SpellEffect.ForceExp.kNormal, false));
            __instance.AddSpell("MONK-A2", new SpellEffect(false).ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kJumpCrouch, 0).LookAt(SpellEffect.Target.kCaster, Constants.Direction.kDefault, 0).Wait(0.25f)
                .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kAtkSpear, 3)
                .MoveSprite(SpellEffect.Target.kCaster, 0.6f, 0.15f, 0f)
                .Wait(0.15f)
                .ShowStillFrame(SpellEffect.Target.kCaster, Constants.Action.kAtkSpear, 4)
                .PlaySound(Sounds.Spells.kPunt, 0f, Sounds.Spells.kGenericWhoosh, 0f, -1f, false)
                .GettingHit(false, false, true)
                .ShowNonBlockingDamage(SpellEffect.Target.kTarget)
                .SpinFrameToFaceCaster(SpellEffect.Target.kTarget, 0.15f, 0, SpellEffect.SpinDirection.kBest, Constants.Direction.kDefault)
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kStand, Constants.LoopType.kDefault, -1)
                .MoveSprite(SpellEffect.Target.kCaster, 0, 0, 0.2f)
                .WaitOn(SpellEffect.Target.kCaster)
                .ShowFrame(SpellEffect.Target.kCaster, Constants.Action.kIdle, Constants.LoopType.kDefault, -1)
                .Wait(0.15f)
                .WaitDamage()
                .Wait(0.1f)
                .CleanupTarget()
                .CleanupCaster(SpellEffect.ForceExp.kNormal, false)
            );
        }
    }
}
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
            LoggerInstance.Msg("Monk Class initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static string MonkClassname => "MONK";
        public static readonly string MonkEffect = nameof(MonkEffect);
        public static readonly string FlurryOfBlows = nameof(FlurryOfBlows);
        public static readonly string ExtraTurn = nameof(ExtraTurn);
        public static readonly CommandBox.AbilityType kFlurryOfBlows = AssetLoaderMod.RequestExtendedEnum<CommandBox.AbilityType>(nameof(kFlurryOfBlows));
        public static readonly CommandBox.AbilityType kFlurryOfBlowsSelection = AssetLoaderMod.RequestExtendedEnum<CommandBox.AbilityType>(nameof(kFlurryOfBlowsSelection));
        public static readonly CommandBox.KeyCodeIndex kFlurryOfBlowsIndex =
            AssetLoaderMod.RequestExtendedEnum<CommandBox.KeyCodeIndex>(nameof(kFlurryOfBlowsIndex));
        public static Context<BaseCharacter> EquipPassiveCtx;
        public static Context<BaseCharacter> UpdatePassivesAndGearCtx;
        public static Context<BattleManager> ProcessInputCtx;
        public static Context<CommandBox> OnCommandBoxSelectCtx;
        public static Context<CommandBox.CommandPage> GetCommandCtx;
        public static Context<DamageCalculations> DamageCalcApplyCtx;
        public static WeaponRegistry VialWeapon;
        private static CommandBox.AbilityType _gottenChoice;
        private static CommandBox.Command _gottenCommand;
        
        [AssetInit]
        public static void Init()
        {
            VialWeapon = RegistryTools.RegisterWeaponsType("Vial", "icon-vial", WeaponRegistry.Handedness.OneHanded)
                .WithJobs(
                    GameConstants.Jobs.Peddler, 
                    GameConstants.Jobs.Alchemystic, 
                    GameConstants.Jobs.Anatomist, 
                    GameConstants.Jobs.PlagueDoctor,
                    GameConstants.Jobs.Druid)
                .WithSounds(WeaponsType.kfMace);
            EquipPassiveCtx =
                AssetLoaderMod.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.EquipPassive), typeof(string))
                    .WithRelease((instance, args, result) =>
                    {
                        var passive = args[0] as string;
                        if (string.IsNullOrEmpty(passive))
                            return;
                        Abilities.Ability ability = Database.GetInstance().GetAbility(passive);
                        if (ability.GetCustomAttributes(out var attr))
                        {
                            foreach (var pair in attr)
                            {
                                Melon<MonkMod>.Logger.Msg($"Ability has attribute, {pair.Key} -> {pair.Value}");
                            }

                            if (attr.TryGetValue(MonkEffect, out var val))
                            {
                                if (val == FlurryOfBlows)
                                {
                                    Melon<MonkMod>.Logger.Msg($"BaseCharacter.EquipPassive Postfix, applied {FlurryOfBlows}");
                                    instance.GetCustomFields()[FlurryOfBlows] = true;
                                }
                            }
                        }
                    });
            
            UpdatePassivesAndGearCtx =
                AssetLoaderMod.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.UpdatePassivesAndGear))
                    .WithHold((instance, args) =>
                    {
                        instance.GetCustomFields()[FlurryOfBlows] = false;
                    });
            
            ProcessInputCtx =
                AssetLoaderMod.RequestContext<BattleManager>(nameof(BattleManager.ProcessInput))
                    .WithRelease((instance, args, result) =>
                    {
                        if (_gottenChoice == CommandBox.AbilityType.kNone)
                        {
                            return;
                        }
                        Melon<MonkMod>.Logger.Msg($"BattleManger ProcessInput got choice {_gottenChoice}");
                        if (_gottenChoice == kFlurryOfBlows)
                        {
                            _gottenChoice = CommandBox.AbilityType.kNone;
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
                        else if (_gottenChoice == kFlurryOfBlowsSelection)
                        {
                            _gottenChoice = CommandBox.AbilityType.kNone;
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
                                abi.GetCustomFields()[ExtraTurn] = true;
                                instance.mCurrentAction.mAbility = abi;
                            }
                            else
                            {
                                instance.CreateAction(CommandBox.AbilityType.kAbility, index);
                                var clone = instance.mCurrentAction.mAbility.Clone("WORK-FB-" + index);
                                clone.name = $"{instance.mLocManager.GetTermNoColors($"ability-{FlurryOfBlows}")} + {clone.name}";
                                clone.manaCost += 10;
                                clone.GetCustomFields()[ExtraTurn] = true;
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
            
            OnCommandBoxSelectCtx =
                AssetLoaderMod.RequestContext<CommandBox>(nameof(CommandBox.OnCommandBoxSelect), typeof(int), typeof(GamePadInput.Button))
                    .WithHold(((instance, args) =>
                    {
                        Melon<MonkMod>.Logger.Msg($"Clearing command");
                        _gottenCommand = null;
                    }))
                    .WithRelease(((instance, args, result) =>
                    {
                        if (_gottenCommand == null || !_gottenCommand.enabled)
                        {
                            return;
                        }

                        Melon<MonkMod>.Logger.Msg($"Did command {_gottenCommand.abilityType}");
                        if (/*GottenCommand.abilityType == kFlurryOfBlows ||*/
                            _gottenCommand.abilityType == kFlurryOfBlowsSelection)
                        {
                            Melon<MonkMod>.Logger.Msg($"Hiding custom box");
                            _gottenCommand = null;
                            instance.Hide();
                        }
                    }));
            
            GetCommandCtx =
                AssetLoaderMod.RequestContext<CommandBox.CommandPage>(nameof(CommandBox.CommandPage.GetCommand), typeof(int))
                    .WithRelease((instance, args, result) =>
                    {
                        if (OnCommandBoxSelectCtx.Get())
                        {
                            _gottenCommand = (CommandBox.Command)result;
                            Melon<MonkMod>.Logger.Msg($"Storing command for {_gottenCommand.abilityType}");
                        }
                    });
            
            DamageCalcApplyCtx =
                AssetLoaderMod.RequestLateContext<DamageCalculations>(nameof(DamageCalculations.Apply), typeof(List<BattleCharacter>), typeof(BattleCharacter), typeof(DamageCalculations.DamageResult))
                    .WithRelease((instance, args, result) =>
                    {
                        var targets = (List<BattleCharacter>)args[0];
                        var caster = (BattleCharacter)args[1];
                        var dmg = (DamageCalculations.DamageResult)args[2];
                        if (dmg.mAbility == null)
                        {
                            return;
                        }
                        if (dmg.mAbility.GetCustomAttributes(out var attr))
                        {
                            if (attr.TryGetValue(MonkEffect, out var val) && val == ExtraTurn)
                            {
                                caster.GetCustomFields()[ExtraTurn] = true;
                            }
                        }

                        if (dmg.mAbility.GetCustomFields().TryGetValue(ExtraTurn, out var val2) &&
                            val2 is bool b && b)
                        {
                            caster.GetCustomFields()[ExtraTurn] = true;
                        }
                    });
                
            Melon<MonkMod>.Logger.Msg("Patch statics initialized");
        }
        
        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.GetChoice))]
        public static class GetChoice
        {
            public static void Prefix(CommandBox __instance)
            {
                //Melon<MonkMod>.Logger.Msg($"GetChoice called");
                if (ProcessInputCtx.Get())
                {
                    _gottenChoice = __instance.mCurrentAbility;
                    if (_gottenChoice != CommandBox.AbilityType.kNone)
                    {
                        Melon<MonkMod>.Logger.Msg($"Storing choice {_gottenChoice}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.AddRazorWindCommand))]
        public static class AddCommandBoxes
        {
            public static void Postfix(CommandBox __instance, bool isActive)
            {
                if (__instance.mCharacter.character.GetCustomFields().TryGetValue(FlurryOfBlows, out var val))
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
                                kFlurryOfBlowsIndex
                                )
                            );
                    }
                }
            }
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
using FellSealAssetLoader;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using XPOverflow;

#if NET6_0
using Il2CppApEngine;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Il2CppGame.Data.DLC1;
using Il2CppGame.DLC1;
using Il2CppSystem.Collections.Generic;
#else
using ApEngine;
using Game.Data;
using Game.Battle;
using Game.Data.DLC1;
using Game.DLC1;
using System.Collections.Generic;
#endif

[assembly: MelonInfo(typeof(XPOverflowMod), "XP Overflow", "0.0.1", "Autumn Mooncat")]
[assembly: MelonAdditionalDependencies("FellSealAssetLoader")]
namespace XPOverflow
{
    public class XPOverflowMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("XP Overflow Melon Initializing");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        public static Context<Inventory> AddMissionRewardCtx;
        public static Context<BaseCharacter> LevelUpCtx;
        public static Context<DamageCalculations> CalculateExpGainCtx;
        private static int _origExp;
        
        [AssetInit]
        public static void Init()
        {
            Melon<XPOverflowMod>.Logger.Msg("XP Overflow Asset Initializing");
            
            AddMissionRewardCtx = AssetLoaderMod.RequestContext<Inventory>(
                nameof(Inventory.AddMissionReward),
                typeof(Inventory.MissionInProgress),
                typeof(MissionListBox.MissionRewardResult),
                typeof(Missions.MissionResult));
            LevelUpCtx = AssetLoaderMod.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.LevelUp))
                .WithHold((instance, args) =>
                {
                    _origExp = instance.experience;
                })
                .WithRelease((instance, args, result) =>
                {
                    if (!AddMissionRewardCtx.Get() && instance.level < BaseCharacter.kLevelMax)
                    {
                        instance.experience = _origExp - 100;
                        if (instance.experience >= 100)
                        {
                            instance.LevelUp();
                        }
                    }
                });
            CalculateExpGainCtx =
                AssetLoaderMod.RequestLateContext<DamageCalculations>(
                        nameof(DamageCalculations.CalculateExpGain), 
                        typeof(BattleCharacter), 
                        typeof(List<BattleCharacter>), 
                        typeof(DamageCalculations.DamageResult), 
                        typeof(SpellEffect.ForceExp))
                    .WithHoldReturn((instance, args, run, result) =>
                    {
                        run[0] = false;
                        var caster = (BattleCharacter)args[0];
                        var targets = (List<BattleCharacter>)args[1];
                        var dmg = (DamageCalculations.DamageResult)args[2];
                        var forceExp = (SpellEffect.ForceExp)args[3];
                        
                        if (caster == null)
                        {
                            result[0] = 0;
                            return;
                        }
                        float doubleCastMulti = caster.mCameFromDoubleCasting ? instance.kDoubleCastExpReduction : 1f;
                        if (dmg == null && forceExp == SpellEffect.ForceExp.kForceYes)
                        {
                            result[0] = caster.character.level >= BaseCharacter.kLevelMax
                                ? 0
                                : (int)(instance.kExpBase * doubleCastMulti);
                            return;
                        }
                        int xp = 0;
                        bool isACounter = dmg.mIsACounter;
                        if (forceExp == SpellEffect.ForceExp.kForceYes)
                            xp = (int) (instance.kExpBase * doubleCastMulti);
                        if (targets != null)
                        {
                            if (dmg.mDidSomething || dmg.mForceDeath || dmg.mWeKilledSomeoneWithPunt)
                            {
                                for (int index = 0; index < targets.Count; ++index)
                                {
                                    BattleCharacter target = targets[index];
                                    int levelDiff = target.character.level - caster.character.level;
                                    if (levelDiff > 0)
                                        levelDiff *= Rand.GetRange(instance.kExpRandomMin, instance.kExpRandomMax);
                                    int xpScaled = (int) ((instance.kExpBase + levelDiff) * doubleCastMulti);
                                    int xpBonus = 0;
                                    if ((target.character.currentValues.isDead || dmg.mForceDeath || dmg.mWeKilledSomeoneWithPunt) && dmg.mDarkRevive == DamageCalculations.DamageResult.DarkReviveType.kNone && !dmg.mRevive && !dmg.mDmgHand[0].mDmgText[index].predictedDmg.isReraise)
                                    {
                                        switch (target.mTimesKilled)
                                        {
                                            case 0:
                                            case 1:
                                                xpBonus += instance.kExpKillbonus;
                                                if (caster.isPlayerActor && !caster.character.isGuest)
                                                {
                                                    if (caster.mIsWearingBloodArmor)
                                                    {
                                                        Inventory mInventory = instance.mInventory;
                                                        int[] bloodArmorValues1 = Database.GetInstance().GetArmorsDb().GetBloodArmorValues();
                                                        ++mInventory.storyValues[0];
                                                        Database.GetInstance().GetArmorsDb().UpdateBloodArmor(mInventory.storyValues[0]);
                                                        int[] bloodArmorValues2 = Database.GetInstance().GetArmorsDb().GetBloodArmorValues();
                                                        caster.character.AddPlus1ToGear(bloodArmorValues1, bloodArmorValues2);
                                                    }
                                                    if (caster.mIsWearingSoulEater)
                                                    {
                                                        Inventory mInventory = instance.mInventory;
                                                        int[] soulEaterValues1 = Database.GetInstance().GetWeaponsDb().GetSoulEaterValues();
                                                        ++mInventory.storyValues[2];
                                                        Database.GetInstance().GetWeaponsDb().UpdateSoulEater(mInventory.storyValues[2]);
                                                        int[] soulEaterValues2 = Database.GetInstance().GetWeaponsDb().GetSoulEaterValues();
                                                        caster.character.AddPlus1ToGear(soulEaterValues1, soulEaterValues2);
                                                    }
                                                    instance.CalculateMvpKill(caster, target);
                                                }
                                                goto case 2;
                                            case 2:
                                                if (target.mBonusExp != 0)
                                                {
                                                    xpBonus += target.mBonusExp;
                                                }
                                                break;
                                            case 3:
                                            case 4:
                                            case 5:
                                                xpBonus -= 5 + target.mTimesKilled;
                                                goto case 2;
                                            default:
                                                xpBonus -= 10;
                                                goto case 2;
                                        }
                                    }
                                    int xpTotal = xpScaled + xpBonus;
                                    if (xpTotal <= 0)
                                        xpTotal = 1;
                                    if (isACounter || forceExp == SpellEffect.ForceExp.kOnlyOnKill)
                                        xpTotal = xpBonus;
                                    if (dmg.mIsAHeal != DamageCalculations.DamageResult.HealingType.kDamage && (dmg.mAbility == null || !dmg.mAbility.isAnItem))
                                        xpTotal += (int) (xpTotal * instance.kExpHealBonus);
                                    if (xp < xpTotal)
                                        xp = xpTotal;
                                }
                            }
                        }
                        else
                        {
                            xp = (int) (instance.kExpBase * doubleCastMulti);
                            if (isACounter)
                                xp = 0;
                        }
                        result[0] = caster.character.level >= BaseCharacter.kLevelMax ? 0 : xp;
                    });
        }
    }
}
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;

#if NET6_0
using Il2CppGame.Data;
#else
using Game.Data;
#endif

namespace FellSealAssetLoader.Patches
{
    [HarmonyPatch]
    public static class CharacterPatches
    {
        private static readonly Context<BaseCharacter> GetWeaponEquipMaskCtx = 
            ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.GetWeaponEquipMask));
        
        private static readonly Context<BaseCharacter> GetArmorEquipMaskCtx = 
            ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.GetArmorEquipMask));
        
        private static readonly Context<BaseCharacter> UpdatePassivesAndGearCtx =
            ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.UpdatePassivesAndGear));
        
        private static readonly Context<BaseCharacter> EquipPassiveCtx =
            ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.EquipPassive), typeof(string));

        private static readonly Context<BaseCharacter> EquipBaseCtx =
            ContextTools.RequestLateContext<BaseCharacter>(nameof(BaseCharacter.EquipBase), typeof(BaseItem));

        private static readonly Context<Abilities.Ability> CloneCtx =
            ContextTools.RequestLateContext<Abilities.Ability>(nameof(Abilities.Ability.Clone), typeof(string));
        
        [AssetInternalInit]
        public static void Init()
        {
            AssetLoaderEvents.DatabaseInit += db =>
            {
                foreach (var job in db.mJobsAndAbilities.jobs)
                {
                    if (job.GetCustomAttributes(out var attr))
                    {
                        if (attr.TryGetValue("CustomWeaponsType", out var weapons))
                        {
                            foreach (var reg in RegistryTools.WeaponRegistries)
                            {
                                if (weapons.Contains(reg.id))
                                {
                                    job.weaponsAllowed |= reg.type;
                                }
                            }
                        }

                        if (attr.TryGetValue("CustomArmorType", out var armors))
                        {
                            foreach (var reg in RegistryTools.ArmorRegistries)
                            {
                                if (armors.Contains(reg.id))
                                {
                                    job.armorsAllowed |= reg.type;
                                }
                            }
                        }
                    }
                }
            };
            
            GetWeaponEquipMaskCtx
                .WithReleaseReturn((instance, args, result) =>
                {
                    var job = instance.GetMainJob();
                    var valid = (WeaponsType)result[0];
                    foreach (var reg in RegistryTools.WeaponRegistries)
                    {
                        if ((job.weaponsAllowed & reg.type) == reg.type)
                        {
                            valid |= reg.type;
                        }
                    }

                    result[0] = valid;
                });
            
            GetArmorEquipMaskCtx
                .WithReleaseReturn((instance, args, result) =>
                {
                    var job = instance.GetMainJob();
                    var valid = (ArmorType)result[0];
                    foreach (var reg in RegistryTools.ArmorRegistries)
                    {
                        if ((job.armorsAllowed & reg.type) == reg.type)
                        {
                            valid |= reg.type;
                        }
                    }

                    result[0] = valid;
                });
            
            UpdatePassivesAndGearCtx
                .WithHold((instance, args) =>
                {
                    instance.CustomEffects().Clear();
                });
            
            EquipPassiveCtx
                .WithRelease((instance, args, result) =>
                {
                    var passive = (string)args[0];
                    if (string.IsNullOrEmpty(passive))
                        return;
                    var ability = Database.GetInstance().GetAbility(passive);
                    instance.CustomEffects().UnionWith(ability.CustomEffects());
                });

            EquipBaseCtx
                .WithRelease((instance, args, result) =>
                {
                    var item = (BaseItem)args[0];
                    instance.CustomEffects().UnionWith(item.CustomEffects());
                });

            CloneCtx
                .WithRelease((instance, args, result) =>
                {
                    result.CustomEffects().UnionWith(instance.CustomEffects());
                });
        }
    }
}
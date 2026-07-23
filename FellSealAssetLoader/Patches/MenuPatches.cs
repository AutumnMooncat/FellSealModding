using System;
using System.Collections.Generic;
using System.Text;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using Il2Cpp;
using Il2CppGame;
using Il2CppGame.Battle;
using Il2CppGame.Data;
using Il2CppGame.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppApEngine;
#else
using Game;
using Game.Battle;
using Game.Data;
using Game.UI;
using ApEngine;
#endif

namespace FellSealAssetLoader.Patches
{
    [HarmonyPatch]
    public static class MenuPatches
    {
        public static bool RebuildGearLists;

        public static readonly Dictionary<WeaponsType, Inventory.GearList> GearMap = new Dictionary<WeaponsType, Inventory.GearList>();
        public static Context<Inventory> CreateGearListCtx;
        public static Context<Inventory> CreateAllGroupsCtx;
        public static Context<BattleManager> ProcessInputCtx;
        public static Context<CommandBox> OnCommandBoxSelectCtx;
        public static Context<CommandBox.CommandPage> GetCommandCtx;
        private static CommandBox.AbilityType _gottenChoice;
        private static CommandBox.Command _gottenCommand;
        
        [AssetInit]
        public static void Init()
        {
            #if NET6_0
            CreateGearListCtx = ContextTools.RequestContext<Inventory>(nameof(Inventory.CreateGearList), typeof(Il2CppStringArray))
            #else
            CreateGearListCtx = ContextTools.RequestContext<Inventory>(nameof(Inventory.CreateGearList), typeof(string[]))
            #endif
                .WithHold((instance, args) =>
                {
                    GearMap.Clear();
                })
                .WithRelease((instance, args, result) =>
                {
                    #if NET6_0
                    var lists = result as Il2CppSystem.Collections.Generic.List<Inventory.GearList>;
                    #else
                    var lists = result as List<Inventory.GearList>;
                    #endif
                    var wIndex = 0;
                    for (var i = 0; i < lists.Count; i++)
                    {
                        if (lists[i] != null && lists[i].mFamily == ItemFamily.kWeapon)
                        {
                            wIndex = i;
                        }
                    }
                    foreach (var list in GearMap.Values)
                    {
                        list.Sort();
                        //lists.Add(list);
                        lists.Insert(wIndex, list);
                    }
                    //Melon<AssetLoaderMod>.Logger.Msg($"CreateGearListCtx added {GearMap.Count} GearLists");
                });

            CreateAllGroupsCtx = ContextTools.RequestContext<Inventory>(nameof(Inventory.CreateAllGroups))
                .WithHold((instance, args) =>
                {
                    GearMap.Clear();
                })
                .WithRelease((instance, args, result) =>
                {
                    var lists = instance.mGearByType;
                    var wIndex = 0;
                    for (var i = 0; i < lists.Count; i++)
                    {
                        if (lists[i] != null && lists[i].mFamily == ItemFamily.kWeapon)
                        {
                            wIndex = i;
                        }
                    }
                    foreach (var list in GearMap.Values)
                    {
                        list.Sort();
                        lists.Insert(wIndex, list);
                    }
                    //Melon<AssetLoaderMod>.Logger.Msg($"CreateAllGroupsCtx added {GearMap.Count} GearLists");
                });
            
            ProcessInputCtx =
                AssetLoaderMod.RequestContext<BattleManager>(nameof(BattleManager.ProcessInput))
                    .WithRelease((instance, args, result) =>
                    {
                        if (_gottenChoice == CommandBox.AbilityType.kNone)
                        {
                            return;
                        }
                        foreach (var reg in RegistryTools.CommandRegistries)
                        {
                            if (_gottenChoice == reg.type)
                            {
                                _gottenChoice = CommandBox.AbilityType.kNone;
                                reg.onSelect(instance);
                                return;
                            }
                        }
                    });
            
            OnCommandBoxSelectCtx =
                AssetLoaderMod.RequestContext<CommandBox>(nameof(CommandBox.OnCommandBoxSelect), typeof(int), typeof(GamePadInput.Button))
                    .WithHold((instance, args) =>
                    {
                        _gottenCommand = null;
                    })
                    .WithRelease((instance, args, result) =>
                    {
                        if (_gottenCommand == null || !_gottenCommand.enabled)
                        {
                            return;
                        }
                        foreach (var reg in RegistryTools.CommandRegistries)
                        {
                            if (_gottenCommand.abilityType == reg.type && !reg.root)
                            {
                                _gottenCommand = null;
                                instance.Hide();
                                return;
                            }
                        }
                    });
            
            GetCommandCtx =
                AssetLoaderMod.RequestContext<CommandBox.CommandPage>(nameof(CommandBox.CommandPage.GetCommand), typeof(int))
                    .WithRelease((instance, args, result) =>
                    {
                        if (OnCommandBoxSelectCtx.Get())
                        {
                            _gottenCommand = (CommandBox.Command)result;
                        }
                    });
        }
        
        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.GetChoice))]
        public static class GetChoice
        {
            public static void Prefix(CommandBox __instance)
            {
                if (ProcessInputCtx.Get())
                {
                    _gottenChoice = __instance.mCurrentAbility;
                }
            }
        }

        [HarmonyPatch(typeof(CommandBox), nameof(CommandBox.AddRazorWindCommand))]
        public static class AddCommandBoxes
        {
            public static void Postfix(CommandBox __instance, bool isActive)
            {
                foreach (var reg in RegistryTools.CommandRegistries)
                {
                    // TODO track and fill in index, new page if too many
                    if (reg.root && reg.shouldAppear(__instance))
                    {
                        __instance.mCommandsList[__instance.mDepth].AddCommand(
                            new CommandBox.Command(
                                __instance.mLocManager.GetTermNoColors(reg.nameKey), 
                                __instance.GetCommandDescription(__instance.mLocManager.GetTerm(reg.descKey)), 
                                reg.type, 
                                0, 
                                isActive, 
                                CommandBox.KeyCodeIndex.kNone
                            )
                        );
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.SpawnExtraBox))]
        public static class SpawnExtraBoxPatch
        {
            public static bool Prefix(BattleManager __instance, Abilities.ExtraCommandBox extraCommandBox)
            {
                foreach (var reg in RegistryTools.ExtraBoxRegistries)
                {
                    if (reg.type == extraCommandBox)
                    {
                        //Melon<AssetLoaderMod>.Logger.Error($"Spawning ExtraBoxReg {reg.name}");
                        __instance.mCustomEntries.Clear();
                        if (!reg.onSpawn(__instance))
                        {
                            __instance.mSoundManager.SfxPlay(2, Sounds.UI.kErrorButton);
                            __instance.mCommandBox.Show();
                        }
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.ProcessCustomCommandBox))]
        public static class ProcessCustomCommandBoxPatch
        {
            public static bool Prefix(BattleManager __instance, Abilities.ExtraCommandBox extraCommand, int index)
            {
                foreach (var reg in RegistryTools.ExtraBoxRegistries)
                {
                    if (reg.type == extraCommand)
                    {
                        //Melon<AssetLoaderMod>.Logger.Error($"Processing ExtraBoxReg {reg.name}");
                        reg.onProcess(__instance, index);
                        __instance.QueueAbilityProcess();
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddweaponToList))]
        public static class AddWeaponToList
        {
            public static bool Prefix(Inventory __instance, Weapon wp, Inventory.ItemInfo info, Inventory.GearList[] wpList)
            {
                /*var index = (int) Bits.GetIndexFromSingleBit((uint) wp.type);
                if (index >= 13)
                {
                    Melon<AssetLoaderMod>.Logger.Error($"Got index {index} from {wp.name}, will explode if enum with {wp.type} is not matched");
                }*/
                if (wp.IsExtendedType(out var type))
                {
                    if (!GearMap.ContainsKey(type))
                    {
                        var reg = type.GetRegistry();
                        if (reg == null)
                        {
                            Melon<AssetLoaderMod>.Logger.Error($"Found unregistered WeaponsType {type}");
                            return false;
                        }
                        GearMap[type] = new Inventory.GearList
                        {
                            mName = __instance.mLocManager.GetTermNoColors("gear-" + reg.name),
                            mFamily = ItemFamily.kWeapon,
                            mType = wp.type,
                            mAtlas = "MenuGeneric",
                            mSprite = reg.sprite,
                            #if NET6_0
                            mItems = new Il2CppSystem.Collections.Generic.List<Inventory.ItemInfo>()
                            #else
                            mItems = new List<Inventory.ItemInfo>()
                            #endif
                        };
                    }
                    GearMap[type].mItems.Add(info);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetGearList), new Type[0])]
        public static class GetGearList
        {
            public static void Prefix(Inventory __instance)
            {
                if (RebuildGearLists)
                {
                    RebuildGearLists = false;
                    __instance.mNeedToRebuildGearList = true;
                }   
            }
        }

        [HarmonyPatch(typeof(EquipPanel), nameof(EquipPanel.SetType), typeof(Weapon))]
        public static class SetTypeW
        {
            public static bool Prefix(EquipPanel __instance, Weapon wp)
            {
                if (wp.IsExtendedType(out var type))
                {
                    var reg = type.GetRegistry();
                    if (reg == null)
                    {
                        Melon<AssetLoaderMod>.Logger.Error($"Found unregistered WeaponsType {type}");
                        return false;
                    }
                    
                    __instance.mType.text.text = (Weapons.IsTwoHanded(type) ? EquipPanel.m2H : EquipPanel.m1H) + __instance.mLocManager.GetTermNoColors("gear-" + reg.name).ToUpperInvariant();
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(EquipPanel), nameof(EquipPanel.SetType), typeof(Armor))]
        public static class SetTypeA
        {
            public static bool Prefix(EquipPanel __instance, Armor ar)
            {
                if (ar.IsExtendedType(out var type))
                {
                    var reg = type.GetRegistry();
                    if (reg == null)
                    {
                        Melon<AssetLoaderMod>.Logger.Error($"Found unregistered ArmorType {type}");
                        return false;
                    }
                    __instance.mType.text.text = __instance.mLocManager.GetTerm("gear-" + reg.name).ToUpperInvariant();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ClassInfoBox), nameof(ClassInfoBox.SetWeaponsAndAmor))]
        public static class SetWeaponsAndAmor
        {
            public static bool Prefix(ClassInfoBox __instance, Abilities.Job job)
            {
                var builder = new StringBuilder();
                var first = true;
                var weaponsTypes = Enum.GetValues(typeof(WeaponsType));
                foreach (var o in weaponsTypes)
                {
                    if (o is WeaponsType type && Bits.CountBits((uint)type) == 1 && (job.weaponsAllowed & type) == type)
                    {
                        if (!first)
                        {
                            builder.Append(__instance.mSeparator);
                        }
                        first = false;
                        
                        var reg = type.GetRegistry();
                        if (reg == null)
                        {
                            builder.Append(__instance.mLocManager.GetTermNoColors("gear-" + LocalizationHelper.kWeaponTypes[(int) Bits.GetIndexFromSingleBit((uint) type)]));
                        }
                        else
                        {
                            builder.Append(__instance.mLocManager.GetTermNoColors("gear-" + reg.name));
                        }
                    }
                }
                builder.AppendLine("<line-height=125%>");

                var state = 0;
                var armorTypes = Enum.GetValues(typeof(ArmorType));
                foreach (var o in armorTypes)
                {
                    if (o is ArmorType type && Bits.CountBits((uint)type) == 1 && (job.armorsAllowed & type) == type)
                    {
                        if (state == 1)
                        {
                            builder.Append(__instance.mSeparator + "</line-height>");
                        } else if (state > 1)
                        {
                            builder.Append(__instance.mSeparator);
                        }
                        state++;
                        
                        var reg = type.GetRegistry();
                        if (reg == null)
                        {
                            builder.Append(__instance.mLocManager.GetTermNoColors("gear-" + LocalizationHelper.kArmorTypes[(int) Bits.GetIndexFromSingleBit((uint) type)]));
                        }
                        else
                        {
                            builder.Append(__instance.mLocManager.GetTermNoColors("gear-" + reg.name));
                        }
                    }
                }
                
                __instance.mEquipment[0].text.text = builder.ToString();
                return false;
            }
        }
    }
}
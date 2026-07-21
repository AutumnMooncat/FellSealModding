using System;
using System.Collections.Generic;
using System.Linq;
using FellSealAssetLoader.Tools;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;

#if NET6_0
using System.Runtime.InteropServices;
using MelonLoader.NativeUtils;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppGame.Battle;
using Il2CppGame.Data;
#else
using Game.Battle;
using Game.Data;
#endif

namespace FellSealAssetLoader.Util
{
    public class Registry
    {
        public readonly MelonInfoAttribute modInfo;

        public Registry(MelonInfoAttribute modInfo)
        {
            this.modInfo = modInfo;
        }
    }

    public class WeaponRegistry : Registry
    {
        public enum Handedness
        {
            OneHanded,
            TwoHanded,
            TrueTwoHanded
        }

        public delegate string GetAttackHash(BattleCharacter caster);
        
        public readonly WeaponsType type;
        public readonly string id;
        public readonly string name;
        public readonly string sprite;
        public readonly Handedness handedness;
        public int minimumRange = 1;
        public bool canTargetSelf;
        public GetAttackHash attackHash;

        public WeaponRegistry(MelonInfoAttribute modInfo, WeaponsType type, string id, string name, string sprite, Handedness handedness) : base(modInfo)
        {
            this.type = type;
            this.id = id;
            this.name = name;
            this.sprite = sprite;
            this.handedness = handedness;
        }

        public WeaponRegistry WithAttackHash(GetAttackHash attackHash)
        {
            this.attackHash = attackHash;
            return this;
        }

        public WeaponRegistry WithMinimumRange(int minimumRange)
        {
            this.minimumRange = minimumRange;
            return this;
        }

        public WeaponRegistry WithSelfTargeting(bool canTargetSelf = true)
        {
            this.canTargetSelf = canTargetSelf;
            return this;
        }
        
        public WeaponRegistry WithJobs(params string[] jobs)
        {
            AssetLoaderEvents.DatabaseInit += db =>
            {
                foreach (var job in jobs)
                {
                    for (int i = 0; i < db.mJobsAndAbilities.jobs.Count; i++)
                    {
                        var found = db.mJobsAndAbilities.jobs[i];
                        if (found.className.Equals(job))
                        {
                            found.weaponsAllowed |= type;
                        }
                    }
                }
            };

            return this;
        }

        public WeaponRegistry WithSounds(WeaponsType copy)
        {
            return WithSounds(copy, copy);
        }

        public WeaponRegistry WithSounds(WeaponsType hitCopy, WeaponsType missCopy)
        {
            AssetLoaderEvents.DatabaseInit += db =>
            {
                db.GetWeaponsDb().mSoundByType[type] = db.GetWeaponsDb().mSoundByType[hitCopy];
                db.GetWeaponsDb().mSoundMissByType[type] = db.GetWeaponsDb().mSoundByType[missCopy];
            };
            return this;
        }

        public WeaponRegistry WithSounds(string hitSound, float hitVol, string missSound, float missVol)
        {
            AssetLoaderEvents.DatabaseInit += db =>
            {
                #if NET6_0
                db.GetWeaponsDb().mSoundByType[type] = new Il2CppSystem.Collections.Generic.KeyValuePair<string, float>(hitSound, hitVol);
                db.GetWeaponsDb().mSoundMissByType[type] = new Il2CppSystem.Collections.Generic.KeyValuePair<string, float>(missSound, missVol);
                #else
                db.GetWeaponsDb().mSoundByType[type] = new KeyValuePair<string, float>(hitSound, hitVol);
                db.GetWeaponsDb().mSoundMissByType[type] = new KeyValuePair<string, float>(missSound, missVol);
                #endif
            };
            return this;
        }
    }
    
    public class ArmorRegistry : Registry
    {
        public readonly ArmorType type;
        public readonly string id;
        public readonly string name;
        public readonly string sprite;

        public ArmorRegistry(MelonInfoAttribute modInfo, ArmorType type, string id, string name, string sprite) : base(modInfo)
        {
            this.type = type;
            this.id = id;
            this.name = name;
            this.sprite = sprite;
        }
    }
}
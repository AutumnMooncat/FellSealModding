using System.Collections.Generic;
using System.IO;
using System.Linq;
using FellSealAssetLoader.Util;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using Sprite = UnityEngine.Sprite;

#if NET6_0
using Il2CppSpriteEngine;
using Il2CppTMPro;
using Il2CppApEngine;
#else
using SpriteEngine;
using TMPro;
using ApEngine;
#endif

namespace FellSealAssetLoader.Loaders
{
    [HarmonyPatch]
    public static class ImageLoader
    {
        private static readonly Dictionary<string, Sprite> UnitySprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> UnityTextures = new Dictionary<string, Texture2D>();
        
        [AssetInit]
        public static void Init(MelonLogger.Instance logger)
        {
            logger.Msg("Loading custom sprites");
            FileChecker.FrozenWalk(MelonEnvironment.ModsDirectory, ".png", png =>
            {
                var name = png.Substring(0, png.Length-4).Split('\\', '/').Last();
                var rawByes = File.ReadAllBytes(png);
                var tex2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                if (tex2D.LoadImage(rawByes, false))
                {
                    //logger.Msg("Loading sprite "+name);
                    if (UnitySprites.ContainsKey(name))
                    {
                        logger.Warning("Sprite name collision on "+name);
                    }
                    
                    var spr = Sprite.Create(tex2D, new Rect(0f, 0f, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f), 1f);
                    spr.name = name;
                    UnitySprites[name] = spr; 
                    UnityTextures[name] = tex2D;
                    tex2D.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    spr.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
                else
                {
                    logger.Error("Failed to load sprite at "+png);
                }
            });
            logger.Msg($"Loaded {UnitySprites.Count} sprites");
        }

        [AssetDeinit]
        public static void Deinit(MelonLogger.Instance logger)
        {
            foreach (var tex in UnityTextures.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }
            foreach (var tex in UnitySprites.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }

            TMPStitching.Stitched = false;
        }
        
        [HarmonyPatch(typeof(Loader), nameof(Loader.Load))]
        public static class LoaderLoad
        {
            public static void Postfix(Loader __instance, Sheet __result, string assetName)
            {
                if (__result == null || assetName == null)
                {
                    return;
                }
                if (assetName.ToLowerInvariant() == "menugeneric" && (!__result.GetCustomField("SpriteInjected", out bool b) || !b))
                {
                    Melon<AssetLoaderMod>.Logger.Msg($"Injecting Sprites into MenuGeneric Sheet");
                    __result.SetCustomField("SpriteInjected", true);
                    var proto = __result.actionsDictionary["icon-rock"];
                    var toInject = new Sprite[UnitySprites.Count];
                    var i = 0;
                    foreach (var pair in UnitySprites)
                    {
                        //Melon<AssetLoaderMod>.Logger.Msg($"Preparing Sprite {i}: {pair.Key} -> ({pair.Value.texture.width},{pair.Value.texture.height})");
                        toInject[i] = pair.Value;
                        __result.actionsDictionary[pair.Key] = new ActionData
                        {
                            name = pair.Key,
                            frameStart = __result.frames.Length + i,
                            frames = proto.frames,
                            loopType = proto.loopType,
                            originalWidth = pair.Value.texture.width,
                            originalHeight = pair.Value.texture.height,
                            duration = proto.duration,
                            directionStyle = proto.directionStyle,
                            durationPerFrame = proto.durationPerFrame
                        };
                        __result.actionsDictionary[pair.Key].framesOffsets[0] = new ActionData.FramesOffsets { offsets = new[] { new Coordinates(0, 0) } };
                        i++;
                    }
                    //Melon<AssetLoaderMod>.Logger.Msg($"Finalizing injection");
                    #if NET6_0
                    var newFrames = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Sprite>(__result.frames.Length + toInject.Length);
                    for (var j = 0; j < __result.frames.Count; j++)
                    {
                        newFrames[j] = __result.frames[j];
                    }
                    for (var k = 0; k < toInject.Length; k++)
                    {
                        newFrames[__result.frames.Length + k] = toInject[k];
                    }
                    __result.frames = newFrames;
                    #else
                    __result.frames = __result.frames.AddRangeToArray(toInject);
                    #endif
                }
            }
        }

        [HarmonyPatch(typeof(TMP_SpriteAsset), nameof(TMP_SpriteAsset.UpdateLookupTables))]
        public static class TMPStitching
        {
            public static bool Stitched;

            public static void Prefix(ref TMP_SpriteAsset __instance)
            {
                if (!Stitched)
                {
                    Stitched = true;
                    if (!UnitySprites.Values.Any(s => s.name.StartsWith("icon-")))
                    {
                        return;
                    }
                    Melon<AssetLoaderMod>.Logger.Msg("Stitching TMP sprite atlas");
                    var activeBackup = RenderTexture.active;
                    var spriteSheet = __instance.spriteSheet;
                    var targetRender = new RenderTexture(spriteSheet.width, spriteSheet.height, 32);
                    Graphics.Blit(spriteSheet, targetRender);
                    var origTex = new Texture2D(spriteSheet.width, spriteSheet.height, TextureFormat.ARGB32, false);
                    origTex.ReadPixels(new Rect(0f, 0f, targetRender.width, targetRender.height), 0, 0);
                    origTex.Apply();
                    RenderTexture.active = activeBackup;
                    var textures = new List<Texture2D>();
                    foreach (var info in __instance.spriteInfoList)
                    {
                        //Melon<ModFile>.Logger.Msg($"Found info {info.name} x,y,w,h = {{{info.x}, {info.y}, {info.width}, {info.height}}} xo,yo,xa = {{{info.xOffset}, {info.yOffset}, {info.xAdvance}}}");
                        var tex = new Texture2D((int)info.width, (int)info.height, TextureFormat.ARGB32, false);
                        Graphics.CopyTexture_Region(origTex, 0, 0, (int)info.x, (int)info.y, (int)info.width, (int)info.height, tex, 0, 0, 0, 0);
                        textures.Add(tex);
                    }
                    foreach (var spr in UnitySprites.Values)
                    {
                        if (!spr.name.StartsWith("icon-"))
                        {
                            continue;
                        }
                        textures.Add(spr.texture);
                        var tmpSpr = new TMP_Sprite
                        {
                            name = spr.name,
                            sprite = spr,
                            id = __instance.spriteInfoList.Count,
                            hashCode = TMP_TextUtilities.GetSimpleHashCode(spr.name),
                            pivot = new Vector2(-spr.texture.width/2f, spr.texture.height/2f),
                            width = spr.texture.width,
                            height = spr.texture.height,
                            scale = 1f,
                            yOffset = spr.texture.height <= 16f ? 14f : 28f,
                            xAdvance = spr.texture.width,
                            x = -1,
                            y = -1
                        };
                        __instance.spriteInfoList.Add(tmpSpr);
                    }
                    var stitchedTex = new Texture2D(spriteSheet.width, spriteSheet.height, TextureFormat.ARGB32, false);
                    var rects = stitchedTex.PackTextures(textures.ToArray(), 0, 8192, false);
                    for (var i = 0; i < __instance.spriteInfoList.Count; i++)
                    {
                        //Melon<ModFile>.Logger.Msg("Got Rect "+rects[i]);
                        __instance.spriteInfoList[i].x = rects[i].x * stitchedTex.width;
                        __instance.spriteInfoList[i].y = rects[i].y * stitchedTex.height;
                    }
                    __instance.spriteSheet = stitchedTex;
                    __instance.material.mainTexture = stitchedTex;
                    File.WriteAllBytes("ExportedAtlas.png", ImageConversion.EncodeToPNG(stitchedTex));
                }
            }
        }
    }
}
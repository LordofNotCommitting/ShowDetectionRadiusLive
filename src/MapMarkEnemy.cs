using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace ShowDetectionRadiusLive
{
    [HarmonyPatch(typeof(FogOfWar), nameof(FogOfWar.RefreshMinimap))]
    public static class MapMarkEnemy
    {
        static Sprite _minimapAllySprite;
        [HarmonyPostfix]
        public static void Postfix(ref FogOfWar __instance, bool forceShowMonsters = false, bool forceShowItems = false, bool forceShowExits = false)
        {
            if (!forceShowMonsters)
            {

                if (__instance._creatures?.Player == null) return;

                var playerData = __instance._creatures.Player.CreatureData;
                bool revealAll = playerData.EffectsController.HasAnyEffect<WoundEffectRevealingEye>();

                if (!revealAll && __instance._creatures.Player.IsAbleToSpotAnEnemy())
                {
                    int enemyRevealRange = playerData.GetEnemyRevealRange();
                    float maxDist = (float)enemyRevealRange;
                    CellPosition playerPos = playerData.Position;

                    NativeArray<Color32> pixelData = __instance._mapTexture.GetPixelData<Color32>(0);
                    int mapWidth = __instance._mapTexture.width;
                    int mapHeight = __instance._mapTexture.height;

                    // Bounding box restricted to the player's detection radius
                    int minX = Math.Max(0, playerPos.X - enemyRevealRange);
                    int maxX = Math.Min(__instance._textureWidth - 1, playerPos.X + enemyRevealRange);
                    int minY = Math.Max(0, playerPos.Y - enemyRevealRange);
                    int maxY = Math.Min(__instance._textureHeight - 1, playerPos.Y + enemyRevealRange);

                    for (int k = minY; k <= maxY; k++)
                    {
                        for (int l = minX; l <= maxX; l++)
                        {
                            CellPosition cellPos = new CellPosition(l, k);
                            float dist = playerPos.Distance(cellPos);

                            // 1. Must satisfy native game detection condition (dist <= enemyRevealRange)
                            // 2. Must be on the edge (dist > enemyRevealRange - 1.0f)
                            if (dist <= maxDist && dist > (maxDist - 1.0f))
                            {
                                MapCell cell = __instance._mapGrid.GetCell(l, k, true);
                                if (cell == null || cell.GetTileIndex(1) == 255) continue;

                                int startX = l * 4;
                                int startY = k * 4;

                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < 4; j++)
                                    {
                                        int px = startX + i;
                                        int py = startY + j;

                                        if (px >= 0 && px < mapWidth && py >= 0 && py < mapHeight)
                                        {
                                            Color32 tempColor = __instance._mapTexture.GetPixel(px, py);
                                            byte greenValue = (byte)Math.Min(tempColor.g + 70, 255);

                                            Color32 overrideColor = new Color32(tempColor.r, greenValue, tempColor.b, tempColor.a);
                                            TextureHelper.SetColor(ref pixelData, __instance._mapTexture, px, py, overrideColor);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (_minimapAllySprite is null) {

                    //Plugin.Logger.Log("when does image creation proc really");
                    Sprite original = __instance._minimapEnemySprite;

                    Texture2D tex = original.texture;
                    Texture2D newTex = new Texture2D(tex.width, tex.height);

                    Color[] px = tex.GetPixels();
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 temp_color = px[i];
                        px[i] = new Color32(0, (byte)(Math.Min(temp_color.r + (byte)70, byte.MaxValue)), (byte)(Math.Min(temp_color.r + (byte)70, byte.MaxValue)), temp_color.a); // white RGB, keep alpha
                    }

                    newTex.SetPixels(px);
                    newTex.Apply();

                    _minimapAllySprite = Sprite.Create(
                        newTex,
                        __instance._minimapEnemySprite.textureRect,
                        __instance._minimapEnemySprite.pivot
                    );
                }
                





                foreach (Monster creature in __instance._creatures.Monsters)
                {

                    int x3 = creature.CreatureData.Position.X * 4;
                    int y3 = creature.CreatureData.Position.Y * 4;
                    if (creature.ShowSignal || creature._wasSpottedThisAp || creature.CreatureData.EffectsController.HasAnyEffect<Spotted>())
                    {
                        //unseen hostile
                        if (!creature.IsSeenByPlayer)
                        {
                            bool is_quest = MissionSystem.IsQuestMonster(__instance._raidMetadata, creature);

                            if (!is_quest && (creature.CreatureData.CreatureAlliance != __instance._creatures.Player.CreatureData.CreatureAlliance))
                            {
                                TextureHelper.BakeSprite32To32(__instance._mapTexture, __instance._minimapEnemySprite, new CellPosition(x3, y3), false);
                            }
                        }
                        //unseen ally
                        if (creature.CreatureData.CreatureAlliance == __instance._creatures.Player.CreatureData.CreatureAlliance)
                        {
                            //Plugin.Logger.Log("player ally detected");
                            TextureHelper.BakeSprite32To32(__instance._mapTexture, _minimapAllySprite, new CellPosition(x3, y3), false);
                        }
                    } 
                    //if creature is seen and is ally
                    else if (creature.IsSeenByPlayer && creature.CreatureData.CreatureAlliance == __instance._creatures.Player.CreatureData.CreatureAlliance)
                    {
                        //Plugin.Logger.Log("player ally detected");
                        TextureHelper.BakeSprite32To32(__instance._mapTexture, _minimapAllySprite, new CellPosition(x3, y3), false);
                    }



                }
                __instance._mapTexture.Apply();
            }


        }
    }
}




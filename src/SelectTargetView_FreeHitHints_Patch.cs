using HarmonyLib;
using MGSC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ShowDetectionRadiusLive.AddDetectionRadius;

namespace ShowDetectionRadiusLive
{
    [HarmonyPatch(typeof(SelectTargetView), nameof(SelectTargetView.FreeHitHints))]
    public static class SelectTargetView_FreeHitHints_Patch
    {

        static bool Tile_Vision = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Tile_Vision", true);
        static bool Edge_Vision = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Edge_Vision", true);
        public static void Postfix(SelectTargetView __instance)
        {
            // Recycle custom tiles when targeting coroutines halt
            //Plugin.Logger.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAA");
            if (Tile_Vision)
            {
                CustomTilePool.RecycleAll();
            }
            if (Edge_Vision)
            {
                LinePool.RecycleAll();
            }


            
        }
    }


}

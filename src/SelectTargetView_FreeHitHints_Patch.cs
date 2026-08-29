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

        static bool Radius_When_Aim = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Radius_When_Aim", true);
        public static void Postfix(SelectTargetView __instance)
        {
            // Recycle custom tiles when targeting coroutines halt
            //Plugin.Logger.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAA");
            if (Radius_When_Aim)
            {
                CustomTilePool.RecycleAll();
            }
        }
    }


}

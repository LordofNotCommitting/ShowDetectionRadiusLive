//using HarmonyLib;
//using MGSC;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using static ShowDetectionRadiusLive.AddDetectionRadius;

//namespace ShowDetectionRadiusLive
//{
//    using HarmonyLib;
//    using UnityEngine;

//    [HarmonyPatch(typeof(Creature3dView), nameof(Creature3dView.RefreshSignalState))]
//    public static class Creature3dView_RefreshSignalState_Patch
//    {
//        private static bool _hasLoggedData = false;

//        public static void Postfix(Creature3dView __instance, bool show)
//        {
//            // Log once when a signal is active to prevent console spam
//            if (_hasLoggedData || !show || __instance == null) return;

//            // Access the private _signal field via Harmony Traverser
//            SpriteRenderer signalSR = Traverse.Create(__instance).Field("_signal").GetValue<SpriteRenderer>();

//            if (signalSR != null)
//            {
//                string layerName = SortingLayer.IDToName(signalSR.sortingLayerID);
//                int layerID = signalSR.sortingLayerID;
//                int order = signalSR.sortingOrder;

//                Material mat = signalSR.sharedMaterial;
//                string shaderName = mat != null ? mat.shader.name : "None";
//                int renderQueue = mat != null ? mat.renderQueue : -1;

//                Plugin.Logger.Log("================ [SIGNAL RENDER DATA] ================");
//                Plugin.Logger.Log($"Sorting Layer Name : '{layerName}'");
//                Plugin.Logger.Log($"Sorting Layer ID   : {layerID}");
//                Plugin.Logger.Log($"Sorting Order      : {order}");
//                Plugin.Logger.Log($"Shader Name        : {shaderName}");
//                Plugin.Logger.Log($"Render Queue       : {renderQueue}");
//                Plugin.Logger.Log("=======================================================");

//                _hasLoggedData = true; // Lock to execute only once
//            }
//        }
//    }



//}

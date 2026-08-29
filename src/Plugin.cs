using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShowDetectionRadiusLive
{

    public class Plugin
    {
        
        // Token: 0x1700000A RID: 10
        // (get) Token: 0x06000042 RID: 66 RVA: 0x000032EA File Offset: 0x000014EA
        public static string ModAssemblyName
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        // Token: 0x1700000B RID: 11
        // (get) Token: 0x06000043 RID: 67 RVA: 0x000032FB File Offset: 0x000014FB
        private static string ModPersistenceFolder
        {
            get
            {
                return Path.Combine(Application.persistentDataPath + "/../Quasimorph_ModConfigs", "LoC_ShowDetectionRadiusLive");
            }
        }

        // Token: 0x1700000C RID: 12
        // (get) Token: 0x06000044 RID: 68 RVA: 0x00003316 File Offset: 0x00001516
        private static string ConfigPath
        {
            get
            {
                return Path.Combine(Plugin.ModPersistenceFolder, "config.txt");
            }
        }

        // Token: 0x1700000D RID: 13
        // (get) Token: 0x06000045 RID: 69 RVA: 0x00003327 File Offset: 0x00001527
        private static string SavePath
        {
            get
            {
                return Path.Combine(Plugin.ModPersistenceFolder, "savedata.json");
            }
        }

        // Token: 0x1700000E RID: 14
        // (get) Token: 0x06000046 RID: 70 RVA: 0x00003338 File Offset: 0x00001538
        // (set) Token: 0x06000047 RID: 71 RVA: 0x0000333F File Offset: 0x0000153F
        public static Logger Logger { get; private set; } = new Logger("");

        // Token: 0x1700000F RID: 15
        // (get) Token: 0x06000048 RID: 72 RVA: 0x00003347 File Offset: 0x00001547
        // (set) Token: 0x06000049 RID: 73 RVA: 0x0000334E File Offset: 0x0000154E
        public static ModConfigGeneral ConfigGeneral { get; set; }

        // Token: 0x17000010 RID: 16
        // (get) Token: 0x0600004A RID: 74 RVA: 0x00003356 File Offset: 0x00001556
        // (set) Token: 0x0600004B RID: 75 RVA: 0x0000335D File Offset: 0x0000155D
        public static ModSave Save { get; set; }

        // Token: 0x0600004C RID: 76 RVA: 0x00003368 File Offset: 0x00001568
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {
            Plugin.ConfigGeneral = new ModConfigGeneral("Show Detection Radius", Plugin.ConfigPath);
            Plugin.Save = new ModSave(Plugin.SavePath);
            new Harmony("LoC_" + Plugin.ModAssemblyName).PatchAll();
        }
    }
}

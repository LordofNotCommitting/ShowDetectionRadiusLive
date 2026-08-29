using MGSC;
using ModConfigMenu.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowDetectionRadiusLive
{
    // Token: 0x02000006 RID: 6
    public class ModConfigGeneral
    {
        // Token: 0x0600001D RID: 29 RVA: 0x00002840 File Offset: 0x00000A40



        public ModConfigGeneral(string ModName, string ConfigPath)
        {
            this.ModName = ModName;
            this.ModData = new ModConfigData(ConfigPath);
            this.ModData.AddConfigHeader("General Settings", "general");
            this.ModData.AddConfigValue("general", "Radius_When_Aim", true, "Show Detection Radius W/ Aim", "Enable to Show detection radius in your vision when aiming happens.");
            this.ModData.AddConfigValue("general", "about_final", "<color=#f51b1b>The game must be restarted after setting then saving this config to take effect.</color>\n");

            //this.ModData.AddConfigValue("general", "Debug_Log_On", false, "Debug Log", "For personal debugging. DO NOT TURN IT ON if you don't intend to.");

            this.ModData.RegisterModConfigData(ModName);
        }

        // Token: 0x04000011 RID: 17
        private string ModName;

        // Token: 0x04000012 RID: 18
        public ModConfigData ModData;

    }
}

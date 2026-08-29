using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace ShowDetectionRadiusLive
{
    // Token: 0x0200000C RID: 12
    public class ModSave
    {
        // Token: 0x17000009 RID: 9
        // (get) Token: 0x0600002E RID: 46 RVA: 0x00002D0C File Offset: 0x00000F0C
        // (set) Token: 0x0600002F RID: 47 RVA: 0x00002D14 File Offset: 0x00000F14
        public int CurrentSlot { get; set; }

        // Token: 0x06000030 RID: 48 RVA: 0x00002D1D File Offset: 0x00000F1D
        public ModSave(string savePath)
        {
            this.SavePath = savePath;
            this.SaveData = this.LoadFromDisk();
            this.CurrentSlot = 0;
        }

        // Token: 0x06000031 RID: 49 RVA: 0x00002D40 File Offset: 0x00000F40
        public T GetCurrentSlotValue<T>(string key)
        {
            return this.GetSlot(this.CurrentSlot).GetValue<T>(key, default(T));
        }

        // Token: 0x06000032 RID: 50 RVA: 0x00002D68 File Offset: 0x00000F68
        public void SetCurrentSlotValue<T>(string key, T value)
        {
            this.GetSlot(this.CurrentSlot).SetValue<T>(key, value);
        }

        // Token: 0x06000033 RID: 51 RVA: 0x00002D80 File Offset: 0x00000F80
        public SlotData GetSlot(int slot)
        {
            SlotData slotData;
            if (!this.SaveData.TryGetValue(slot, out slotData))
            {
                slotData = new SlotData(null);
                this.SaveData[slot] = slotData;
            }
            return slotData;
        }

        // Token: 0x06000034 RID: 52 RVA: 0x00002DB2 File Offset: 0x00000FB2
        public void ClearSlot(int slot)
        {
            this.SaveData[slot] = new SlotData(null);
        }

        // Token: 0x06000035 RID: 53 RVA: 0x00002DC8 File Offset: 0x00000FC8
        public void SaveToDisk()
        {
            JSONObject jsonobject = new JSONObject();
            foreach (KeyValuePair<int, SlotData> keyValuePair in this.SaveData)
            {
                jsonobject[keyValuePair.Key.ToString()] = keyValuePair.Value.ToJson();
            }
            File.WriteAllText(this.SavePath, jsonobject.ToString());
        }

        // Token: 0x06000036 RID: 54 RVA: 0x00002E4C File Offset: 0x0000104C
        public Dictionary<int, SlotData> LoadFromDisk()
        {
            if (!File.Exists(this.SavePath))
            {
                this.SaveData = new Dictionary<int, SlotData>();
                return this.SaveData;
            }
            JSONObject jsonobject = JSON.Parse(File.ReadAllText(this.SavePath)) as JSONObject;
            Dictionary<int, SlotData> dictionary = new Dictionary<int, SlotData>();
            foreach (string text in jsonobject.Keys)
            {
                int key;
                if (int.TryParse(text, out key))
                {
                    dictionary[key] = new SlotData(jsonobject[text]);
                }
                else
                {
                    Plugin.Logger.LogWarning("Incorrect key encountered while loading json data: " + text);
                }
            }
            this.SaveData = dictionary;
            return this.SaveData;
        }

        // Token: 0x04000022 RID: 34
        private string SavePath;

        // Token: 0x04000023 RID: 35
        private Dictionary<int, SlotData> SaveData;
    }
}

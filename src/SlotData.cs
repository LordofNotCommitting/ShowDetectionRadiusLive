using System;
using System.Collections.Generic;
using SimpleJSON;

namespace ShowDetectionRadiusLive
{
    // Token: 0x0200000D RID: 13
    public class SlotData
    {
        // Token: 0x06000037 RID: 55 RVA: 0x00002EFC File Offset: 0x000010FC
        public SlotData(JSONNode node = null)
        {
            this.slotData = new Dictionary<string, SlotEntry>();
            if (node != null)
            {
                foreach (string text in node.Keys)
                {
                    this.slotData[text] = new SlotEntry(node[text]);
                }
            }
        }

        // Token: 0x06000038 RID: 56 RVA: 0x00002F60 File Offset: 0x00001160
        public void SetValue<T>(string key, T value)
        {
            ValType valType;
            if (value is bool)
            {
                valType = ValType.Bool;
            }
            else if (value is int)
            {
                valType = ValType.Int;
            }
            else if (value is float)
            {
                valType = ValType.Float;
            }
            else if (value is string)
            {
                valType = ValType.String;
            }
            else
            {
                valType = ValType.Unsupported;
            }
            if (valType != ValType.Unsupported)
            {
                this.slotData[key] = new SlotEntry(value, valType);
                return;
            }
            Plugin.Logger.LogError("[SlotEntry] Unsupported type for SlotData is being used for key:" + key);
            this.slotData[key] = new SlotEntry(0, ValType.Int);
        }

        // Token: 0x06000039 RID: 57 RVA: 0x00003000 File Offset: 0x00001200
        public T GetValue<T>(string key, T fallback = default(T))
        {
            SlotEntry slotEntry;
            if (this.slotData.TryGetValue(key, out slotEntry))
            {
                return slotEntry.GetValue<T>(fallback);
            }
            Plugin.Logger.LogWarning("Unable to get value for key:" + key + " | Data not found.");
            return fallback;
        }

        // Token: 0x0600003A RID: 58 RVA: 0x00003040 File Offset: 0x00001240
        public bool Contains(string key)
        {
            return this.slotData.ContainsKey(key);
        }

        // Token: 0x0600003B RID: 59 RVA: 0x0000304E File Offset: 0x0000124E
        public bool Remove(string key)
        {
            return this.slotData.Remove(key);
        }

        // Token: 0x0600003C RID: 60 RVA: 0x0000305C File Offset: 0x0000125C
        public JSONNode ToJson()
        {
            JSONObject jsonobject = new JSONObject();
            foreach (KeyValuePair<string, SlotEntry> keyValuePair in this.slotData)
            {
                jsonobject[keyValuePair.Key] = keyValuePair.Value.ToJson();
            }
            return jsonobject;
        }

        // Token: 0x04000024 RID: 36
        private Dictionary<string, SlotEntry> slotData;
    }
}

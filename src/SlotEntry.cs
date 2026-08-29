using System;
using SimpleJSON;

namespace ShowDetectionRadiusLive
{
    // Token: 0x0200000E RID: 14
    public class SlotEntry
    {
        // Token: 0x0600003D RID: 61 RVA: 0x000030C8 File Offset: 0x000012C8
        public SlotEntry(object value, ValType type)
        {
            this.Value = value;
            this.Type = type;
        }

        // Token: 0x0600003E RID: 62 RVA: 0x000030E0 File Offset: 0x000012E0
        public SlotEntry(JSONNode node)
        {
            try
            {
                this.Type = (ValType)Enum.Parse(typeof(ValType), node["Type"]);
                switch (this.Type)
                {
                    case ValType.Bool:
                        this.Value = node["Value"].AsBool;
                        break;
                    case ValType.Int:
                        this.Value = int.Parse(node["Value"]);
                        break;
                    case ValType.Float:
                        this.Value = float.Parse(node["Value"]);
                        break;
                    case ValType.String:
                        this.Value = node["Value"];
                        break;
                }
            }
            catch (Exception arg)
            {
                this.Value = 0;
                this.Type = ValType.Int;
                Plugin.Logger.LogError(string.Format("[SlotEntry] Deserialization error: {0}", arg));
            }
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000031EC File Offset: 0x000013EC
        public void SetValue(object value, ValType type)
        {
            this.Value = value;
            this.Type = type;
        }

        // Token: 0x06000040 RID: 64 RVA: 0x000031FC File Offset: 0x000013FC
        public T GetValue<T>(T fallback = default(T))
        {
            object value = this.Value;
            if (value is T)
            {
                return (T)((object)value);
            }
            return fallback;
        }

        // Token: 0x06000041 RID: 65 RVA: 0x00003224 File Offset: 0x00001424
        public JSONNode ToJson()
        {
            JSONObject jsonobject = new JSONObject();
            jsonobject["Type"] = this.Type.ToString();
            switch (this.Type)
            {
                case ValType.Bool:
                    jsonobject["Value"] = (bool)this.Value;
                    break;
                case ValType.Int:
                    jsonobject["Value"] = (int)this.Value;
                    break;
                case ValType.Float:
                    jsonobject["Value"] = (float)this.Value;
                    break;
                case ValType.String:
                    jsonobject["Value"] = (string)this.Value;
                    break;
            }
            return jsonobject;
        }

        // Token: 0x04000025 RID: 37
        public object Value;

        // Token: 0x04000026 RID: 38
        public ValType Type;
    }
}

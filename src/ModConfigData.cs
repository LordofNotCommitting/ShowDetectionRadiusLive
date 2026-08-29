using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MGSC;
using ModConfigMenu;
using ModConfigMenu.Contracts;
using ModConfigMenu.Implementations;
using ModConfigMenu.Objects;
using ModConfigMenu.Services;
using UnityEngine;

namespace ShowDetectionRadiusLive
{
    public class ModConfigData
    {
        public ModConfigData(string ConfigPath)
        {
            this.ConfigPath = ConfigPath;
            this.Settings = new Dictionary<string, object>();
            this.ConfigValues = new List<IConfigValue>();
            this.LoadConfig();
        }

        public void RegisterModConfigData(string menuName)
        {
            ModConfigMenuAPI.RegisterModConfig(menuName, this.ConfigValues, new ModConfigMenuAPI.ConfigStoredDelegate(this.OnSave));
        }

        public void AddConfigHeader(string headerKey, string locKey = null)
        {
            //this.GetKeyEnsureLocalization(headerKey, ModConfigData.KeyType.Header, locKey);
        }

        public void AddConfigValue(string headerKey, string valueKey, string stringKey)
        {
            StringConfig item = new StringConfig(valueKey, stringKey, headerKey);
            this.ConfigValues.Add(item);
        }

        public void AddConfigValue(string headerKey, string valueKey, object defaultValue, string labelKey, string tooltipKey)
        {
            if (!this.Settings.ContainsKey(valueKey))
            {
                this.Settings.Add(valueKey, defaultValue);
            }
            ConfigValue item = new ConfigValue(valueKey, this.Settings[valueKey], headerKey, defaultValue, tooltipKey, labelKey);
            this.ConfigValues.Add(item);
        }

        public void AddConfigValue(string headerKey, string valueKey, int defaultValue, int min, int max, string labelKey, string tooltipKey)
        {
            if (!this.Settings.ContainsKey(valueKey))
            {
                this.Settings.Add(valueKey, defaultValue);
            }
            RangeConfig<int> item = new RangeConfig<int>(valueKey, this.GetConfigValue<int>(valueKey, 0), defaultValue, min, max, headerKey, tooltipKey, labelKey);
            this.ConfigValues.Add(item);
        }

        public void AddConfigValue(string headerKey, string valueKey, string defaultValue, List<object> valueList, string labelKey, string tooltipKey)
        {
            if (!this.Settings.ContainsKey(valueKey))
            {
                this.Settings.Add(valueKey, defaultValue);
            }
            DropdownConfig item = new DropdownConfig(valueKey, this.GetConfigValue<string>(valueKey, null), headerKey, defaultValue, tooltipKey, labelKey, valueList);
            this.ConfigValues.Add(item);
        }

        public T GetConfigValue<T>(string key, T fallback = default(T))
        {
            object value;
            if (this.Settings.TryGetValue(key, out value))
            {
                try
                {
                    return (T)((object)Convert.ChangeType(value, typeof(T)));
                }
                catch
                {
                    return fallback;
                }
                return fallback;
            }
            return fallback;
        }

        public T GetDropdownValue<T>(string key, T fallback = default(T))
        {
            object obj;
            if (this.Settings.TryGetValue(key, out obj))
            {
                try
                {
                    return (T)((object)Convert.ChangeType(obj, typeof(T)));
                }
                catch
                {
                    string text = obj as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        Match match = Regex.Match(text, "^(\\d+)\\.");
                        int num;
                        if (match.Success && int.TryParse(match.Groups[1].Value, out num))
                        {
                            num--;
                            try
                            {
                                return (T)((object)Convert.ChangeType(num, typeof(T)));
                            }
                            catch
                            {
                                return fallback;
                            }
                        }
                    }
                    return fallback;
                }
                return fallback;
            }
            return fallback;
        }

        public TEnum GetEnumValue<TEnum>(string key, TEnum fallback = default(TEnum)) where TEnum : struct, Enum
        {
            Debug.Log("START " + key);
            string configValue = this.GetConfigValue<string>(key, null);
            if (string.IsNullOrEmpty(configValue))
            {
                return fallback;
            }
            TEnum result;
            try
            {
                Debug.Log("try start");
                int num = configValue.IndexOf('.');
                int num2;
                if (num <= 0)
                {
                    Debug.Log("RETURN INDEX DOT");
                    result = fallback;
                }
                else if (int.TryParse(configValue.Substring(0, num), out num2))
                {
                    Debug.Log("PRASED numberPart");
                    num2--;
                    TEnum[] array = (TEnum[])Enum.GetValues(typeof(TEnum));
                    if (num2 < 0)
                    {
                        Debug.Log("index < 0");
                        result = fallback;
                    }
                    else if (num2 >= array.Length)
                    {
                        Debug.Log("index >= values.Length");
                        result = array[array.Length - 1];
                    }
                    else
                    {
                        Debug.Log(string.Format("RETURNING INDEX {0} for {1}", num2, key));
                        result = array[num2];
                    }
                }
                else
                {
                    Debug.Log("NOT PRASED numberPart");
                    result = fallback;
                }
            }
            catch
            {
                Debug.Log("RETURN CATCH");
                result = fallback;
            }
            return result;
        }

        private void CreateConfig()
        {
            if (!File.Exists(this.ConfigPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.ConfigPath));
                File.Create(this.ConfigPath).Close();
            }
        }

        private void LoadConfig()
        {
            if (!File.Exists(this.ConfigPath))
            {
                this.CreateConfig();
                return;
            }
            foreach (string text in File.ReadAllLines(this.ConfigPath))
            {
                if (!text.StartsWith("#") && !string.IsNullOrWhiteSpace(text))
                {
                    string[] array2 = text.Split(new char[]
                    {
                        '='
                    });
                    if (array2.Length == 2)
                    {
                        string key = array2[0].Trim();
                        string text2 = array2[1].Trim();
                        int num;
                        float num2;
                        bool flag;
                        if (int.TryParse(text2, out num))
                        {
                            this.Settings.Add(key, num);
                        }
                        else if (float.TryParse(text2, out num2))
                        {
                            this.Settings.Add(key, num2);
                        }
                        else if (bool.TryParse(text2, out flag))
                        {
                            this.Settings.Add(key, flag);
                        }
                        else
                        {
                            this.Settings.Add(key, text2);
                        }
                    }
                }
            }
        }

        private void SaveConfig()
        {
            if (!File.Exists(this.ConfigPath))
            {
                this.CreateConfig();
            }
            File.WriteAllLines(this.ConfigPath, from entry in this.Settings
                                                select string.Format("{0}={1}", entry.Key, entry.Value));
        }

        protected virtual bool OnSave(Dictionary<string, object> newConfig, out string feedbackMessage)
        {
            feedbackMessage = "Saving";
            this.Settings = newConfig;
            this.SaveConfig();
            return true;
        }

        private string ConfigPath;

        private Dictionary<string, object> Settings;

        private List<IConfigValue> ConfigValues;

        public enum KeyType
        {
            Header,
            Label,
            Tooltip,
            Description
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceSnippet {
    public class Config {
        private static string NameTag = "NAME";
        private static string WakeTag = "WAKE";
        private static string SleepTag = "SLEEP";
        private static List<string> RequiredConfigs = new List<string> { NameTag, WakeTag, SleepTag };
        private static string ConfigFile = "configs.json";
        private static int MaxValueLength = 30;
        public string Setting { get; set; }
        public string Value { get; set; }
        public Config() { 
            Setting = "";
            Value = "";
        }
        public Config(string setting, string value) {
            Setting = setting;
            Value = value;
        }
        public class ConfigHolder {
            public List<Config> Configs { get; set; }
            public ConfigHolder() {
                Configs = new List<Config>();
            }
            public string GetValue(string setting) {
                return Configs.FirstOrDefault(c => c.Setting.ToUpper() == setting.ToUpper())?.Value;
            }
        }
        public static bool ReadConfigs(out ConfigHolder result, out List<string> issues) {
            issues = new List<string>();
            string json = File.ReadAllText(ConfigFile);
            Config.ConfigHolder configHolder = JsonConvert.DeserializeObject<ConfigHolder>(json);

            if (configHolder == null || configHolder.Configs == null || configHolder.Configs.Count == 0) {
                issues.Add($"Unable to load {ConfigFile} or it was empty");
                result = null;
                return false;
            }

            foreach (string s in RequiredConfigs) {
                if (!configHolder.Configs.Any(c => c.Setting.ToUpper() == s)) {
                    issues.Add($"Required config {s} not found");
                    result = null;
                    return false;
                }
                string v = configHolder.GetValue(s);
                if(string.IsNullOrEmpty(v)) {
                    issues.Add($"Required config {s} is null or empty");
                    result = null;
                    return false;
                }
                if(v.Length > MaxValueLength) {
                    issues.Add($"Required config {s} is too long, limit is {MaxValueLength}");
                    result = null;
                    return false;
                }
            }

            result = configHolder;
            return true;
        }
        public static string GetWakePhrase(ConfigHolder holder) {
            string name = holder.GetValue(NameTag);
            string wake = holder.GetValue(WakeTag);
            return name + " " + wake;
        }
        public static string GetSleepPhrase(ConfigHolder holder) {
            string name = holder.GetValue(NameTag);
            string sleep = holder.GetValue(SleepTag);
            return name + " " + sleep;
        }
        public static bool IsWakePhrase(string phrase, ConfigHolder holder) {
            string wake = GetWakePhrase(holder);
            return phrase.ToUpper() == wake.ToUpper();
        }
        public static bool IsSleepPhrase(string phrase, ConfigHolder holder) {
            string sleep = GetSleepPhrase(holder);
            return phrase.ToUpper() == sleep.ToUpper();
        }
        public static List<string> PrintConfigs(ConfigHolder holder) {
            List<string> result = new List<string>();
            foreach (Config c in holder.Configs) {
                result.Add($"Config: {c.Setting} = {c.Value}");
            }
            result.Add(Config.GetWakePhrase(holder));
            result.Add(Config.GetSleepPhrase(holder));
            return result;
        }
    }
}

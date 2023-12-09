using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceSnippet {
    public class Config {
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
        }
    }
}

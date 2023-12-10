using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceSnippet {
    public class Command
        {

        public string Voice { get; set; }
        public List<KeyAction> Actions { get; set; }

        public Command() {
            Voice = "";
            Actions = new List<KeyAction>();
        }

        public class KeyAction {
            public string Action { get; set; }
            public string Key { get; set; }
            public List<string> Modifiers { get; set; }
            public string Text { get; set; }
            public KeyAction() {
                Action = "";
                Key = "";
                Modifiers = new List<string>();
                Text = "";
            }
            public string ToString() {
                return "Action: " + Action + " Key: " + Key + " Modifier: " + ModifierToString() + " Text: " + Text;
            }
            public string ModifierToString() {
                if(Modifiers == null || Modifiers.Count == 0) {
                    return "";
                } else {
                    return string.Join(",", Modifiers);
                }
            }
            public bool IsEmpty() {
                return Action == "" && Key == "" && Text == "" && (Modifiers == null || Modifiers.Count == 0);
            }
        }

        public class CommandHolder {
            public List<Command> Commands { get; set; }
            public CommandHolder() {
                Commands = new List<Command>();
            }
        }
    }
}

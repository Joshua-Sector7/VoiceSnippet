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
            public string Modifier { get; set; }
            public string Text { get; set; }
            public KeyAction() {
                Action = "";
                Key = "";
                Modifier = "";
                Text = "";
            }
            public string ToString() {
                return "Action: " + Action + " Key: " + Key + " Modifier: " + Modifier + " Text: " + Text;
            }
            public bool IsEmpty() {
                return Action == "" && Key == "" && Modifier == "" && Text == "";
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

﻿using System.Speech.Recognition;
using System.Speech.Synthesis;
using Newtonsoft.Json;
using WindowsInput;
using WindowsInput.Native;

namespace VoiceSnippet {
    public class Program {
        //private static string configFile = "configs.json";
        private static string commandFile = "commands.json";
        //private static List<string> RequiredConfigs = new List<string> { "NAME", "WAKE", "SLEEP", "SWITCH" };
        private static Config.ConfigHolder CfgHolder = new Config.ConfigHolder();
        private static Command.CommandHolder CmdHolder = new Command.CommandHolder();
        private static SpeechSynthesizer Synth = new SpeechSynthesizer();
        private static SpeechRecognitionEngine recognizer;
        private static bool KeepAlive = true;

        //private static Command.CommandHolder CmdHolder = new Command.CommandHolder();
        private static InputSimulator InSim = new InputSimulator();
        private static List<string> VoiceCommands = new List<string>();
        //private static CommandMap CmdMap = new CommandMap();
        static void Main(string[] args) {
            List<string> temp = new List<string>();
            if (!Config.ReadConfigs(out CfgHolder, out temp)) {
                Print(temp);
                return;
            }
            Print(Config.PrintConfigs(CfgHolder));

            if (!ReadCommands()) {
                return;
            }
            PrintCommands();

            VoiceCommands.Add(Config.GetWakePhrase(CfgHolder));
            VoiceCommands.Add(Config.GetSleepPhrase(CfgHolder));

            Console.WriteLine($"Total commads found {VoiceCommands.Count}");

            foreach (InstalledVoice voice in Synth.GetInstalledVoices()) {
                VoiceInfo info = voice.VoiceInfo;
                Console.WriteLine("Voice Name: " + info.Name);
            }

            Synth.SelectVoice("Microsoft Zira Desktop");
            Synth.Speak("Hello, my name is Zira");
            recognizer = new SpeechRecognitionEngine();

            // Define the wake words
            Choices voiceLibrary = new Choices(VoiceCommands.ToArray());
            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(voiceLibrary);
            Grammar voiceLibraryGrammer = new Grammar(gb);

            // Load the grammar
            recognizer.LoadGrammar(voiceLibraryGrammer);

            // Attach event handlers
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;

            try {
                // Set the input to the default audio device
                recognizer.SetInputToDefaultAudioDevice();
            } catch (Exception) {
                Console.WriteLine($"Unable to set input to default audio device");
                return;
            }

            // Start asynchronous recognition
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            Console.WriteLine("Press any key to exit... 3");
            Console.ReadKey();
            Console.WriteLine("Press any key to exit... 2");
            Console.ReadKey();
            Console.WriteLine("Press any key to exit... 1");
            Console.ReadKey();

            KeepAlive = false;

            // Stop recognition
            recognizer.RecognizeAsyncStop();
        }

        private static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
            DateTime dt = DateTime.Now;
            Console.WriteLine($"{dt.ToString()}: Recognized text: {e.Result.Text}");
            //Synth.Speak(e.Result.Text);
            Synth.SpeakAsync(e.Result.Text);

            if(Config.IsWakePhrase(e.Result.Text, CfgHolder)) {
                Synth.SpeakAsync("wake");
            } else if (Config.IsSleepPhrase(e.Result.Text, CfgHolder)) {
                Synth.SpeakAsync("sleep");
            } else {
                ExecuteCommand(GetCommand(e.Result.Text));
            }
        }

        private static Command GetCommand(string commandVoice) {
            return CmdHolder.Commands.FirstOrDefault(c => c.Voice == commandVoice);
        }

        private static void ExecuteCommand(Command command) {
            if(command == null) {
                Console.WriteLine($"Command not found...");
                return;
            }

            Console.WriteLine($"Executing command {command.Voice}");

            Command.KeyAction currentAction = null;

            try {
                foreach (var action in command.Actions) {
                    currentAction = action;
                    switch (action.Action) {
                        case "keyPress":
                            InSim.Keyboard.KeyPress((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Key));
                            break;

                        case "modifiedKeyStroke":
                            // Assuming 'action' is the current action being processed
                            // and it has a 'List<string> Modifiers' and a 'string Key'

                            List<VirtualKeyCode> modifierKeys = action.Modifiers
                                .Select(modifier => (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), modifier))
                                .ToList();

                            VirtualKeyCode mainKey = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Key);

                            // Now apply the modifiers and the main key
                            // This approach assumes that you have modified the InSim.Keyboard.ModifiedKeyStroke 
                            // method to accept a list of modifiers
                            InSim.Keyboard.ModifiedKeyStroke(modifierKeys, mainKey);

                            //InSim.Keyboard.ModifiedKeyStroke(
                            //    (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Modifier),
                            //    (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Key));
                            break;

                        case "textEntry":
                            //InSim.Keyboard.TextEntry(action.Text);
                            HandleTextEntry(action.Text);
                            break;
                    }
                }
            } catch (Exception) {
                if(currentAction != null) {
                    Console.WriteLine($"Error executing command {command.Voice} action {currentAction.ToString()}");
                } else {
                    Console.WriteLine($"Error executing action for {command.Voice}");
                }
            }
        }

        private static void HandleTextEntry(string text) {
            if (text.Contains("\n")) {
                string[] strings = text.Split("\n");
                foreach(string s in strings) {
                    InSim.Keyboard.TextEntry(s);
                    InSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                }
            } else {
                InSim.Keyboard.TextEntry(text);
            }
        }   

        private static void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e) {
            DateTime dt = DateTime.Now;

            Console.WriteLine($"{dt.ToString()}: Recognition completed.");

            if (KeepAlive) {
                Synth.SpeakAsync("Timeout, restarting");
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            } else {
                Synth.Speak("Goodbye");
            }
        }

        private static bool ReadCommands() {
            string json = File.ReadAllText(commandFile);

            Command.CommandHolder ch = JsonConvert.DeserializeObject<Command.CommandHolder>(json);

            if(ch == null || ch.Commands == null || ch.Commands.Count == 0) {
                Console.WriteLine($"Unable to load {commandFile} or it was empty");
                return false;
            }

            bool badFile = false;
            foreach(Command c in ch.Commands) {
                if (string.IsNullOrEmpty(c.Voice)) {
                    Console.WriteLine($"Command missing voice value");
                    badFile = true;
                } else if (c.Actions == null || c.Actions.Count == 0) {
                    Console.WriteLine($"Command {c.Voice} missing or corrupt action");
                    badFile = true;
                } else {
                    for(int i = 0; i < c.Actions.Count; i++) {
                        if (c.Actions[i].IsEmpty()) {
                            Console.WriteLine($"Command {c.Voice} action {i+1} invalid");
                            badFile = true;
                        } else {
                            if (c.Actions[i].Action == "modifiedKeyStroke") {
                                if (c.Actions[i].Modifiers == null || c.Actions[i].Modifiers.Count == 0) {
                                    Console.WriteLine($"Command {c.Voice} action {i + 1} missing modifier");
                                    badFile = true;
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(c.Voice)) {
                    if(VoiceCommands.Contains(c.Voice.ToLower())) {
                        Console.WriteLine($"Duplicate voice command {c.Voice} found");
                        badFile = true;
                    } else {
                        VoiceCommands.Add(c.Voice.ToLower());
                    }
                }
            }

            if (badFile) {
                return false;
            }

            CmdHolder = ch;
            return true;
        }

        private static void PrintCommands() {
            foreach (Command c in CmdHolder.Commands) {
                foreach(Command.KeyAction ka in c.Actions) {
                    Console.WriteLine($"Command: {c.Voice} {ka.ToString()}");
                }
            }
        }
        private static void Print(List<string> data) {
            foreach (string s in data) {
                Console.WriteLine(s);
            }
        }
    }
}

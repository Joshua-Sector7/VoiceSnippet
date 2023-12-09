using System.Speech.Recognition;
using Newtonsoft.Json;
using WindowsInput;
using WindowsInput.Native;

namespace VoiceSnippet {
    public class Program {
        private static string configFile = "configs.json";
        private static string commandFile = "commands.json";
        private static List<string> RequiredConfigs = new List<string> { "NAME", "WAKE", "SLEEP", "SWITCH" };
        private static Config.ConfigHolder CfgHolder = new Config.ConfigHolder();
        private static Command.CommandHolder CmdHolder = new Command.CommandHolder();

        //private static Command.CommandHolder CmdHolder = new Command.CommandHolder();
        private static InputSimulator InSim = new InputSimulator();
        private static List<string> VoiceCommands = new List<string>();
        //private static CommandMap CmdMap = new CommandMap();
        static void Main(string[] args) {
            if (!ReadConfigs()) {
                return;
            }
            PrintConfigs();

            if (!ReadCommands()) {
                return;
            }
            PrintCommands();

            Console.WriteLine($"Total commads found {VoiceCommands.Count}");

            SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();

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

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            // Stop recognition
            recognizer.RecognizeAsyncStop();
        }

        private static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
            Console.WriteLine($"Recognized text: {e.Result.Text}");
            ExecuteCommand(GetCommand(e.Result.Text));
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
                            InSim.Keyboard.ModifiedKeyStroke(
                                (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Modifier),
                                (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Key));
                            break;

                        case "textEntry":
                            InSim.Keyboard.TextEntry(action.Text);
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

        private static void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e) {
            Console.WriteLine("Recognition completed.");
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
                    Console.WriteLine($"Command: {c.Voice} = {ka.Action} {ka.Key} {ka.Modifier} {ka.Text}");
                }
            }
        }

        private static bool ReadConfigs() {
            string json = File.ReadAllText(configFile);
            Config.ConfigHolder configHolder = JsonConvert.DeserializeObject<Config.ConfigHolder>(json);

            if (configHolder == null || configHolder.Configs == null || configHolder.Configs.Count == 0) {
                Console.WriteLine($"Unable to load {configFile} or it was empty");
                return false;
            }

            foreach (string s in RequiredConfigs) {
                if (!configHolder.Configs.Any(c => c.Setting.ToUpper() == s)) {
                    Console.WriteLine($"Required config {s} not found");
                    return false;
                }
            }

            CfgHolder = configHolder;
            return true;
        }

        private static void PrintConfigs() {
            foreach (Config c in CfgHolder.Configs) {
                Console.WriteLine($"Config: {c.Setting} = {c.Value}");
            }
        }
    }
}

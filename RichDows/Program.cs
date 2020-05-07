
using DiscordRPC.Message;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace DiscordRPC.Example
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        /// <summary>
        /// The level of logging to use.
        /// </summary>
        private static Logging.LogLevel logLevel = Logging.LogLevel.Trace;

        /// <summary>
        /// The pipe to connect too.
        /// </summary>
        private static int discordPipe = -1;

        /// <summary>
        /// The current presence to send to discord.
        /// </summary>
        private static RichPresence presence = new RichPresence()
        {
            Details = "Preparing...",
            Assets = new Assets()
            {
                LargeImageKey = "raingold",
                LargeImageText = "RichDows v0.1a by Colean",
            }
        };

        /// <summary>
        /// The discord client
        /// </summary>
        private static DiscordRpcClient client;

        /// <summary>
        /// Is the main loop currently running?
        /// </summary>
        private static bool isRunning = true;

        /// <summary>
        /// The string builder for the command
        /// </summary>
        private static StringBuilder word = new StringBuilder();


        static string GetActiveWindow()
        {
            const int nChars = 256;
            IntPtr handle;
            StringBuilder Buff = new StringBuilder(nChars);

            handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return "No Window Title";
        }

        private static Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            return hwnd != null ? GetProcessByHandle(hwnd) : null;
        }
        private static Process GetProcessByHandle(IntPtr hwnd)
        {
            try
            {
                uint processID;
                GetWindowThreadProcessId(hwnd, out processID);
                return Process.GetProcessById((int)processID);
            }
            catch { return null; }
        }
        //Main Loop
        static void Main(string[] args)
        {
            //Reads the arguments for the pipe
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-pipe":
                        discordPipe = int.Parse(args[++i]);
                        break;

                    default: break;
                }
            }

            //Seting a random details to test the update rate of the presence
            //BasicExample();
            FullClientExample();

            Console.WriteLine("Press any key to terminate");
            Console.ReadKey();
        }
        static void FullClientExample()
        {
            using (client = new DiscordRpcClient("707975437174833165",
                    pipe: discordPipe,                                          
                    logger: new Logging.ConsoleLogger(logLevel, true),        
                    autoEvents: true,                                           
                    client: new IO.ManagedNamedPipeClient()                     
                ))
            {


                client.OnReady += OnReady;                                     
                client.OnClose += OnClose;                                     
                client.OnError += OnError;                                     

                client.OnConnectionEstablished += OnConnectionEstablished;     
                client.OnConnectionFailed += OnConnectionFailed;                

                client.OnPresenceUpdate += OnPresenceUpdate;                    

                client.OnSubscribe += OnSubscribe;                              
                client.OnUnsubscribe += OnUnsubscribe;                                           

                client.SetPresence(presence);

                client.Initialize();

                Console.Title = "RichDows Console";
                MainLoop();
                
            }
        }
        static void MainLoop()
        {
            isRunning = true;
            while (client != null && isRunning)
            {
                Process currentProcess = GetActiveProcess();
                client.SetPresence(new RichPresence()
                { 
                    Details = GetActiveWindow(),
                    State = currentProcess.ProcessName,
                    Assets = new Assets()
                    {
                        LargeImageKey = "raingold",
                        LargeImageText = "RichDows v0.1a by Colean",
                    }
                });

                if (Console.KeyAvailable)
                    ProcessKey();

                Thread.Sleep(25);
            }

            Console.WriteLine("Press any key to terminate");
            Console.ReadKey();
        }

        #region Events

        #region State Events
        private static void OnReady(object sender, ReadyMessage args)
        {
            presence.Timestamps = Timestamps.Now;
            Console.WriteLine("On Ready. RPC Version: {0}", args.Version);

        }
        private static void OnClose(object sender, CloseMessage args)
        {
            Console.WriteLine("Lost Connection with client because of '{0}'", args.Reason);
        }
        private static void OnError(object sender, ErrorMessage args)
        {
            Console.WriteLine("Error occured within discord. ({1}) {0}", args.Message, args.Code);
        }
        #endregion

        #region Pipe Connection Events
        private static void OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {

            Console.WriteLine("Pipe Connection Established. Valid on pipe #{0}", args.ConnectedPipe);
        }
        private static void OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            Console.WriteLine("Pipe Connection Failed. Could not connect to pipe #{0}", args.FailedPipe);
            isRunning = false;
        }
        #endregion

        private static void OnPresenceUpdate(object sender, PresenceMessage args)
        {
            Console.WriteLine("Rich Presence Updated. Playing {0}", args.Presence == null ? "Nothing (NULL)" : args.Presence.State);
        }

        #region Subscription Events
        private static void OnSubscribe(object sender, SubscribeMessage args)
        {
            Console.WriteLine("Subscribed: {0}", args.Event);
        }
        private static void OnUnsubscribe(object sender, UnsubscribeMessage args)
        {
            Console.WriteLine("Unsubscribed: {0}", args.Event);
        }
        #endregion

        
        #endregion


        static int cursorIndex = 0;
        static string previousCommand = "";
        static void ProcessKey()
        {
            //Read they key
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    //Write the new line
                    Console.WriteLine();
                    cursorIndex = 0;

                    //The enter key has been sent, so send the message
                    previousCommand = word.ToString();
                    ExecuteCommand(previousCommand);

                    word.Clear();
                    break;

                case ConsoleKey.Backspace:
                    word.Remove(cursorIndex - 1, 1);
                    Console.Write("\r                                         \r");
                    Console.Write(word);
                    cursorIndex--;
                    break;

                case ConsoleKey.Delete:
                    if (cursorIndex < word.Length)
                    {
                        word.Remove(cursorIndex, 1);
                        Console.Write("\r                                         \r");
                        Console.Write(word);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    cursorIndex--;
                    break;

                case ConsoleKey.RightArrow:
                    cursorIndex++;
                    break;

                case ConsoleKey.UpArrow:
                    word.Clear().Append(previousCommand);
                    Console.Write("\r                                         \r");
                    Console.Write(word);
                    break;

                default:
                    if (!Char.IsControl(key.KeyChar))
                    {
                        //Some other character key was sent
                        Console.Write(key.KeyChar);
                        word.Insert(cursorIndex, key.KeyChar);
                        Console.Write("\r                                         \r");
                        Console.Write(word);
                        cursorIndex++;
                    }
                    break;
            }

            if (cursorIndex < 0) cursorIndex = 0;
            if (cursorIndex >= Console.BufferWidth) cursorIndex = Console.BufferWidth - 1;
            Console.SetCursorPosition(cursorIndex, Console.CursorTop);
        }

        static void ExecuteCommand(string word)
        {
            //Trim the extra spacing
            word = word.Trim();

            //Prepare the command and its body
            string command = word;
            string body = "";

            //Split the command and the values.
            int whitespaceIndex = word.IndexOf(' ');
            if (whitespaceIndex >= 0)
            {
                command = word.Substring(0, whitespaceIndex);
                if (whitespaceIndex < word.Length)
                    body = word.Substring(whitespaceIndex + 1);
            }

            //Parse the command
            switch (command.ToLowerInvariant())
            {
                case "close":
                    client.Dispose();
                    System.Environment.Exit(1);
                    break;

                case "help":
                    Console.WriteLine("Available Commands: close");
                    break;

                default:
                    Console.WriteLine("Unkown Command '{0}'. Try 'help' for a list of commands", command);
                    break;
            }

        }

    }
}

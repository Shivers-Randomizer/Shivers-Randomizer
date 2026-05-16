using Shivers_Randomizer.enums;
using Shivers_Randomizer.TwitchIntegration;
using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static Shivers_Randomizer.utils.MemoryMap;

namespace Shivers_Randomizer;

public partial class TwitchIntegrationMain : Window
{
    private readonly App app;

    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;

    private string chatbotusername;
    private string token;
    private string channel;

    public event Action<string, string> OnChatMessage;
    private string? reply;
    private readonly Random rng = new Random();
    public bool hasJoinedChannel;
    private bool IsConnected => client != null && client.Connected;
    private ObservableCollection<ChatUser> observableUsers = new ObservableCollection<ChatUser>();
    private DispatcherTimer pointsTimer;
    public int pointsPerMinute = 10;
    private readonly TimeSpan timerInterval = TimeSpan.FromMinutes(1);
    private bool isGameRunning = false;

    public TwitchIntegrationMain(App app)
    {
        InitializeComponent();
        listViewUsers.ItemsSource = observableUsers;
        this.app = app;
    }

public class ChatUser : INotifyPropertyChanged
{
    private int points;
    public string Name { get; set; }
    public int Points
    {
        get => points;
        set
        {
            if (points != value)
            {
                points = value;
                OnPropertyChanged(nameof(Points));
            }
        }
    }
    public ChatUser(string name)
    {
        Name = name;
        Points = 0;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


    private Dictionary<string, ChatUser> activeUsers = new Dictionary<string, ChatUser>();

    public async Task SafeShutdown()
    {
        Disconnect();
        MainWindow.isTwitchIntegrationOpen = false;
        app.mainWindow.EnableOptions();
        app.twitchIntegrationMain = null;
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (app.twitchIntegrationMain != null)
        {
            try
            {
                await SafeShutdown();
            }
            catch (Exception ex)
            {
            }
        }
    }

    public void Connect()
    {
        hasJoinedChannel = false;
        try
        {
            client = new TcpClient("irc.chat.twitch.tv", 6667);
            NetworkStream stream = client.GetStream();

            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);

            //writer.WriteLine("CAP REQ :twitch.tv/tags");
            writer.WriteLine($"PASS {token}");
            writer.WriteLine($"NICK {chatbotusername}");
            writer.WriteLine($"JOIN #{channel}");
            writer.Flush();

            Thread thread = new Thread(ReadChat);
            thread.IsBackground = true;
            thread.Start();

            Task.Run(async () =>
            {
                await Task.Delay(3000);

                if (!hasJoinedChannel)
                {
                    Dispatcher.Invoke(() =>
                    {
                        rtbMessage($"ERROR: Failed to join #{channel}. Channel may not exist.\n");
                    });

                    Disconnect();
                }
            });
        }
        catch (Exception ex)
        {
            rtbMessage($"Connection failed: {ex.Message}\n");
        }
    }

    private void Disconnect()
    {
        try
        {
            reader?.Close();
            writer?.Close();
            client?.Close();
        }
        catch { }

        reader = null;
        writer = null;
        client = null;

        hasJoinedChannel = false;

        Dispatcher.Invoke(() =>
        {
            rtbMessage("Disconnected from Twitch.\n");
            buttonConnect.Content = "Connect";
            buttonConnect.IsEnabled = true;
        });
    }

    private void ReadChat()
    {
        try
        {
            while (client != null && client.Connected)
            {
                string? rawMessage = reader.ReadLine();
                //rtbMessage($"{rawMessage}\n");

                if (rawMessage == null)
                {
                    rtbMessage("Disconnected from Twitch.\n");
                    break;
                }

                if (rawMessage.Contains("Login authentication failed"))
                {
                    rtbMessage("ERROR: Invalid OAuth token.\n");
                    return;
                }

                if (rawMessage.Contains("End of /NAMES list"))
                {
                    hasJoinedChannel = true;
                    Dispatcher.Invoke(() =>
                    {
                        buttonConnect.Content = "Disconnect";
                        buttonConnect.IsEnabled = true;
                    });
                    rtbMessage($"Successfully joined #{channel}!\n");
                }

                if (rawMessage.StartsWith("PING"))
                {
                    writer.WriteLine("PONG :tmi.twitch.tv");
                    writer.Flush();
                    rtbMessage("Sent PONG reply\n");
                    continue;
                }
                if (rawMessage.Contains(" PRIVMSG #"))
                {
                    var (user, chatMessage) = ExtractUserAndMessage(rawMessage);

                    if (user != null && chatMessage != null)
                    {
                        rtbMessage($"{user}: {chatMessage}\n");

                        AddUser(user);

                        ProcessCommand(user, chatMessage);

                    }
                }
            }
        }
        catch (IOException)
        {
            // Expected when disconnecting
            rtbMessage("Connection closed.\n");
        }
        catch (Exception ex)
        {
            // Unexpected errors
            rtbMessage($"ReadChat error: {ex.Message}\n");
        }

    }

    private (string? user, string? message) ExtractUserAndMessage(string rawMessage)
    {
        int exclamationIndex = rawMessage.IndexOf('!');
        int secondColonIndex = rawMessage.IndexOf(':', 1);

        if (exclamationIndex == -1 || secondColonIndex == -1)
            return (null, null);

        string user = rawMessage.Substring(1, exclamationIndex - 1);
        string message = rawMessage.Substring(secondColonIndex + 1).Trim();

        return (user, message);
    }

    private void AddUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        if (!activeUsers.ContainsKey(username))
        {
            var newUser = new ChatUser(username) { Points = 100 };
            activeUsers[username] = newUser;

            Dispatcher.Invoke(() =>
            {
                observableUsers.Add(newUser);
            });
        }
    }

    private void ProcessCommand(string username, string chatMessage)
    {
        if(!CanProcessCommand())
            return;

        reply = null;

        if (chatMessage.Trim().Equals("!points", StringComparison.OrdinalIgnoreCase))
        {
            if (activeUsers.TryGetValue(username, out ChatUser user))
            {
                reply = $"@{username} your current points are {user.Points}.";
            }
        }
        else if (chatMessage.TrimStart().StartsWith("!guide", StringComparison.OrdinalIgnoreCase))
        {
            reply = $"@{username} https://tinyurl.com/37hj83te";
        }
        else if (isGameRunning)
        {
            if (chatMessage.TrimStart().StartsWith("!teleport ", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsRoomBanned(username))
                {
                    Teleport(username, chatMessage);
                }
            }
            else if (chatMessage.TrimStart().StartsWith("!reset ", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsRoomBanned(username))
                {
                    Reset(username, chatMessage);
                }
            }
            else if (chatMessage.TrimStart().StartsWith("!release ", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsRoomBanned(username))
                {
                    Release(username, chatMessage);
                }
            }
        }
        else
        {
            return;
        }


        /*
        if (chatMessage.TrimStart().StartsWith("!cf ", StringComparison.OrdinalIgnoreCase))
        {
            CheckFlags();
        }
        */

        if (reply != null)
        {
            SendTwitchMessage(reply);
        }
        
    }

    private void CheckFlags()
    {
        if (BotData.PuzzleFlags == null || BotData.PuzzleFlags.Count == 0)
        {
            rtbMessage("No puzzle flags defined.\n");
            return;
        }

        rtbMessage("=== Puzzle Flags Status ===\n");

        foreach (var kvp in BotData.PuzzleFlags)
        {
            string puzzleName = kvp.Key;
            var flagBit = kvp.Value.Flag;
            int cost = kvp.Value.Cost;

            // Check if the flag is set
            bool isSet = AppHelpers.IsKthBitSet(app.ReadMemory(flagBit.Offset,1), flagBit.Bit);

            string status = isSet ? "SOLVED" : "UNSOLVED";

            // Include the Offset and Bit in the output
            rtbMessage($"{puzzleName.PadRight(20)} : {status} (Cost: {cost}, Offset: {flagBit.Offset}, Bit: {flagBit.Bit})\n");
        }

        rtbMessage("===========================\n");
    }

    private void Teleport(string username, string chatMessage)
    {
        if (chatMessage.Length <= 10)
            return;

        string destination = chatMessage.Substring(10).Trim().ToLower();

        if (activeUsers.TryGetValue(username, out ChatUser user))
        {
            int locationInt = -9999;
            string locationText = "";
            int cost = 100;

            if (destination == "random")
            {
                var allLocs = BotData.TeleportLocations.Values.ToList();
                var randomLoc = allLocs[rng.Next(allLocs.Count)];

                locationInt = randomLoc.LocationId;
                locationText = "a random location";
                cost = 40;
            }
            else if (BotData.TeleportLocations.TryGetValue(destination, out var loc))
            {
                locationInt = loc.LocationId;
                locationText = loc.DisplayName;
                cost = loc.Cost;
            }
            else
            {
                return; // Destination not found in dictionary
            }
            if (user.Points >= cost)
            {
                user.Points -= cost;
                //reply = $"@{username} redeemed teleport to {locationText}! [Remaining: {user.Points} pts]\n";
                app.StopAudio(locationInt);
            }
        }
    }

    private void Reset(string username, string chatMessage)
    {
        if (chatMessage.Length <= 7)
            return;

        string puzzleName = chatMessage.Substring(7).Trim().ToLower();

        if (!BotData.PuzzleFlags.TryGetValue(puzzleName, out var puzzleData))
            return;


        if (activeUsers.TryGetValue(username, out ChatUser user))
        {
            if (AppHelpers.IsKthBitSet(app.ReadMemory(puzzleData.Flag.Offset,1), puzzleData.Flag.Bit))
            {
                int cost = puzzleData.Cost;
                if (user.Points >= cost)
                {
                    user.Points -= cost;
                    app.SetKthBitMemoryOneByte(puzzleData.Flag.Offset, puzzleData.Flag.Bit, false);

                    if (puzzleName == "workshop drawers")
                    {
                        app.SetKthBitMemoryOneByte(Flags.WorkshopDrawersOpen.Offset, Flags.WorkshopDrawersOpen.Bit, false);
                    }

                }
            }
            else
            {
                reply = $"@{username} {puzzleName} not solved.";
                return;
            }
        }
    }

    private void Release(string username, string chatMessage)
    {
        if (chatMessage.Length <= 9)
            return;

        string ixupiName = chatMessage.Substring(9).Trim().ToLower();

        if (!BotData.IxupiFlags.TryGetValue(ixupiName, out var ixupi))
            return;

        //Ixupi not captured
        int memoryByte = app.ReadMemory(ixupi.Flag.Offset, 1);
        if (!AppHelpers.IsKthBitSet(memoryByte, ixupi.Flag.Bit))
        {
            reply = $"@{username} {ixupiName} not captured.";
            return;
        }

        if (!activeUsers.TryGetValue(username, out ChatUser user))
            return;

        int cost = ixupi.Cost;
        if (user.Points < cost)
            return;

        //Subtract points
        user.Points -= cost;

        //Get current room
        int currentRoom = app.roomNumber;

        //Move to main menu to allow updating the gui
        app.WriteMemory(Addresses.PLAYER_LOCATION, 922);
        // Wait until game sets previous room number to current
        while (app.ReadMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 2) != 922)
        {
            Thread.Sleep(10);
        }

        //Reset captured flag
        app.SetKthBitMemoryOneByte(ixupi.Flag.Offset, ixupi.Flag.Bit, false);

        //Place pot pieces back into game
        app.TwitchIntegrationPlacePotPieces((int)ixupi.PotBaseID);

        // Pick a random valid location
        int[] ixupiLocations = BotData.IxupiLocations[ixupiName];
        int randomLocation = ixupiLocations[rng.Next(ixupiLocations.Length)];

        // Map ixupiName to IxupiLocationOffsets
        IxupiLocationOffsets locationOffset = ixupiName switch
        {
            "water" => IxupiLocationOffsets.WATER,
            "wax" => IxupiLocationOffsets.WAX,
            "ash" => IxupiLocationOffsets.ASH,
            "oil" => IxupiLocationOffsets.OIL,
            "cloth" => IxupiLocationOffsets.CLOTH,
            "wood" => IxupiLocationOffsets.WOOD,
            "crystal" => IxupiLocationOffsets.CRYSTAL,
            "lightning" => IxupiLocationOffsets.LIGHTNING,
            "sand" => IxupiLocationOffsets.SAND,
            "metal" => IxupiLocationOffsets.METAL,
            _ => throw new Exception("Invalid Ixupi name")
        };

        // Release ixupi
        app.WriteMemory((int)locationOffset, randomLocation);

        // Subtract 1 from Ixupi captured counter
        int numberIxupiCapturedTemp = app.ReadMemory(MemoryMap.Addresses.IXUPI_CAPTURED_NUMBER, 1);
        numberIxupiCapturedTemp = Math.Max(0, numberIxupiCapturedTemp - 1);
        app.WriteMemory(Addresses.IXUPI_CAPTURED_NUMBER, numberIxupiCapturedTemp);

        //Redraw inventory bar by setting previous room to main menu
        app.WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 910);

        // Wait until game sets previous room number to current
        while (app.ReadMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 2) != 922)
        {
            Thread.Sleep(10);
        }

        //Move player back to where they were
        app.StopAudio(currentRoom);
    }

    private bool IsRoomBanned(string username)
    {
        int roomNumber = app.roomNumber;

        if (roomNumber >= 3120 && roomNumber <= 3320 || roomNumber == 12600 || roomNumber == 3500 || roomNumber == 3510)
        {
            reply = $"@{username} cannot redeem points while on the boat.";
            return true;
        }
        else if (BotData.BannedRooms.Contains(roomNumber))
        {
            reply = $"@{username} cannot redeem points during a puzzle.";
            return true;
        }
        else if (roomNumber < 1000)
        {
            reply = $"@{username} cannot redeem points on non museum screens.";
            return true;
        }
        
        return false;
    }

    private void StartGame()
    {
        // Initialize timer if it doesn't exist
        if (pointsTimer == null)
        {
            pointsTimer = new DispatcherTimer();
            pointsTimer.Interval = timerInterval;
            pointsTimer.Tick += PointsTimer_Tick;
        }

        // Start the timer
        pointsTimer.Start();

        rtbMessage("Game started!\n");

        reply = "Game started!";
        SendTwitchMessage(reply);

        isGameRunning = true;
    }

    private void ResetGame()
    {
        if (pointsTimer != null && pointsTimer.IsEnabled)
        {
            // Reset all users' points
            foreach (var user in observableUsers)
            {
                user.Points = 100;
            }

            pointsTimer.Stop();
            rtbMessage("Game Reset.\n");

            reply = "Game Reset.";
            SendTwitchMessage(reply);

            isGameRunning = false;
        }
    }

    private void PointsTimer_Tick(object sender, EventArgs e)
    {
        foreach (var user in observableUsers)
        {
            user.Points += pointsPerMinute;
        }

        rtbMessage($"All players have been awarded {pointsPerMinute} points.\n");
    }



    private void buttonConnect_Click(object sender, RoutedEventArgs e)
    {
        buttonConnect.IsEnabled = false;

        if (!IsConnected)
        {
            // Attempt connection
            if (string.IsNullOrWhiteSpace(txtBoxToken.Text))
            {
                rtbMessage("Please enter a valid token.\n");
                buttonConnect.IsEnabled = true;
                return;
            }
            token = "oauth:" + txtBoxToken.Text.Trim();

            chatbotusername = "Anythingworks";
            channel = txtBoxTwitchChannel.Text.Trim();

            if (string.IsNullOrWhiteSpace(channel))
            {
                rtbMessage("Please enter a channel name.\n");
                buttonConnect.IsEnabled = true;
                return;
            }

            Connect();
        }
        else
        {
            Disconnect();
        }
    }

    private void SendTwitchMessage(string message)
    {
        if (writer != null)
        {
            writer.WriteLine($"PRIVMSG #{channel} :{message}");
            writer.Flush();

            rtbMessage($"BOT SENT: {message}\n");
        }
    }
    private void rtbMessage(string message)
    {
        Dispatcher.Invoke(() =>
        {
            rtbReceivedMessages.AppendText(message);
            rtbReceivedMessages.ScrollToEnd();
        });
    }

    private DateTime lastCommandTime = DateTime.MinValue;
    private readonly int globalCooldownSeconds = 1;
    private bool CanProcessCommand()
    {
        if ((DateTime.UtcNow - lastCommandTime).TotalSeconds >= globalCooldownSeconds)
        {
            lastCommandTime = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    private void buttonStartGame_Click(object sender, RoutedEventArgs e)
    {
        if(!isGameRunning)
        {
            StartGame();
        }
        else
        {
            rtbMessage("Game is already running.\n");
        }
    }

    private void buttonResetGame_Click(object sender, RoutedEventArgs e)
    {
        if (isGameRunning)
        {
            ResetGame();
        }
        else
        {
            rtbMessage("No game is currently running.\n");
        }

    }

    private void buttonSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new TwitchIntegrationSettings(this);
        settingsWindow.Owner = this;
        settingsWindow.Show();
    }
}
﻿using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;
using Shivers_Randomizer.enums;
using Shivers_Randomizer.Properties;
using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Shivers_Randomizer.utils.AppHelpers;
using static Shivers_Randomizer.utils.Constants;
using MessagePartColor = Archipelago.MultiClient.Net.Models.Color;

namespace Shivers_Randomizer;

public partial class Archipelago_Client : Window
{
    ArchipelagoSession? session;

    string? serverUrl;
    string? userName;
    string? password;

    LoginResult? cachedConnectionResult;

    private readonly App app;

    public bool IsConnected => (session?.Socket.Connected ?? false) && (cachedConnectionResult?.Successful ?? false) && finishedConnecting;

    public Dictionary<string, string> storagePlacementsDict = new();
    public bool slotDataSettingElevators;
    public bool slotDataSettingEarlyBeth;
    public bool slotDataEarlyLightning;
    public bool slotDataFrontDoorUsable;
    public CollectBehavior slotDataCollectBehavior;
    public int slotDataIxupiCapturesNeeded = 10;
    public ArchipelagoDataStorage? dataStorage;
    private bool userHasScrolledUp;
    private bool userManuallyReconnected;
    private bool userManuallyDisconnected;
    private bool finishedConnecting;
    private int reconnectionAttempts = 0;
    private const int MAX_RECONNECTION_ATTEMPTS = 3;
    private const int SECONDS_PER_ATTEMPT = 5;
    private const int MAX_MESSAGES = 1000;
    private readonly Guid clientGuid = Guid.NewGuid();
    private readonly Queue<LogMessage> pendingMessages = new();
    private readonly DispatcherTimer messageTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(2)
    };

    private readonly DispatcherTimer reconnectionTimer = new()
    {
        Interval = TimeSpan.FromSeconds(SECONDS_PER_ATTEMPT)
    };

    public Archipelago_Client(App app)
    {
        InitializeComponent();
        this.app = app;
        messageTimer.Tick += MessageTimer_Tick;
        reconnectionTimer.Tick += ReconnectionTimer_Tick;
        app.mainWindow.DisableOptions();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        messageTimer.Stop();
        Disconnect();
        serverUrl = null;
        userName = null;
        password = null;
        session = null;
        MainWindow.isArchipelagoClientOpen = false;
        app.archipelago_Client = null;
        app.mainWindow.EnableOptions();
    }

    public LoginResult Connect(string server, string user, string pass, bool reconnect = false)
    {
        if (IsConnected && cachedConnectionResult != null)
        {
            if (serverUrl == server && userName == user && password == pass)
            {
                return cachedConnectionResult;
            }

            Disconnect();
        }

        if (!reconnect)
        {
            serverUrl = server;
        }

        userName = user;
        password = pass;

        try
        {
            if (session == null || !reconnect)
            {
                session = ArchipelagoSessionFactory.CreateSession(serverUrl);
                session.MessageLog.OnMessageReceived += OnMessageReceived;
                session.Socket.ErrorReceived += Socket_ErrorReceived;
            }

            cachedConnectionResult = session.TryConnectAndLogin("Shivers", userName, ItemsHandlingFlags.AllItems, password: password, uuid: clientGuid.ToString());

            if (session.Socket.Connected)
            {
                if (session.RoomState.GeneratorVersion >= new Version(0, 5, 1))
                {
                    // Grab Pot placement data
                    var jsonObject = ((LoginSuccessful)cachedConnectionResult).SlotData;
                    JToken storagePlacements = (JToken)jsonObject["StoragePlacements"];

                    storagePlacementsDict = storagePlacements?.Cast<JProperty>()?.ToDictionary(
                        token => token.Name.Replace("Accessible: Storage: ", ""),
                        token => token.Value.ToString().Replace(" DUPE", "")
                    ) ?? new();

                    // Grab elevator option
                    TryGetBoolSetting(jsonObject, "ElevatorsStaySolved", out slotDataSettingElevators);

                    // Grab early beth option
                    TryGetBoolSetting(jsonObject, "EarlyBeth", out slotDataSettingEarlyBeth);

                    // Grab early lightning option
                    TryGetBoolSetting(jsonObject, "EarlyLightning", out slotDataEarlyLightning);

                    // Grab front door option
                    TryGetBoolSetting(jsonObject, "FrontDoorUsable", out slotDataFrontDoorUsable);

                    // Grab collect option
                    slotDataCollectBehavior = (CollectBehavior)TryGetIntSetting(jsonObject, "PuzzleCollectBehavior", (int)CollectBehavior.PREVENT_OUT_OF_LOGIC_ACCESS);

                    // Grab goal ixupi capture option
                    slotDataIxupiCapturesNeeded = TryGetIntSetting(jsonObject, "IxupiCapturesNeeded", 10);
                    finishedConnecting = true;
                }
                else
                {
                    using (new CursorBusy())
                    {
                        var message = new Message(
                            "This client version can only be used for games generated with Archipelago >=0.5.1."
                        );

                        message.Closed += (s, e) =>
                        {
                            Disconnect();
                        };
                        message.ShowDialog();
                    }
                }
            }
            else if (cachedConnectionResult is LoginFailure failure)
            {
                string messageToPrint = $"Failed to connect to the multiworld server.{Environment.NewLine}";
                failure.Errors.ToList().ForEach(error =>
                {
                    messageToPrint += $"Connection Error: {error}{Environment.NewLine}";
                });

                messageToPrint += Environment.NewLine;

                ServerMessageBox.Dispatcher.Invoke(() =>
                {
                    ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                    ScrollMessages();
                });
            }
        }
        catch (Exception e)
        {
            cachedConnectionResult = new LoginFailure(e.GetBaseException().Message);
            var messageToPrint = $"Error: {e.Message}{Environment.NewLine}";
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                ScrollMessages();
            });
        }

        return cachedConnectionResult;
    }

    public static bool TryGetBoolSetting(Dictionary<string, object> jsonObject, string key, out bool result)
    {
        result = false;

        if (jsonObject.ContainsKey(key))
        {
            result = Convert.ToBoolean(jsonObject[key]);
            return true;
        }

        new Message(
            $"Could not find the setting for '{key}' from the server." +
            "\nThe option will be turned off."
        ).ShowDialog();

        return false;
    }

    public static int TryGetIntSetting(Dictionary<string, object> jsonObject, string key, int defaultValue)
    {
        if (jsonObject.ContainsKey(key))
        {
            return Convert.ToInt32(jsonObject[key]);
        }

        new Message(
            $"Could not find the setting for '{key}' from the server." +
            $"\nThe default value ({defaultValue}) will be used."
        ).ShowDialog();

        return defaultValue;
    }

    private void Socket_ErrorReceived(Exception e, string message)
    {
        var messageToPrint = $"{Environment.NewLine}Socket Error: ";
        if (e is AggregateException)
        {
            var innerException = e.InnerException;
            messageToPrint += innerException != null ? $"{innerException.Message}{Environment.NewLine}" : $"{message}{Environment.NewLine}";
            while (innerException?.InnerException != null)
            {
                innerException = innerException.InnerException;
                messageToPrint += $"    {innerException.Message}{Environment.NewLine}";
            }
        }
        else
        {
            messageToPrint += $"{message}{Environment.NewLine}";
            messageToPrint += e.Source != null ? $"{e.Source}{Environment.NewLine}" : "";
            foreach (var line in e.StackTrace?.Split('\n') ?? Array.Empty<string>())
            {
                messageToPrint += $"    {line}";
            }
        }

        messageToPrint += Environment.NewLine + Environment.NewLine;

        ServerMessageBox.Dispatcher.Invoke(() =>
        {
            ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
            ScrollMessages();
        });

        if (cachedConnectionResult is LoginSuccessful && !userManuallyDisconnected && !reconnectionTimer.IsEnabled)
        {
            Dispatcher.Invoke(() =>
            {
                Disconnect();
            });
            userManuallyReconnected = false;
            reconnectionAttempts = 1;
            reconnectionTimer.Interval = TimeSpan.FromSeconds(SECONDS_PER_ATTEMPT);
            reconnectionTimer.Start();
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                string messageToPrint = $"Lost connection to the multiworld server.{Environment.NewLine}";
                messageToPrint += $"...automatically reconnecting in {SECONDS_PER_ATTEMPT} seconds.{Environment.NewLine}{Environment.NewLine}";
                ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                ScrollMessages();
            });
        }
    }

    private void ReconnectionTimer_Tick(object? sender, EventArgs e)
    {
        reconnectionTimer.Stop();
        if (!IsConnected && !userManuallyReconnected)
        {
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                string messageToPrint = $"...attempting to reconnect.{Environment.NewLine}{Environment.NewLine}";
                ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                ScrollMessages();
            });
            AttemptConnection();

            if (!IsConnected)
            {
                if (reconnectionAttempts < MAX_RECONNECTION_ATTEMPTS)
                {
                    reconnectionAttempts++;
                    int secondsToSleep = SECONDS_PER_ATTEMPT * reconnectionAttempts;
                    reconnectionTimer.Interval = TimeSpan.FromSeconds(secondsToSleep);
                    reconnectionTimer.Start();

                    ServerMessageBox.Dispatcher.Invoke(() =>
                    {
                        string messageToPrint = $"...automatically reconnecting in {secondsToSleep} seconds.{Environment.NewLine}{Environment.NewLine}";
                        ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                        ScrollMessages();
                    });
                }
                else
                {
                    reconnectionAttempts = 0;
                    ServerMessageBox.Dispatcher.Invoke(() =>
                    {
                        string messageToPrint = $"Max reconnection attempts reached.{Environment.NewLine}";
                        messageToPrint += $"Try refresing the room, check connection settings, or check internet.{Environment.NewLine}";
                        ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                        ScrollMessages();
                    });
                }
            }
            else
            {
                reconnectionAttempts = 0;
            }
        }
        else
        {
            reconnectionAttempts = 0;
        }
    }

    public async void Disconnect()
    {
        if (session != null)
        {
            if (IsConnected)
            {
                await session.Socket.DisconnectAsync();
            }

            cachedConnectionResult = null;
            buttonConnect.Content = "Connect";
            buttonConnect.IsDefault = true;
            finishedConnecting = false;

            app.StopArchipelago();
        }
    }

    private void OnMessageReceived(LogMessage message)
    {
        pendingMessages.Enqueue(message);
        if (!messageTimer.IsEnabled)
        {
            messageTimer.Start();
        }
    }

    private void MessageTimer_Tick(object? sender, EventArgs e)
    {
        if (pendingMessages.TryDequeue(out var message))
        {
            var messageParts = message.Parts.Select(p =>
            {
                var part = new Part(p.Text, p.Color);
                ModifyColors(part);
                return part;
            }).ToList();
                
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                var document = ServerMessageBox.Document;
                while (document.Blocks.Count > MAX_MESSAGES)
                {
                    document.Blocks.Remove(document.Blocks.FirstBlock);
                }

                messageParts.ForEach(part =>
                {
                    System.Windows.Media.Color color = FromDrawingColor(part.Color);
                    ServerMessageBox.AppendTextWithColor(part.Text, new SolidColorBrush(color));
                });
                ServerMessageBox.AppendTextWithColor(Environment.NewLine, Brushes.Transparent);
                ScrollMessages();
            });
        } else {
            messageTimer.Stop();
        }
    }

    private static System.Windows.Media.Color FromDrawingColor(MessagePartColor drawingColor) =>
        System.Windows.Media.Color.FromArgb(255, drawingColor.R, drawingColor.G, drawingColor.B);

    private static void ModifyColors(Part part)
    {
        if (part.Color == MessagePartColor.Magenta)
        {
            part.Color.R = 238;
            part.Color.B = 238;
        }
        else if (part.Color == MessagePartColor.Green)
        {
            part.Color.R = 0;
            part.Color.G = 254;
            part.Color.B = 127;
        }
        else if (part.Color == MessagePartColor.Yellow)
        {
            part.Color.R = 246;
            part.Color.G = 246;
            part.Color.B = 207;
        }
        else if (part.Color == MessagePartColor.Plum)
        {
            part.Color.R = 175;
            part.Color.G = 153;
            part.Color.B = 239;
        }
        else if(part.Color == MessagePartColor.SlateBlue)
        {
            part.Color.R = 109;
            part.Color.G = 139;
            part.Color.B = 232;
        }
    }

    internal class Part
    {
        public string Text { get; set; }
        public MessagePartColor Color;

        public Part(string text, MessagePartColor color)
        {
            Text = text;
            Color = color;
        }
    }

    private void ButtonConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!IsConnected)
        {
            AttemptConnection(reconnectionAttempts > 0);
        }
        else
        {
            userManuallyDisconnected = true;
            Disconnect();
        }
    }

    private void AttemptConnection(bool manualReconnect = false)
    {
        using (new CursorBusy())
        {
            if (serverUrl == null || serverUrl == "localhost" && serverIP.Text != "localhost" ||
                serverUrl.Split(":").LastOrDefault() != serverIP.Text.Split(":").LastOrDefault())
            {
                Connect(serverIP.Text, slotName.Text, serverPassword.Text);
            }
            else
            {
                Connect(serverUrl, slotName.Text, serverPassword.Text, manualReconnect);
            }

            if (IsConnected)
            {
                reconnectionAttempts = 0;
                buttonConnect.Content = "Disconnect";
                buttonConnect.IsDefault = false;
                userManuallyDisconnected = false;
                Keyboard.ClearFocus();
                Settings.Default.serverIp = serverIP.Text;
                Settings.Default.slotName = slotName.Text;

                if (manualReconnect)
                {
                    userManuallyReconnected = true;
                    reconnectionTimer.Stop();
                }
            }
        }
    }

    public void SendCheck(int checkID)
    {
        session?.Locations.CompleteLocationChecks(checkID);
    }

    public List<int> GetItemsFromArchipelagoServer()
    {
        ReadOnlyCollection<ItemInfo> networkItems = session?.Items.AllItemsReceived ?? new ReadOnlyCollection<ItemInfo>(new List<ItemInfo>());
        return (from ItemInfo item in networkItems select (int)item.ItemId).ToList();
    }

    public void Send_completion() => session?.SetGoalAchieved();

    public void SetStatus(ArchipelagoClientState state) => session?.SetClientState(state);

    public void Commands(string command)
    {
        if (!string.IsNullOrEmpty(command))
        {
            session?.Say(command);
        }
    }

    public List<long>? GetLocationsCheckedArchipelagoServer()
    {
        return session?.Locations.AllLocationsChecked.ToList();
    }

    public void SaveData(int numItemsReceived)
    {
        if (session != null)
        {
            session.DataStorage [Scope.Slot, "SaveState"] = JToken.FromObject(dataStorage);
            session.DataStorage[Scope.Slot, "NumItemsReceived"] = numItemsReceived;
        }
    }

    public async Task LoadData()
    {
        if (session != null)
        {
            dataStorage = await session.DataStorage[Scope.Slot, "SaveState"].GetAsync<ArchipelagoDataStorage>();
        }
    }

    public void InitializeDataStorage(int skullDialPrehistoric, int skullDialTarRiver, int skullDialWerewolf, int skullDialBurial, int skullDialEgypt, int skullDialGods)
    {
        // Initialize Data storage
        Dictionary<string, AddressedValue> skullDials = new()
        {
            { "Prehistoric", new(836, skullDialPrehistoric) },
            { "TarRiver", new(840, skullDialTarRiver) },
            { "Werewolf", new(844, skullDialWerewolf) },
            { "Burial", new(848, skullDialBurial) },
            { "Egypt", new(852, skullDialEgypt) },
            { "Gods", new(856, skullDialGods) }
        };

        Dictionary<string, AddressedValue> ixupiDamage = new()
        {
            { "Water", new(184, 0) },
            { "Wax", new(192, 0) },
            { "Ash", new(200, 0) },
            { "Oil", new(208, 0) },
            { "Cloth" ,new(216, 0) },
            { "Wood", new(224, 0) },
            { "Crystal", new(232, 0) },
            { "Lightning", new(240, 0) },
            { "Sand", new(248, 0) },
            { "Metal", new(256, 0) },
        };

        ArchipelagoDataStorage saveState = new(new(), skullDials, ixupiDamage);

        session?.DataStorage[Scope.Slot, "SaveState"].Initialize(JToken.FromObject(saveState));
        session?.DataStorage[Scope.Slot, "NumItemsReceived"].Initialize(0);
    }

    public void ArchipelagoUpdateWindow(int roomNumber, List<int> items)
    {
        // Update storage
        bool connected = IsConnected && roomNumber != 910 && roomNumber != 922;
        LabelStorageDeskDrawer.Content = connected ? ConvertPotNumberToString(app.ReadMemory(0, 1)) : "";
        LabelStorageWorkshopDrawers.Content = connected ? ConvertPotNumberToString(app.ReadMemory(8, 1)) : "";
        LabelStorageLibraryCabinet.Content = connected ? ConvertPotNumberToString(app.ReadMemory(16, 1)) : "";
        LabelStorageLibraryStatue.Content = connected ? ConvertPotNumberToString(app.ReadMemory(24, 1)) : "";
        LabelStorageSlide.Content = connected ? ConvertPotNumberToString(app.ReadMemory(32, 1)) : "";
        LabelStorageTransformingMask.Content = connected ? ConvertPotNumberToString(app.ReadMemory(40, 1)) : "";
        LabelStorageEaglesNest.Content = connected ? ConvertPotNumberToString(app.ReadMemory(48, 1)) : "";
        LabelStorageOcean.Content = connected ? ConvertPotNumberToString(app.ReadMemory(56, 1)) : "";
        LabelStorageTarRiver.Content = connected ? ConvertPotNumberToString(app.ReadMemory(64, 1)) : "";
        LabelStorageTheater.Content = connected ? ConvertPotNumberToString(app.ReadMemory(72, 1)) : "";
        LabelStorageGreenhouse.Content = connected ? ConvertPotNumberToString(app.ReadMemory(80, 1)) : "";
        LabelStorageEgypt.Content = connected ? ConvertPotNumberToString(app.ReadMemory(88, 1)) : "";
        LabelStorageChineseSolitaire.Content = connected ? ConvertPotNumberToString(app.ReadMemory(96, 1)) : "";
        LabelStorageShamanHut.Content = connected ? ConvertPotNumberToString(app.ReadMemory(104, 1)) : "";
        LabelStorageLyre.Content = connected ? ConvertPotNumberToString(app.ReadMemory(112, 1)) : "";
        LabelStorageSkeleton.Content = connected ? ConvertPotNumberToString(app.ReadMemory(120, 1)) : "";
        LabelStorageAnansi.Content = connected ? ConvertPotNumberToString(app.ReadMemory(128, 1)) : "";
        LabelStorageJanitorCloset.Content = connected ? ConvertPotNumberToString(app.ReadMemory(136, 1)) : "";
        LabelStorageUFO.Content = connected ? ConvertPotNumberToString(app.ReadMemory(144, 1)) : "";
        LabelStorageAlchemy.Content = connected ? ConvertPotNumberToString(app.ReadMemory(152, 1)) : "";
        LabelStorageSkullBridge.Content = connected ? ConvertPotNumberToString(app.ReadMemory(160, 1)) : "";
        LabelStorageGallows.Content = connected ? ConvertPotNumberToString(app.ReadMemory(168, 1)) : "";
        LabelStorageClockTower.Content = connected ? ConvertPotNumberToString(app.ReadMemory(176, 1)) : "";
        
        // Update keys
        LabelKeyOfficeElevator.IsEnabled = items.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR );
        LabelKeyBedroomElevator.IsEnabled = items.Contains((int)APItemID.KEYS.BEDROOM_ELEVATOR);
        LabelKeyThreeFloorElevator.IsEnabled = items.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR);
        LabelKeyWorkshop.IsEnabled = items.Contains((int)APItemID.KEYS.WORKSHOP);
        LabelKeyOffice.IsEnabled = items.Contains((int)APItemID.KEYS.OFFICE);
        LabelKeyPrehistoric.IsEnabled = items.Contains((int)APItemID.KEYS.PREHISTORIC);
        LabelKeyGreenhouse.IsEnabled = items.Contains((int)APItemID.KEYS.GREENHOUSE);
        LabelKeyOcean.IsEnabled = items.Contains((int)APItemID.KEYS.OCEAN);
        LabelKeyProjector.IsEnabled = items.Contains((int)APItemID.KEYS.PROJECTOR);
        LabelKeyGenerator.IsEnabled = items.Contains((int)APItemID.KEYS.GENERATOR);
        LabelKeyEgypt.IsEnabled = items.Contains((int)APItemID.KEYS.EGYPT);
        LabelKeyLibrary.IsEnabled = items.Contains((int)APItemID.KEYS.LIBRARY);
        LabelKeyShaman.IsEnabled = items.Contains((int)APItemID.KEYS.SHAMAN);
        LabelKeyUFO.IsEnabled = items.Contains((int)APItemID.KEYS.UFO);
        LabelKeyTorture.IsEnabled = items.Contains((int)APItemID.KEYS.TORTURE);
        LabelKeyPuzzle.IsEnabled = items.Contains((int)APItemID.KEYS.PUZZLE);
        LabelKeyBedroom.IsEnabled = items.Contains((int)APItemID.KEYS.BEDROOM);
        LabelKeyUndergroundLake.IsEnabled = items.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM);
        LabelKeyJantiorCloset.IsEnabled = items.Contains((int)APItemID.KEYS.JANITOR_CLOSET);
        LabelKeyFrontDoor.Visibility = slotDataFrontDoorUsable ? Visibility.Visible : Visibility.Hidden;
        LabelKeyFrontDoor.IsEnabled = items.Contains((int)APItemID.KEYS.FRONT_DOOR);
        LabelKeyCrawling.IsEnabled = items.Contains((int)APItemID.ABILITIES.CRAWLING);
        LabelEasierLyre.Visibility = items.Contains((int)APItemID.FILLER.EASIER_LYRE) ? Visibility.Visible : Visibility.Hidden;
        LabelEasierLyre.Content = "Easier Lyre x " + (items?.Count(item => item == ((int)APItemID.FILLER.EASIER_LYRE)) ?? 0);
    }

    public async void ReportNewItemsReceived()
    {
        if (session == null)
        {
            return;
        }

        int numItemsReceived = await session.DataStorage[Scope.Slot, "NumItemsReceived"].GetAsync<int?>() ?? 0;
        List<int> items = GetItemsFromArchipelagoServer();
        Brush plumBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 153, 239));

        if (numItemsReceived < items.Count)
        {
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                ServerMessageBox.AppendTextWithColor($"Since you last connected you have received the following items:{Environment.NewLine}", Brushes.LimeGreen);
                for (int i = numItemsReceived; i < items.Count; i++)
                {
                    string itemName = GetItemName(items[i]) ?? "Error Retrieving Item";
                    string message = $"{itemName} {Environment.NewLine}";

                    if (items[i] < ARCHIPELAGO_BASE_ITEM_ID + 90)
                    {
                        ServerMessageBox.AppendTextWithColor(message, plumBrush);
                    }
                    else
                    {
                        ServerMessageBox.AppendTextWithColor(message, Brushes.Cyan);
                    }
                }

                ScrollMessages();
            });
            session.DataStorage[Scope.Slot, "NumItemsReceived"] = items.Count;
        }
    }

    public void MoveToRegistry()
    {
        ServerMessageBox.Dispatcher.Invoke(() =>
        {
            ServerMessageBox.AppendTextWithColor($"Please press New Game in Shivers.{Environment.NewLine}", Brushes.Red);
            ScrollMessages();
        });
    }

    private void ServerMessageBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        userHasScrolledUp = e.VerticalOffset < e.ExtentHeight - e.ViewportHeight;
    }

    private void ButtonCommands_Click(object sender, RoutedEventArgs e)
    {
        string CommandMessage = commandBox.Text;
        commandBox.Text = "";
        Commands(CommandMessage);
    }
    private void CommandBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string CommandMessage = commandBox.Text;
            commandBox.Text = "";
            Commands(CommandMessage);
        }
    }

    private string GetItemName(int itemID)
    {
        return session?.Items.GetItemName(itemID) ?? "Error retrieving item";
    }

    private void ScrollMessages()
    {
        if (!userHasScrolledUp)
        {
            ServerMessageBox.ScrollToEnd();
        }
    }
}
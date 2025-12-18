using Shivers_Randomizer.enums;
using Shivers_Randomizer.Properties;
using Shivers_Randomizer.room_randomizer;
using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static Shivers_Randomizer.utils.AppHelpers;
using static Shivers_Randomizer.utils.Constants;
using static Shivers_Randomizer.utils.MemoryMap;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public MainWindow mainWindow;
    public Overlay overlay;
    public LiveSplit? liveSplit = null;
    public Multiplayer_Client? multiplayer_Client = null;
    public Archipelago_Client? archipelago_Client = null;
    public DevMenu? dev_Menu = null;
    private DispatcherTimer appTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(1)
    };

    private RectSpecial ShiversWindowDimensions = new();

    public UIntPtr processHandle;
    public UIntPtr MyAddress;
    public Process? shiversProcess;
    public bool? AddressLocated = null;

    public bool scrambling = false;
    public int Seed;
    public bool setSeedUsed;
    private Random rng;
    private IxupiPot[] Locations = new IxupiPot[POT_LOCATIONS.Count];
    public int roomNumber;
    public int roomNumberPrevious;
    public int numberIxupiCaptured;
    public int health;
    public int healthPrevious;
    public int firstToTheOnlyXNumber;
    public bool finalCutsceneTriggered;
    private bool useFastTimer;
    private bool elevatorOfficeSolved;
    private bool elevatorBedroomSolved;
    private bool elevatorThreeFloorSolved;
    private int elevatorSolveCountPrevious;
    private bool real925ScriptLocated;
    private int multiplayerSyncCounter;
    private bool multiplayerScreenRedrawNeeded;

    public bool settingsVanilla;
    public bool settingsIncludeAsh;
    public bool settingsIncludeLightning;
    public bool settingsEarlyBeth;
    public bool settingsExtraLocations;
    public bool settingsExcludeLyre;
    public bool settingsSolvedLyre;
    public bool settingsEarlyLightning;
    public bool settingsRedDoor;
    public bool settingsFullPots;
    public bool settingsFirstToTheOnlyFive;
    public bool settingsRedHerrings;
    public bool settingsRoomShuffle;
    public bool settingsIncludeElevators;
    public bool settingsMultiplayer;
    public bool settingsOnly4x4Elevators;
    public bool settingsElevatorsStaySolved;
    public bool settingsUnlockEntrance;
    public bool settingsAnywhereLightning;
    public bool settingsFlashTravel;

    public bool currentlyTeleportingPlayer = false;
    public RoomTransition? lastTransitionUsed;

    public bool disableScrambleButton;
    public int[] multiplayerLocations = new int[POT_LOCATIONS.Count];
    public bool[] multiplayerIxupi = new bool[IXUPI.Count];
    public int[] ixupiLocations = new int[IXUPI.Count];

    public bool currentlyRunningThreadOne = false;
    public bool currentlyRunningThreadTwo = false;

    public RoomTransition[] roomTransitions = Array.Empty<RoomTransition>();

    readonly List<Tuple<int, UIntPtr>> scriptsFound = new();
    readonly List<int> completeScriptList = new();
    bool scriptsLocated = false;
    bool scriptAlreadyModified = false;
    public int lastScriptModified = -1;


    private List<int> archipelagoReceivedItems = new();
    private bool archipelagoInitialized;
    private bool archipelagoReportedNewItems;
    private bool archipelagoTimerTick;
    private bool archipelagoRegistryMessageSent;
    private readonly bool[] archipelagoPiecePlaced = new bool[APItemID.AP_POTS_COUNT];
    private bool archipelagoRunningTick;
    private bool archipelagoCheckStoneTablet;
    private bool archipelagoCheckBasilisk;
    private bool archipelagoCheckSirenSong;
    private bool archipelagoCheckEgyptianSphinx;
    private bool archipelagoCheckGallowsPlaque;
    private bool archipelagoCheckGeoffreyWriting;
    private bool archipelagoCheckPlaqueUFO;
    private bool archipelagoGeneratorSwitchOn;
    private bool archipelagoGeneratorSwitchScreenRefreshed;
    private int archipelagoHealCountPrevious;
    List<int> archipelagoCompleteScriptList = new();
    private bool archipelagoCurrentlyLoadingData;
    private bool archipelagoElevatorSettings;
    private CollectBehavior archipelagoCollectBehavior = CollectBehavior.PREVENT_OUT_OF_LOGIC_ACCESS;
    readonly List<int> archipelagoChecksReadyToSend = new();
    public bool archipelagoReleaseDisabled = false;

    public App()
    {
        mainWindow = new(this);
        overlay = new(this);
        rng = new();
        mainWindow.Show();
        appTimer.Tick += Timer_Tick;
        var cursorStream = new MemoryStream(Shivers_Randomizer.Properties.Resources.ShiversCursor);
        Mouse.OverrideCursor = new Cursor(cursorStream);
        AppHelpers.CheckUpgrade();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    }

    public async Task SafeShutdown()
    {
        await (archipelago_Client?.SafeShutdown() ?? Task.CompletedTask);
        liveSplit?.Disconnect();
        liveSplit?.Close();
        liveSplit = null;
        multiplayer_Client?.Close();
        multiplayer_Client = null;
        appTimer.Stop();
        Settings.Default.Save();
        Shutdown();
    }

    public void StopArchipelago()
    {
        stopArchipelagoTimerEvent?.Set();
        archipelagoTimerThread?.Join();
        stopScriptModificationTimerEvent?.Set();
        scriptModificationTimerThread?.Join();
        stopArchipelagoTimerEvent?.Dispose();
        stopScriptModificationTimerEvent?.Dispose();
        stopArchipelagoTimerEvent = null;
        archipelagoTimerThread = null;
        stopScriptModificationTimerEvent = null;
        scriptModificationTimerThread = null;

        // Reset initialization info
        // If player was on the boat move to pause menu screen and then to main menu, else move straight to main menu. This clears the boat state flag
        if (roomNumber >= 3120 && roomNumber <= 3320)
        {
            WriteMemory(Addresses.PLAYER_LOCATION, 990); // Move to pause menu
            Thread.Sleep(500);
            WriteMemory(Addresses.PLAYER_LOCATION, 910); // Move to main menu
        }
        else
        {
            WriteMemory(Addresses.PLAYER_LOCATION, 910); // Move to main menu
        }
        
        archipelagoHealCountPrevious = 0;
        archipelagoReceivedItems.Clear();
        archipelagoChecksReadyToSend.Clear();
        Array.Fill(archipelagoPiecePlaced, false);

        // Reset flags
        archipelagoCheckStoneTablet = false;
        archipelagoCheckBasilisk = false;
        archipelagoCheckSirenSong = false;
        archipelagoCheckEgyptianSphinx = false;
        archipelagoCheckGallowsPlaque = false;
        archipelagoCheckGeoffreyWriting = false;
        archipelagoCheckPlaqueUFO = false;
        archipelagoElevatorSettings = false;
        archipelagoCollectBehavior = CollectBehavior.PREVENT_OUT_OF_LOGIC_ACCESS;
        archipelagoGeneratorSwitchOn = false;
        archipelagoGeneratorSwitchScreenRefreshed = false;
        archipelagoInitialized = false;
        archipelagoRegistryMessageSent = false;
        archipelagoReportedNewItems = false;
        archipelagoRunningTick = false;
        archipelagoTimerTick = false;
        elevatorOfficeSolved = false;
        elevatorBedroomSolved = false;
        elevatorThreeFloorSolved = false;
        scriptsLocated = false;
        scriptAlreadyModified = false;
    }

    private void CleanUpRandomizer()
    {
        settingsVanilla = false;
        settingsIncludeAsh = false;
        settingsIncludeLightning = false;
        settingsEarlyBeth = false;
        settingsExtraLocations = false;
        settingsExcludeLyre = false;
        settingsSolvedLyre = false;
        settingsEarlyLightning = false;
        settingsRedDoor = false;
        settingsFullPots = false;
        settingsFirstToTheOnlyFive = false;
        settingsRoomShuffle = false;
        settingsIncludeElevators = false;
        settingsMultiplayer = false;
        settingsOnly4x4Elevators = false;
        settingsElevatorsStaySolved = false;
        settingsUnlockEntrance = false;
        settingsAnywhereLightning = false;
        elevatorOfficeSolved = false;
        elevatorBedroomSolved = false;
        elevatorThreeFloorSolved = false;
        elevatorSolveCountPrevious = 0;
        numberIxupiCaptured = 0;
        finalCutsceneTriggered = false;
        lastTransitionUsed = null;
        roomTransitions = Array.Empty<RoomTransition>();
    }

    public void Scramble()
    {

        scrambling = true;
        mainWindow.button_Scramble.IsEnabled = false;

        if (multiplayer_Client != null)
        {
            settingsMultiplayer = multiplayer_Client.multiplayerEnabled;
        }

        // Check if seed was entered
        if (mainWindow.txtBox_Seed.Text != "")
        {
            // check if seed is too big, if not use it
            if (!int.TryParse(mainWindow.txtBox_Seed.Text, out Seed))
            {
                ScrambleFailure("Seed was not less then 2,147,483,647. Please try again with a smaller number.");
                return;
            }
            setSeedUsed = true;
        }
        else
        {
            setSeedUsed = false;
            // if not seed entered, seed to the system clock
            Seed = (int)DateTime.Now.Ticks;

        }

        // If not a set seed, hide the system clock seed number so that it cant be used to cheat (unlikely but what ever)
        Random rngHidden = new(Seed);
        
        if (!setSeedUsed)
        {
            Seed = rngHidden.Next();
        }
        rng = new(Seed);

        // If early lightning then set flags for timer
        finalCutsceneTriggered = false;

        // Reset elevator flags
        elevatorOfficeSolved = false;
        elevatorBedroomSolved = false;
        elevatorThreeFloorSolved = false;

    Scramble:
        Locations = new IxupiPot[POT_LOCATIONS.Count];

        // If Vanilla is selected then use the vanilla placement algorithm
        if (settingsVanilla)
        {
            Locations[(int)PotLocation.DESK_DRAWER] = IxupiPot.ASH_TOP;
            Locations[(int)PotLocation.SLIDE] = IxupiPot.LIGHTNING_TOP;
            Locations[(int)PotLocation.GREENHOUSE] = IxupiPot.ASH_BOTTOM;
            VanillaPlacePiece(IxupiPot.WATER_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.WAX_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.OIL_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.CLOTH_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.WOOD_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.CRYSTAL_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.LIGHTNING_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.SAND_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.METAL_BOTTOM, rng);
            VanillaPlacePiece(IxupiPot.WATER_TOP, rng);
            VanillaPlacePiece(IxupiPot.WAX_TOP, rng);
            VanillaPlacePiece(IxupiPot.OIL_TOP, rng);
            VanillaPlacePiece(IxupiPot.CLOTH_TOP, rng);
            VanillaPlacePiece(IxupiPot.WOOD_TOP, rng);
            VanillaPlacePiece(IxupiPot.CRYSTAL_TOP, rng);
            VanillaPlacePiece(IxupiPot.SAND_TOP, rng);
            VanillaPlacePiece(IxupiPot.METAL_TOP, rng);
        }
        else if (!settingsFirstToTheOnlyFive) // Normal Scramble
        {
            List<IxupiPot> piecesNeededToBePlaced = new();
            List<IxupiPot> piecesRemainingToBePlaced = new();
            int numberOfRemainingPots = 20;
            int numberOfFullPots = 0;

            // Check if ash is added to the scramble
            if (!settingsIncludeAsh)
            {
                Locations[(int)PotLocation.DESK_DRAWER] = IxupiPot.ASH_TOP;
                Locations[(int)PotLocation.GREENHOUSE] = IxupiPot.ASH_BOTTOM;
                numberOfRemainingPots -= 2;
            }
            // Check if lighting is added to the scramble
            if (!settingsIncludeLightning)
            {
                Locations[(int)PotLocation.SLIDE] = IxupiPot.LIGHTNING_TOP;
                numberOfRemainingPots -= 1;
            }

            if (settingsFullPots)
            {
                if (settingsExcludeLyre && !settingsExtraLocations)
                {   // No more then 8 since ash/lightning will be rolled outside of the count
                    numberOfFullPots = rng.Next(1, 9); // Roll how many completed pots. If no lyre and no extra locations you must have at least 1 completed to have room.
                }
                else
                {
                    numberOfFullPots = rng.Next(0, 9); // Roll how many completed pots
                }

                IxupiPot fullPotRolled;
                for (int i = 0; i < numberOfFullPots; i++)
                {
                RollFullPot:
                    fullPotRolled = (IxupiPot)rng.Next(POT_FULL_OFFSET, POT_FULL_OFFSET + 10); // Grab a random pot
                    if (fullPotRolled == IxupiPot.ASH_FULL || fullPotRolled == IxupiPot.LIGHTNING_FULL) // Make sure its not ash or lightning
                    {
                        goto RollFullPot;
                    }

                    if (piecesNeededToBePlaced.Contains(fullPotRolled)) // Make sure it wasn't already selected
                    {
                        goto RollFullPot;
                    }
                    piecesNeededToBePlaced.Add(fullPotRolled);
                    numberOfRemainingPots -= 2;
                }
                if (rng.Next(0, 2) == 1 && settingsIncludeAsh) // Is ash completed
                {
                    piecesNeededToBePlaced.Add(IxupiPot.ASH_FULL);
                    numberOfRemainingPots -= 2;
                }
                if (rng.Next(0, 2) == 1 && settingsIncludeLightning) // Is lighting completed
                {
                    piecesNeededToBePlaced.Add(IxupiPot.LIGHTNING_FULL);
                    numberOfRemainingPots -= 2;
                }
            }

            IxupiPot pieceBeingAddedToList; // Add remaining pieces to list
            while (numberOfRemainingPots != 0)
            {
                pieceBeingAddedToList = (IxupiPot)(rng.Next(0, 20) + POT_BOTTOM_OFFSET);
                // Check if piece already added to list
                // Check if piece was ash and ash not included in scramble
                // Check if piece was lighting top and lightning not included in scramble
                if (piecesNeededToBePlaced.Contains(pieceBeingAddedToList) ||
                    !settingsIncludeAsh && (pieceBeingAddedToList == IxupiPot.ASH_BOTTOM || pieceBeingAddedToList == IxupiPot.ASH_TOP) ||
                    !settingsIncludeLightning && pieceBeingAddedToList == IxupiPot.LIGHTNING_TOP)
                {
                    continue;
                }
                // Check if completed pieces are used and the base pieces are rolled
                if (((int)pieceBeingAddedToList < POT_TOP_OFFSET && piecesNeededToBePlaced.Contains(pieceBeingAddedToList + 20)) ||
                    ((int)pieceBeingAddedToList >= POT_TOP_OFFSET && piecesNeededToBePlaced.Contains(pieceBeingAddedToList + 10)))
                {
                    continue;
                }
                piecesNeededToBePlaced.Add(pieceBeingAddedToList);
                numberOfRemainingPots -= 1;
            }

            PotLocation randomLocation;
            piecesRemainingToBePlaced = new List<IxupiPot>(piecesNeededToBePlaced);
            while (piecesRemainingToBePlaced.Count > 0)
            {
                randomLocation = POT_LOCATIONS[rng.Next(POT_LOCATIONS.Count)];
                if (!settingsExtraLocations && EXTRA_LOCATIONS.Contains(randomLocation)) // Check if extra locations are used
                {
                    continue;
                }
                if (settingsExcludeLyre && randomLocation == PotLocation.LYRE) // Check if lyre excluded
                {
                    continue;
                }
                if (Locations[(int)randomLocation] != 0) // Check if location is filled
                {
                    continue;
                }
                Locations[(int)randomLocation] = piecesRemainingToBePlaced[0];
                piecesRemainingToBePlaced.RemoveAt(0);
            }

            // Check for bad scramble
            // Check if oil behind oil
            // Check if cloth behind cloth
            // Check if oil behind cloth AND cloth behind oil
            if (OIL_POTS.Contains(Locations[(int)PotLocation.TAR_RIVER]) ||
                CLOTH_POTS.Contains(Locations[(int)PotLocation.JANITOR_CLOSET]) ||
                OIL_POTS.Contains(Locations[(int)PotLocation.JANITOR_CLOSET]) && CLOTH_POTS.Contains(Locations[(int)PotLocation.TAR_RIVER]))
            {
                goto Scramble;
            }

            // Check if oil behind slide and something behind oil
            // check if cloth behind slide and something behind cloth
            if (settingsEarlyLightning && !settingsEarlyBeth &&
                (OIL_POTS.Contains(Locations[(int)PotLocation.SLIDE]) && Locations[(int)PotLocation.TAR_RIVER] != 0 ||
                    CLOTH_POTS.Contains(Locations[(int)PotLocation.SLIDE]) && Locations[(int)PotLocation.JANITOR_CLOSET] != 0))
            {
                goto Scramble;
            }
        }
        else if (settingsFirstToTheOnlyFive) // First to the only X
        {
            List<IxupiPot> piecesNeededToBePlaced = new();
            List<IxupiPot> piecesRemainingToBePlaced = new();

            // Get number of sets
            int numberOfRemainingPots = 2 * firstToTheOnlyXNumber;

            // Check for invalid numbers
            if (numberOfRemainingPots == 0) // No Sets
            {
                ScrambleFailure("Number of Ixupi must be greater than 0.");
                return;
            }
            else if (numberOfRemainingPots == 2 && !settingsIncludeAsh && !settingsIncludeLightning)
            {
                ScrambleFailure("If selecting 1 pot set you must include either lighting or ash into the scramble.");
                return;
            }

            // If 1 set and either IncludeAsh/IncludeLighting is false then force the other. Else roll randomly from all available pots
            if (numberOfRemainingPots == 2 && (settingsIncludeAsh ^ settingsIncludeLightning))
            {
                if (!settingsIncludeAsh) // Force lightning
                {
                    piecesNeededToBePlaced.Add(IxupiPot.LIGHTNING_BOTTOM);
                    Locations[(int)PotLocation.SLIDE] = IxupiPot.LIGHTNING_TOP;
                }
                else if (!settingsIncludeLightning) // Force Ash
                {
                    Locations[(int)PotLocation.DESK_DRAWER] = IxupiPot.ASH_TOP;
                    Locations[(int)PotLocation.GREENHOUSE] = IxupiPot.ASH_BOTTOM;
                }
            }
            else
            {
                List<Ixupi> setsAvailable = IXUPI.ToList();

                // Determine which sets will be included in the scramble
                // First check if lighting/ash are included in the scramble. if not force them
                if (!settingsIncludeAsh)
                {
                    Locations[(int)PotLocation.DESK_DRAWER] = IxupiPot.ASH_TOP;
                    Locations[(int)PotLocation.GREENHOUSE] = IxupiPot.ASH_BOTTOM;
                    numberOfRemainingPots -= 2;
                    setsAvailable.Remove(Ixupi.ASH);
                }
                if (!settingsIncludeLightning)
                {
                    piecesNeededToBePlaced.Add(IxupiPot.LIGHTNING_BOTTOM);
                    Locations[(int)PotLocation.SLIDE] = IxupiPot.LIGHTNING_TOP;
                    numberOfRemainingPots -= 2;
                    setsAvailable.Remove(Ixupi.LIGHTNING);
                }

                // Next select from the remaining sets available
                bool oilSelected = false;
                bool clothSelected = false;
                while (numberOfRemainingPots > 0)
                {
                    int setSelected = rng.Next(0, setsAvailable.Count);
                    Ixupi ixupiSelected = setsAvailable[setSelected];
                    // Check/roll for full pot
                    if (settingsFullPots && rng.Next(0, 2) == 1)
                    {
                        piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_FULL_OFFSET));
                    }
                    else
                    {
                        piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_BOTTOM_OFFSET));
                        piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_TOP_OFFSET));
                    }

                    numberOfRemainingPots -= 2;
                    setsAvailable.RemoveAt(setSelected);

                    //set flags for if oil and cloth were selected for impossible scramble logic check
                    if(ixupiSelected == Ixupi.OIL)
                    {
                        oilSelected = true;
                    }
                    if(ixupiSelected == Ixupi.CLOTH)
                    {
                        clothSelected = true;
                    }    

                }

                //Add Red Herrings
                if(settingsRedHerrings && !settingsFullPots)
                {
                    while (setsAvailable.Count > 0)
                    {
                        int setSelected = rng.Next(0, setsAvailable.Count);
                        Ixupi ixupiSelected = setsAvailable[setSelected];

                        //Roll how many red herrings of each set will be added
                        int herringAmmount = rng.Next(0, 3);

                        //Roll for if red herring is tops or bottoms
                        if(rng.Next(0, 2) == 0)
                        {
                            if(herringAmmount == 1)
                            {
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_BOTTOM_OFFSET));
                            }
                            if(herringAmmount == 2)
                            {
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_BOTTOM_OFFSET));
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_BOTTOM_OFFSET));
                            }
                        }
                        else
                        {
                            if (herringAmmount == 1)
                            {
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_TOP_OFFSET));
                            }
                            if (herringAmmount == 2)
                            {
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_TOP_OFFSET));
                                piecesNeededToBePlaced.Add((IxupiPot)((int)ixupiSelected + POT_TOP_OFFSET));
                            }
                        }

                        setsAvailable.RemoveAt(setSelected);
                    }
                }

                PotLocation randomLocation;
                piecesRemainingToBePlaced = new List<IxupiPot>(piecesNeededToBePlaced);
                while (piecesRemainingToBePlaced.Count > 0)
                {
                    randomLocation = POT_LOCATIONS[rng.Next(POT_LOCATIONS.Count)];
                    if (!settingsExtraLocations && EXTRA_LOCATIONS.Contains(randomLocation)) // Check if extra locations are used
                    {
                        continue;
                    }
                    if (settingsExcludeLyre && randomLocation == PotLocation.LYRE) // Check if lyre excluded
                    {
                        continue;
                    }
                    if (Locations[(int)randomLocation] != 0) // Check if location is filled
                    {
                        continue;
                    }
                    Locations[(int)randomLocation] = piecesRemainingToBePlaced[0];
                    piecesRemainingToBePlaced.RemoveAt(0);
                }

                // Check for bad scramble
                // Check if oil behind oil
                // Check if cloth behind cloth
                // Check if oil behind cloth AND cloth behind oil
                // Check if a piece behind oil with no oil pot available
                // Check if a piece behind cloth with no cloth pot available
                if (OIL_POTS.Contains(Locations[(int)PotLocation.TAR_RIVER]) && oilSelected ||
                    CLOTH_POTS.Contains(Locations[(int)PotLocation.JANITOR_CLOSET]) && clothSelected ||
                    OIL_POTS.Contains(Locations[(int)PotLocation.JANITOR_CLOSET]) && CLOTH_POTS.Contains(Locations[(int)PotLocation.TAR_RIVER]) && (oilSelected || clothSelected) ||
                    Locations[(int)PotLocation.TAR_RIVER] != 0 && !oilSelected ||
                    Locations[(int)PotLocation.JANITOR_CLOSET] != 0 && !clothSelected)
                {
                    goto Scramble;
                }

                // Check if oil behind slide and something behind oil
                // check if cloth behind slide and something behind cloth
                if (settingsEarlyLightning && !settingsEarlyBeth &&
                    (OIL_POTS.Contains(Locations[(int)PotLocation.SLIDE]) && Locations[(int)PotLocation.TAR_RIVER] != 0 ||
                        CLOTH_POTS.Contains(Locations[(int)PotLocation.SLIDE]) && Locations[(int)PotLocation.JANITOR_CLOSET] != 0))
                {
                    goto Scramble;
                }
            }
        }

        // Place pieces in memory
        PlacePieces();

        // Set bytes for red door, beth, and lyre
        if (!settingsVanilla)
        {
            SetKthBitMemoryOneByte(Flags.PuzzleSolvedRedDoor, settingsRedDoor);
            SetKthBitMemoryOneByte(Flags.BethElectricalPanelAvailable, settingsEarlyBeth);
            SetKthBitMemoryOneByte(Flags.PuzzleSolvedLyre, settingsSolvedLyre);
        }

        // Set ixupi captured number
        if (settingsFirstToTheOnlyFive)
        {
            WriteMemory(Addresses.IXUPI_CAPTURED_NUMBER, 10 - firstToTheOnlyXNumber);
        }
        else // Set to 0 if not running First to The Only X
        {
            WriteMemory(Addresses.IXUPI_CAPTURED_NUMBER, 0);
        }

        if (settingsRoomShuffle)
        {
            roomTransitions = new RoomRandomizer(this, rng).RandomizeMap();
        }

        // Sets crawlspace in lobby
        SetKthBitMemoryOneByte(Flags.LobbyTarRiverCrawlspaceAvailable, settingsRoomShuffle);

        // Start fast timer for room shuffle
        if (settingsRoomShuffle)
        {
            if(!useFastTimer)
            {
                FastTimer();
                useFastTimer = true;
            }
        }
        else
        {
            useFastTimer = false;
        }

        overlay.SetInfo();
        mainWindow.label_Flagset.Content = $"Flagset: {overlay.flagset}";

        // Set Seed info and flagset info
        if (setSeedUsed)
        {
            mainWindow.label_Seed.Content = $"Set Seed: {Seed}";
        } else
        {
            mainWindow.label_Seed.Content = $"Seed: {Seed}";
        }

        // -----------Multiplayer------------
        if (settingsMultiplayer && multiplayer_Client != null)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                currentlyRunningThreadOne = true;

                // Disable scramble button till all data is don't being received by server
                disableScrambleButton = true;

                // Send starting pots to server
                multiplayer_Client.sendServerStartingPots(Locations.Select(location => (int)location).ToArray());

                // Send starting skulls to server
                for (int i = 0; i < 6; i++)
                {
                    multiplayer_Client.sendServerSkullDial(i, ReadMemory(836 + i * 4, 1));
                }

                // Send starting flagset to server
                multiplayer_Client.sendServerFlagset(overlay.flagset);

                // Send starting seed
                multiplayer_Client.sendServerSeed(Seed);

                // Send starting skull dials to server

                // Reenable scramble button
                disableScrambleButton = false;

                currentlyRunningThreadOne = false;
            }).Start();
        }

        scrambling = false;
        mainWindow.button_Scramble.IsEnabled = true;
    }

    private void ScrambleFailure(string message)
    {
        new Message(message).ShowDialog();
        scrambling = false;
        mainWindow.button_Scramble.IsEnabled = true;
    }

    public void SetFlagset()
    {
        overlay.UpdateFlagset();
        mainWindow.label_Flagset.Content = $"Flagset: {overlay.flagset}";
    }

    public void PlacePieces()
    {
        IEnumerable<(IxupiPot, int)> potPieces = Locations.Select((potPiece, index) => (potPiece, index));
        foreach(var (potPiece, index) in potPieces)
        {
            WriteMemory(index * 8, (int)potPiece);
        }
    }

    public void StartAppTimer()
    {
        appTimer.Start();
    }

    private int fastTimerCounter = 0;
    private int slowTimerCounter = 0;
    public void FastTimer()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        new Thread(() =>
        {
            while (useFastTimer)
            {
                if (stopwatch.ElapsedMilliseconds >= 4)
                {
                    fastTimerCounter += 1;

                    Dispatcher.Invoke(() =>
                    {
                        mainWindow.label_fastCounter.Content = fastTimerCounter;
                    });

                    GetRoomNumber();

                    if (settingsUnlockEntrance)
                    {
                        OutsideAccess();
                    }

                    RoomShuffle();

                    stopwatch.Restart();
                }
            }
            stopwatch.Stop();
        }).Start();
    }

    private Thread? archipelagoTimerThread = null;
    private Thread? scriptModificationTimerThread = null;
    private ManualResetEvent? stopArchipelagoTimerEvent = null;
    private ManualResetEvent? stopScriptModificationTimerEvent = null;

    public void StartArchipelagoTimer()
    {
        stopArchipelagoTimerEvent = new ManualResetEvent(false);
        Stopwatch stopwatch = new();
        stopwatch.Start();

        archipelagoTimerThread = new Thread(() =>
        {
            while (!stopArchipelagoTimerEvent.WaitOne(0))
            {
                if (stopwatch.ElapsedMilliseconds >= 1)
                {
                    Thread.Sleep(2000);
                    archipelagoTimerTick = true;
                    stopwatch.Restart();
                }
            }
            stopwatch.Stop();
        });
        archipelagoTimerThread.Start();
    }

    public void StartScriptModificationTimer()
    {
        stopScriptModificationTimerEvent = new ManualResetEvent(false);
        Stopwatch stopwatch = new();
        stopwatch.Start();
        
        scriptModificationTimerThread = new Thread(() =>
        {
            while (!stopScriptModificationTimerEvent.WaitOne(0))
            {
                if (stopwatch.ElapsedMilliseconds >= 10)
                {
                    // Modify Scripts
                    ArchipelagoModifyScripts();

                    // Check if a player slipped through a door without a key
                    ArchipelagoKeyCheck();

                    stopwatch.Restart();
                }
            }
            stopwatch.Stop();
        });
        scriptModificationTimerThread.Priority = ThreadPriority.Highest;
        scriptModificationTimerThread.Start();
    }
    private async void Timer_Tick(object? sender, EventArgs e)
    {
        slowTimerCounter += 1;
        mainWindow.label_slowCounter.Content = slowTimerCounter;

        // Check that the process is still Shivers, if so disconnect archipelago and livesplit
        CheckAttachState();

        var windowExists = GetWindowRect((UIntPtr)(long)(shiversProcess?.MainWindowHandle ?? IntPtr.Zero), ref ShiversWindowDimensions);
        var windowIconic = IsIconic((UIntPtr)(long)(shiversProcess?.MainWindowHandle ?? IntPtr.Zero));

        overlay.Left = ShiversWindowDimensions.Left;
        overlay.Top = ShiversWindowDimensions.Top + (int)SystemParameters.WindowCaptionHeight;
        overlay.labelOverlay.Foreground = windowExists && windowIconic ? overlay.brushTransparent : overlay.brushLime;

        if(shiversProcess?.MainWindowHandle != null)
        {
            UIntPtr aboveGameHandle = GetWindow((UIntPtr)(long)shiversProcess?.MainWindowHandle, 3);

            if(aboveGameHandle != overlay.hwnd && aboveGameHandle != UIntPtr.Zero)
            {
                SetWindowPos(overlay.hwnd, aboveGameHandle, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);
            }
        }
        
        if (Seed == 0)
        {
            overlay.labelOverlay.Content = "Not yet randomized";
        }
        if (archipelago_Client != null)
        {
            overlay.OverlayArchipelago();
        }

        // Check if using the fast timer, if not get the room number
        if (!useFastTimer)
        {
            GetRoomNumber();
        }

        if (AddressLocated.HasValue)
        {
            mainWindow.label_ShiversDetected.Content = AddressLocated.Value ? "Shivers Detected! 🙂" : "Shivers not detected! 🙁";
            
            if (windowExists)
            {
                overlay.Show();
            }
            else
            {
                AddressLocated = false;
                overlay.Hide();
            }
        }
        else
        {
            mainWindow.label_ShiversDetected.Content = "";
        }

        #if DEBUG
                mainWindow.button_Scramble.IsEnabled = true;
        #else
                mainWindow.button_Scramble.IsEnabled = roomNumber == 922 && !scrambling;
        #endif

        // Label for ixupi captured number
        int numberIxupiCapturedTemp = ReadMemory(Addresses.IXUPI_CAPTURED_NUMBER, 1);
        if (numberIxupiCapturedTemp > numberIxupiCaptured)
        {
            numberIxupiCaptured = numberIxupiCapturedTemp;
            mainWindow.label_ixupidNumber.Content = numberIxupiCaptured;
            liveSplit?.IxupiCaptured(numberIxupiCaptured);
        }

        // Label for base memory address
        mainWindow.label_baseMemoryAddress.Content = MyAddress.ToString("X8");

        // Early lightning
        if (settingsEarlyLightning && !settingsVanilla)
        {
            EarlyLightning();

            // Anywhere lightning
            if (settingsAnywhereLightning && roomNumber > 1000)
            {
                AnywhereLightning();
            }
        }
        else if (settingsFirstToTheOnlyFive)
        {
            HasGameFinished();
        }


        // Elevators Stay Solved
        // Only 4x4 elevators.
        ElevatorSettings();

        // Check if oil is captured in room shuffle
        if (settingsRoomShuffle)
        {
            CheckOil();
        }

        // Locks/Unlocks entrance
        if (settingsUnlockEntrance)
        {
            SetKthBitMemoryOneByte(Flags.ExploreMode, roomNumber == 1550 || roomNumber == 9670);
        }

        //Flash Travel
        if (settingsFlashTravel)
        {
            FlashTravel();
        }

        int healthTemp = ReadMemory(-40, 1);
        if (healthTemp != health)
        {
            healthPrevious = health;
            health = healthTemp;
            liveSplit?.HealthChanged(healthPrevious, health, roomNumber);
        }

        liveSplit?.BethRiddleFound();
        liveSplit?.JukeboxSet();
        

        // ---------Multiplayer----------

        if (multiplayer_Client != null)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                // if (settingsMultiplayer && runThreadIfAvailable && !currentlyRunningThreadTwo && !currentlyRunningThreadOne)
                if (settingsMultiplayer && !currentlyRunningThreadTwo && !currentlyRunningThreadOne)
                {
                    currentlyRunningThreadTwo = true;
                    disableScrambleButton = true;

                    // Request current pot list from server
                    multiplayer_Client.sendServerRequestPotList();

                    // Monitor each location and send a sync update to server if it differs
                    for (int i = 0; i < 23; i++)
                    {
                        int potRead = ReadMemory(i * 8, 1);
                        if (potRead != multiplayerLocations[i]) // All locations are 8 apart in the memory so can multiply by i
                        {
                            multiplayerLocations[i] = potRead;
                            multiplayer_Client.sendServerPotUpdate(i, multiplayerLocations[i]);
                        }
                    }

                    // Check if a piece needs synced from another player
                    for (int i = 0; i < 23; i++)
                    {
                        if (ReadMemory(i * 8, 1) != multiplayer_Client.syncPiece[i]) // All locations are 8 apart in the memory so can multiply by i
                        {
                            WriteMemory(i * 8, multiplayer_Client.syncPiece[i]);
                            multiplayerLocations[i] = multiplayer_Client.syncPiece[i];

                            // Force a screen redraw if looking at pot being synced
                            PotSyncRedraw();
                        }
                    }

                    // Check if an ixupi was captured, if so send to the server
                    int ixupiCaptureRead = ReadMemory(-60, 2);

                    for (int i = 0; i < 10; i++)
                    {
                        if (IsKthBitSet(ixupiCaptureRead, i) && multiplayerIxupi[i] == false) // Check if ixupi at specific bit is now set, and if its not set in multiplayerIxupi list
                        {
                            multiplayerIxupi[i] = true;
                            multiplayer_Client.sendServerIxupiCaptured(i);
                        }
                    }

                    // Check what the latest ixupi captured list is and see if a sync needs completed
                    // A list is automatically sent on a capture, this is just backup, only pull a list only once every 10 seconds or so
                    multiplayerSyncCounter += 1;
                    if (multiplayerSyncCounter > 600)
                    {
                        multiplayerSyncCounter = 0;
                        multiplayer_Client.sendServerRequestIxupiCapturedList();
                    }
                    
                    if (ixupiCaptureRead < multiplayer_Client.ixupiCapture)
                    {
                        // Set the ixupi captured
                        WriteMemory(-60, multiplayer_Client.ixupiCapture);

                        // Redraw pots on the inventory bar by setting previous room to the name select
                        multiplayerScreenRedrawNeeded = true;

                        // Remove captured ixupi from the game and count how many have been captured
                        ixupiCaptureRead = multiplayer_Client.ixupiCapture;
                        int multiplayerNumCapturedIxupi = 0;

                        if (IsKthBitSet(ixupiCaptureRead, 0)) // Sand
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.SAND, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 1)) // Crystal
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CRYSTAL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 2)) // Metal
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.METAL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 3)) // Oil
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 4)) // Wood
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WOOD, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 5)) // Lightning
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.LIGHTNING, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 6)) // Ash
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.ASH, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 7)) // Water
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WATER, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 8)) // Cloth
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CLOTH, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 9)) // Wax
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WAX, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                    }

                    // Synchronize Skull Dials
                    // If looking at a skull and the value in memory has changed, the player has changed it, send to server
                    int[] skullDialColor =
                    {
                        ReadMemory(836, 1),
                        ReadMemory(840, 1),
                        ReadMemory(844, 1),
                        ReadMemory(848, 1),
                        ReadMemory(852, 1),
                        ReadMemory(856, 1)

                    };
                    switch (roomNumber) // Player has changed a skull dial
                    {
                        case 11330: // Prehistoric
                            if (multiplayer_Client.skullDials[0] != skullDialColor[0])
                            {
                                multiplayer_Client.sendServerSkullDial(0, skullDialColor[0]);
                                multiplayer_Client.skullDials[0] = skullDialColor[0];
                            }
                            break;
                        case 14170: // Tar River
                            if (multiplayer_Client.skullDials[1] != skullDialColor[1])
                            {
                                multiplayer_Client.sendServerSkullDial(1, skullDialColor[1]);
                                multiplayer_Client.skullDials[1] = skullDialColor[1];
                            }
                            break;
                        case 24170: // Werewolf
                            if (multiplayer_Client.skullDials[2] != skullDialColor[2])
                            {
                                multiplayer_Client.sendServerSkullDial(2, skullDialColor[2]);
                                multiplayer_Client.skullDials[2] = skullDialColor[2];
                            }
                            break;
                        case 21400: // Burial
                            if (multiplayer_Client.skullDials[3] != skullDialColor[3])
                            {
                                multiplayer_Client.sendServerSkullDial(3, skullDialColor[3]);
                                multiplayer_Client.skullDials[3] = skullDialColor[3];
                            }
                            break;
                        case 20190: // Egypt
                            if (multiplayer_Client.skullDials[4] != skullDialColor[4])
                            {
                                multiplayer_Client.sendServerSkullDial(4, skullDialColor[4]);
                                multiplayer_Client.skullDials[4] = skullDialColor[4];
                            }
                            break;
                        case 23650: // Gods
                            if (multiplayer_Client.skullDials[5] != skullDialColor[5])
                            {
                                multiplayer_Client.sendServerSkullDial(5, skullDialColor[5]);
                                multiplayer_Client.skullDials[5] = skullDialColor[5];
                            }
                            break;
                    }

                    for (int i = 0; i < 6; i++) // Other player has changed a skull dial
                    {
                        if (multiplayer_Client.skullDials[i] != skullDialColor[i])
                        {
                            WriteMemory(836 + i * 4, multiplayer_Client.skullDials[i]);
                        }
                    }

                    // Check if a screen redraw allowed. 
                    if (multiplayerScreenRedrawNeeded)
                    {
                        // Check if screen redraw allowed
                        bool ScreenRedrawAllowed = CheckScreenRedrawAllowed();
                        if (ScreenRedrawAllowed)
                        {
                            multiplayerScreenRedrawNeeded = false;
                            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 922);
                        }
                    }

                    disableScrambleButton = false;
                    currentlyRunningThreadTwo = false;
                }
            }).Start();
        }


        // ---------Archipelago----------
        
        mainWindow.button_Archipelago.IsEnabled = MyAddress != UIntPtr.Zero;

        // Update client window to show pot locations
        archipelago_Client?.ArchipelagoUpdateWindow(roomNumber, archipelagoReceivedItems);

        if (archipelago_Client?.IsConnected ?? false && AddressLocated.HasValue && AddressLocated.Value)
        {
            mainWindow.button_Scramble.IsEnabled = false;

            // Prevent the player from entering the save/load screen
            ArchipelagoPreventSaveLoad();

            // Initialization
            if (!archipelagoInitialized)
            {
                archipelagoElevatorSettings = archipelago_Client.slotDataSettingElevators;
                archipelagoCollectBehavior = archipelago_Client.slotDataCollectBehavior;

                if (!archipelagoReportedNewItems)
                {
                    archipelago_Client.ReportNewItemsReceived();
                    archipelagoReceivedItems = archipelago_Client.GetItemsFromArchipelagoServer();
                    archipelagoReportedNewItems = true;
                }

                if (roomNumber == 922)
                {
                    CleanUpRandomizer();
                    StartArchipelagoTimer(); // 2 second timer so we aren't hitting the archipelago server as fast as possible
                    StartScriptModificationTimer();
                    archipelagoInitialized = true;

                    // Remove all pot pieces from museum
                    // Start be clearing any pot data
                    Array.Fill<IxupiPot>(Locations, 0);

                    // Place empty locations
                    PlacePieces();

                    // Initialize data storage
                    archipelago_Client.InitializeDataStorage(
                        ReadMemory(836, 1), ReadMemory(840, 1), ReadMemory(844, 1), ReadMemory(848, 1), ReadMemory(852, 1), ReadMemory(856, 1) // Skull Dial States
                    );

                    // Load flags
                    await ArchipelagoLoadData();
                    ArchipelagoLoadFlags();
                }
                else
                {
                    // If player isn't on registry page, move player to title screen, also send message to player to tell them to move to the registry page
                    if (!archipelagoRegistryMessageSent)
                    {
                        archipelago_Client.MoveToRegistry();
                        archipelagoRegistryMessageSent = true;
                    }
                }
            }
            else
            {
                if (archipelagoTimerTick == true && !archipelagoRunningTick)
                {
                    archipelagoRunningTick = true;

                    // Get items
                    archipelagoReceivedItems = archipelago_Client.GetItemsFromArchipelagoServer() ?? new();

                    // Send Checks
                    if (!windowIconic)
                    {
                        // Send checks
                        ArchipelagoSendChecks();

                        // Stage checks for sending next iteration. This is to prevent checks being sent on killing the shivers process. 
                        // Killing the process causes garbled memory to be read and sent as checks.
                        ArchipelagoStageChecks();
                    }

                    // If received a pot piece, place it in the museum.
                    ArchipelagoPlacePieces();

                    // Heal character if heal received
                    ArchipelagoHeal();

                    // Check if player is dead, if so save a value of 0, in the load data function it will reset the player
                    if (ReadMemory(-40, 0) == 0 || roomNumber == 914)
                    {
                        var dataStorage = archipelago_Client?.dataStorage;
                        if (archipelago_Client != null && dataStorage != null)
                        {
                            archipelago_Client.dataStorage = dataStorage with { Health = 0 };
                            archipelago_Client.SaveData(archipelagoReceivedItems?.Count ?? 0);
                        }
                    }

                    // Save Data
                    ArchipelagoSaveData();

                    // Check for victory
                    if (finalCutsceneTriggered)
                    {
                        archipelago_Client?.SendCheck(ARCHIPELAGO_BASE_LOCATION_ID + 116);
                        archipelago_Client?.Send_completion();
                    }

                    archipelagoTimerTick = false;
                    archipelagoRunningTick = false;
                }

                //Check if release mode disabled. If so prevent explore mode. This is specifically outside of the tick event so that it is checked quickly.
                ArchipelagoReleaseDisabled();

                // Allow outside access
                OutsideAccess();

                // Always available ixupi from filler items
                ArchipelagoAvailableIxupi();

                // Elevators stay solved
                ElevatorSettings();

                // Early Lightning Setting
                if(archipelago_Client?.slotDataEarlyLightning ?? false)
                {
                    EarlyLightning();
                }
                else
                {
                    HasGameFinished();
                }

                if (roomNumber == 1550 || roomNumber == 9670)// Front door useable
                {
                    if (archipelagoReceivedItems?.Contains((int)APItemID.KEYS.FRONT_DOOR) ?? false)
                    {
                        SetKthBitMemoryOneByte(Flags.ExploreMode, true);
                    }
                }
                else
                {
                    SetKthBitMemoryOneByte(Flags.ExploreMode, false);
                }

                // Set flags for checks that are sent based on room number. These need captured immediately and not on the send checks timer
                if (roomNumber == 23311) // Stone Tablet Message Seen
                {
                    archipelagoCheckStoneTablet = true;
                }
                if (roomNumber == 7162) // Basilisk Paper Seen
                {
                    archipelagoCheckBasilisk = true;
                }
                if (roomNumber == 12590 && (ReadMemory(-400, 2) == 5857 || ReadMemory(-400, 2) == 5975)) // Siren Song Heard
                {
                    archipelagoCheckSirenSong = true;
                }
                if (roomNumber == 20572 && ReadMemory(-400, 2) == 5975) // Egyptian Sphinx Heard
                {
                    archipelagoCheckEgyptianSphinx = true;
                }
                if (roomNumber == 32810) // Gallows Plaque Seen
                {
                    archipelagoCheckGallowsPlaque = true;
                }
                if (roomNumber == 34040) // Geoffrey Writing In Elevator Seen
                {
                    archipelagoCheckGeoffreyWriting = true;
                }
                if (roomNumber == 29830) // Information Plaque: UFO
                {
                    archipelagoCheckPlaqueUFO = true;
                }

                // ----TODO: Exclude locations----
                // ----TODO: Fix the freeze if server is stopped before closing client, it hangs on send check in client.cs
                // ----TODO: Generate a list of screens we are allowed to redraw on, then when the health meter is adjusted redraw the screen. Currently the screen is redrawn when modifying a script which happens when the player gets to a door

                // If player goes back to main menu reinitialize
                if (roomNumber == 910)
                {
                    archipelagoInitialized = false;
                    archipelagoRegistryMessageSent = false;
                    Array.Fill(archipelagoPiecePlaced, false);
                }
            }
        }
        else if (archipelago_Client?.IsConnected == true)
        {
            await (archipelago_Client?.DisconnectAsync() ?? Task.CompletedTask);
        }
        else
        {
            archipelago_Client?.CheckConnection();
        }


        //Dev Menu
        dev_Menu?.update_Pot_Locations();
    }

    private void ArchipelagoReleaseDisabled()
    {
        //If game mode is release disabled then prevent explore museum mode (Which sends checks automatically) by sending player to last know location to continue exploring.
        if (roomNumber == 923 && archipelagoReleaseDisabled)
        {
            if(archipelago_Client?.dataStorage != null)
            {
                WriteMemory(Addresses.PLAYER_LOCATION, archipelago_Client.dataStorage.PlayerLocation);
            }
            else
            {
                WriteMemory(Addresses.PLAYER_LOCATION, 1012);
            }

        }
    }

    private void ArchipelagoHeal()
    {
        int numberOfHealsCurrent = archipelagoReceivedItems.Count(num => num == (int)APItemID.FILLER.HEAL);
        if (numberOfHealsCurrent > archipelagoHealCountPrevious)
        {
            // A Heal was received
            int currentHealth = ReadMemory(-40, 1);

            // See if a heal is required
            if (currentHealth < 100)
            {
                int numberOfHealsReceived = numberOfHealsCurrent - archipelagoHealCountPrevious;

                // Add 10 health for each heal item received
                currentHealth = Math.Min(currentHealth + numberOfHealsReceived * 10, 100);
                WriteMemory(-40, currentHealth);

                // Remove 10 dmg from any ixupi for each heal item received
                while (numberOfHealsReceived > 0)
                {
                    int ixupiDamage = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        ixupiDamage = ReadMemory(184 + i * 8, 1);
                        if(ixupiDamage > 0)
                        {
                            WriteMemory(184 + i * 8, ixupiDamage - 10);
                            numberOfHealsReceived--;
                            break;
                        }
                    }

                    // If no damage was found then break the while loop
                    if(ixupiDamage == 0)
                    {
                        break;
                    }
                }
            }
        }

        archipelagoHealCountPrevious = numberOfHealsCurrent;
    }

    private void ArchipelagoKeyCheck()
    {
        // Checks for if a player went through a door without a key
        if (roomNumber > 1000 && roomNumberPrevious > 1000)
        {
            if (
                (roomNumber == 5010 && roomNumberPrevious == 4620 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR) ?? false)) ||   // Office Elevator from underground lake side
                (roomNumber == 5130 && roomNumberPrevious == 6290 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR) ?? false)) ||   // Office Elevator from office side
                (roomNumber == 38010 && roomNumberPrevious == 38110 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.BEDROOM_ELEVATOR) ?? false)) || // Bedroom Elevator from Office side
                (roomNumber == 38011 && roomNumberPrevious == 37330 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.BEDROOM_ELEVATOR) ?? false)) || // Bedroom Elevator from bedroom hallway side
                (roomNumber == 34030 && roomNumberPrevious == 10100 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false)) || // 3-Floor Elevator from Maintenance Tunnels
                (roomNumber == 34030 && roomNumberPrevious == 27212 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false)) || // 3-Floor Elevator from Blue Maze Bottom
                (roomNumber == 34030 && roomNumberPrevious == 33140 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false)) || // 3-Floor Elevator from Blue Maze Top
                (roomNumber == 7010 && roomNumberPrevious == 6260 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.WORKSHOP) ?? false)) ||   // Workshop
                (roomNumber == 9020 && roomNumberPrevious == 6030 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE) ?? false)) ||   // Lobby from Office
                (roomNumber == 6020 && roomNumberPrevious == 9010 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE) ?? false)) ||   // Office from Lobby
                (roomNumber == 11020 && roomNumberPrevious == 9590 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PREHISTORIC) ?? false)) ||  // Prehistoric
                (roomNumber == 19040 && roomNumberPrevious == 11320 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.GREENHOUSE) ?? false)) || // Greenhouse
                (roomNumber == 12010 && roomNumberPrevious == 11120 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OCEAN) ?? false)) || // Ocean
                (roomNumber == 17010 && roomNumberPrevious == 18230 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PROJECTOR) ?? false)) || // Projector Room
                (roomNumber == 39010 && roomNumberPrevious == 10290 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.GENERATOR) ?? false)) || // Generator Room
                (roomNumber == 20060 && roomNumberPrevious == 9570 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.EGYPT) ?? false)) ||  // Egypt from Lobby
                (roomNumber == 9560 && roomNumberPrevious == 20040 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.EGYPT) ?? false)) ||  // Lobby from Egypt
                (roomNumber == 9450 && roomNumberPrevious == 8030 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.LIBRARY) ?? false)) ||  // Lobby from Library
                (roomNumber == 8000 && roomNumberPrevious == 9470 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.LIBRARY) ?? false)) ||   // Library from Lobby
                (roomNumber == 22020 && roomNumberPrevious == 21440 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.SHAMAN) ?? false)) || // Shaman
                (roomNumber == 29460 && roomNumberPrevious == 30010 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UFO) ?? false)) || // UFO from Inventions side
                (roomNumber == 30020 && roomNumberPrevious == 29450 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UFO) ?? false)) || // UFO from UFO side
                (roomNumber == 32010 && roomNumberPrevious == 30430 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.TORTURE) ?? false)) || // Torture
                (roomNumber == 31020 && roomNumberPrevious == 32450 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PUZZLE) ?? false)) || // Puzzle
                (roomNumber == 37010 && roomNumberPrevious == 37300 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.BEDROOM) ?? false)) || // Bedroom
                (roomNumber == 3020 && roomNumberPrevious == 2330 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM) ?? false)) ||   // Underground Lake Room from Stonehenge side
                (roomNumber == 2320 && roomNumberPrevious == 3010 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM) ?? false)) ||   // Underground Lake Room from Lake side
                (roomNumber == 25010 && roomNumberPrevious == 26310 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.JANITOR_CLOSET) ?? false)) || // Janitor Closet
                (roomNumber == 9660 && roomNumberPrevious == 1550 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.FRONT_DOOR) ?? false)) || // Front Door from Outside
                (roomNumber == 1551 && roomNumberPrevious == 9670 && !(archipelagoReceivedItems?.Contains((int)APItemID.KEYS.FRONT_DOOR) ?? false)) || // Front Door from Lobby
                (roomNumber == 38110 && roomNumberPrevious == 6030 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false)) ||  // Office Crawl Space
                (roomNumber == 10460 && roomNumberPrevious == 18240 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false)) || // Theater Backhalls Crawlspace
                (roomNumber == 9620 && roomNumberPrevious == 15260 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false)) ||  // Tar River Crawlspace from Tar River
                (roomNumber == 15240 && roomNumberPrevious == 9630 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false)) ||  // Tar River Crawlspace from Lobby
                (roomNumber == 27024 && roomNumberPrevious == 20150 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false)) || // Egypt Crawl Space from Egypt Side
                (roomNumber == 20160 && roomNumberPrevious == 27023 && !(archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false))    // Egypt Crawl Space from Blue Halls Side
            )
            {
                WriteMemory(Addresses.PLAYER_LOCATION, roomNumberPrevious);
            }
        }
    }

    private async Task ArchipelagoLoadData()
    {
        archipelagoCurrentlyLoadingData = true; //This is used to prevent the save data method running before we have loaded all of the data

        if (archipelago_Client == null)
        {
            archipelagoCurrentlyLoadingData = false;
            return;
        }

        await archipelago_Client.LoadData();

        if (archipelago_Client.dataStorage == null)
        {
            archipelagoCurrentlyLoadingData = false;
            return;
        }

        ArchipelagoDataStorage dataStorage = archipelago_Client.dataStorage;

        // Load player location
        if (dataStorage.PlayerLocation >= 1000 && archipelagoCompleteScriptList.Contains(dataStorage.PlayerLocation))
        {
            WriteMemory(Addresses.PLAYER_LOCATION, dataStorage.PlayerLocation);
        }
        else
        {
            WriteMemory(Addresses.PLAYER_LOCATION, 1012);
        }

        // Load skull dials
        dataStorage.SkullDials.Values.ToList().ForEach(skullDial =>
        {
            WriteMemory(skullDial.Location, skullDial.Value);
        });

        // Load Jukebox State
        if (dataStorage.Jukebox)
        {
            // Check not obtained but jukebox was set
            ArchipelagoSetFlagBit(Flags.JukeboxSet); // Jukebox Set
        }

        // Load Tar River Shortcut flag
        if (dataStorage.TarRiverShortcut)
        {
            ArchipelagoSetFlagBit(Flags.LobbyTarRiverCrawlspaceAvailable); // Tar River Shortcut open flag set
        }

        // Load Player Health and ixupi damage
        if (dataStorage.Health == 0) // 0 Health was saved, reset health to 100 and ixupi damage to 0, move player to front gate
        {
            WriteMemory(-40, 100); // Set player health to 100
            for (int i = 0; i < 10; i++)
            {
                WriteMemory(184 + i * 8, 0); // Set Ixupi damage to 0
            }

            WriteMemory(Addresses.PLAYER_LOCATION, 1012);
        }
        else
        {
            // Load player health
            WriteMemory(-40, health);

            // Load Ixupi Damage
            dataStorage.IxupiDamage.Values.ToList().ForEach(ixupiDamage =>
            {
                WriteMemory(ixupiDamage.Location, ixupiDamage.Value);
            });
        }

        archipelagoHealCountPrevious = dataStorage.HealItemsReceived;

        // Load Ixupi Captured Data
        int ixupiCapturedStates = dataStorage.IxupiCapturedStates;
        int ixupiCapturedAmount = 10 - archipelago_Client?.slotDataIxupiCapturesNeeded ?? 10;

        // Determine how many Ixupi are captured
        for (int i = 0; i < 10; i++)
        {
            if(IsKthBitSet(ixupiCapturedStates, i))
            {
                ixupiCapturedAmount += 1;
            }
        }
        // Set ixupi captured
        WriteMemory(-60, ixupiCapturedStates);

        // Set ixupi captured amount in memory, and local variable
        WriteMemory(Addresses.IXUPI_CAPTURED_NUMBER, ixupiCapturedAmount);
        numberIxupiCaptured = ixupiCapturedAmount;

        // Remove Captured Ixupi
        ArchipelagoRemoveCapturedIxupi();

        WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 922); // Refresh screen to redraw inventory

        archipelagoCurrentlyLoadingData = false;
    }

    private void ArchipelagoSaveData()
    {
        if (archipelago_Client == null || archipelago_Client.dataStorage == null)
        {
            return;
        }
        // Make sure in the game
        if (roomNumber >= 1000 && !archipelagoCurrentlyLoadingData) //Room 912 is the game over screen, allow data to save on that screen to trigger a health save of 0 to restart player 
        {
            ArchipelagoDataStorage dataStorage = archipelago_Client.dataStorage;

            // Save player location, but not on the boat
            if (archipelagoCompleteScriptList.Contains(roomNumber) && !(roomNumber >= 3120 && roomNumber <= 3320 || roomNumber == 12600 || roomNumber == 3500 || roomNumber == 3510))
            {
                dataStorage = dataStorage with { PlayerLocation = roomNumber };
            }

            // Save skull dials
            dataStorage.SkullDials["Prehistoric"] = dataStorage.SkullDials["Prehistoric"] with { Value = ReadMemory(836, 1) };
            dataStorage.SkullDials["TarRiver"] = dataStorage.SkullDials["TarRiver"] with { Value = ReadMemory(840, 1) };
            dataStorage.SkullDials["Werewolf"] = dataStorage.SkullDials["Werewolf"] with { Value = ReadMemory(844, 1) };
            dataStorage.SkullDials["Burial"] = dataStorage.SkullDials["Burial"] with { Value = ReadMemory(848, 1) };
            dataStorage.SkullDials["Egypt"] = dataStorage.SkullDials["Egypt"] with { Value = ReadMemory(852, 1) };
            dataStorage.SkullDials["Gods"] = dataStorage.SkullDials["Gods"] with { Value = ReadMemory(856, 1) };

            // Save Ixupi damage
            dataStorage.IxupiDamage["Water"] = dataStorage.IxupiDamage["Water"] with { Value = ReadMemory(184, 1) };
            dataStorage.IxupiDamage["Wax"] = dataStorage.IxupiDamage["Wax"] with { Value = ReadMemory(192, 1) };
            dataStorage.IxupiDamage["Ash"] = dataStorage.IxupiDamage["Ash"] with { Value = ReadMemory(200, 1) };
            dataStorage.IxupiDamage["Oil"] = dataStorage.IxupiDamage["Oil"] with { Value = ReadMemory(208, 1) };
            dataStorage.IxupiDamage["Cloth"] = dataStorage.IxupiDamage["Cloth"] with { Value = ReadMemory(216, 1) };
            dataStorage.IxupiDamage["Wood"] = dataStorage.IxupiDamage["Wood"] with { Value = ReadMemory(224, 1) };
            dataStorage.IxupiDamage["Crystal"] = dataStorage.IxupiDamage["Crystal"] with { Value = ReadMemory(232, 1) };
            dataStorage.IxupiDamage["Lightning"] = dataStorage.IxupiDamage["Lightning"] with { Value = ReadMemory(240, 1) };
            dataStorage.IxupiDamage["Sand"] = dataStorage.IxupiDamage["Sand"] with { Value = ReadMemory(248, 1) };
            dataStorage.IxupiDamage["Metal"] = dataStorage.IxupiDamage["Metal"] with { Value = ReadMemory(256, 1) };

            var byte360 = ReadMemory(360, 1);
            var byte361 = ReadMemory(361, 1);
            var byte364 = ReadMemory(364, 1);
            var byte365 = ReadMemory(365, 1);
            var byte368 = ReadMemory(368, 1);
            var byte372 = ReadMemory(372, 1);
            var byte377 = ReadMemory(377, 1);
            var byte380 = ReadMemory(380, 1);
            var byte381 = ReadMemory(381, 1);

            dataStorage = dataStorage with
            {
                Jukebox = dataStorage.Jukebox || IsKthBitSet(byte377, 5),
                TarRiverShortcut = dataStorage.TarRiverShortcut || IsKthBitSet(byte368, 6),
                Health = ReadMemory(-40, 1),
                HealItemsReceived = archipelagoReceivedItems?.Count(num => num == (int)APItemID.FILLER.HEAL) ?? 0,
                IxupiCapturedStates = ReadMemory(-60, 2),
                PuzzlesSolved = dataStorage.PuzzlesSolved with
                {
                    CombinationLock = dataStorage.PuzzlesSolved.CombinationLock || IsKthBitSet(byte372, Flags.PuzzleSolvedComboLock.Bit),
                    Gears = dataStorage.PuzzlesSolved.Gears || IsKthBitSet(byte361, Flags.PuzzleSolvedGears.Bit),
                    Stonehenge = dataStorage.PuzzlesSolved.Stonehenge || IsKthBitSet(byte361, Flags.PuzzleSolvedStonehenge.Bit),
                    WorkshopDrawers = dataStorage.PuzzlesSolved.WorkshopDrawers || IsKthBitSet(byte377, Flags.PuzzleSolvedWorkshopDrawers.Bit),
                    LibraryStatue = dataStorage.PuzzlesSolved.LibraryStatue || IsKthBitSet(byte368, Flags.PuzzleSolvedLibraryStatue.Bit),
                    TheaterDoor = dataStorage.PuzzlesSolved.TheaterDoor || IsKthBitSet(byte364, Flags.PuzzleSolvedTheaterDoor.Bit),
                    ClockTowerDoor = dataStorage.PuzzlesSolved.ClockTowerDoor || IsKthBitSet(byte364, Flags.PuzzleSolvedClocktowerDoor.Bit),
                    ClockChains = dataStorage.PuzzlesSolved.ClockChains || IsKthBitSet(byte380, Flags.PuzzleSolvedClockChains.Bit),
                    Atlantis = dataStorage.PuzzlesSolved.Atlantis || IsKthBitSet(byte360, Flags.PuzzleSolvedAtlantis.Bit),
                    Organ = dataStorage.PuzzlesSolved.Organ || IsKthBitSet(byte360, Flags.PuzzleSolvedOrgan.Bit),
                    MazeDoor = dataStorage.PuzzlesSolved.MazeDoor || IsKthBitSet(byte364, Flags.PuzzleSolvedMazeDoor.Bit),
                    ColumnsOfRA = dataStorage.PuzzlesSolved.ColumnsOfRA || IsKthBitSet(byte365, Flags.PuzzleSolvedColumnsOfRA.Bit),
                    BurialDoor = dataStorage.PuzzlesSolved.BurialDoor || IsKthBitSet(byte365, Flags.PuzzleSolvedBurialDoor.Bit),
                    ChineseSolitaire = dataStorage.PuzzlesSolved.ChineseSolitaire || IsKthBitSet(byte381, Flags.PuzzleSolvedChineseSolitaire.Bit),
                    ShamanDrums = dataStorage.PuzzlesSolved.ShamanDrums || IsKthBitSet(byte365, Flags.PuzzleSolvedShamanDrums.Bit),
                    Lyre = dataStorage.PuzzlesSolved.Lyre || IsKthBitSet(byte365, Flags.PuzzleSolvedLyre.Bit),
                    RedDoor = dataStorage.PuzzlesSolved.RedDoor || IsKthBitSet(byte364, Flags.PuzzleSolvedRedDoor.Bit),
                    FortuneTellerDoor = dataStorage.PuzzlesSolved.FortuneTellerDoor || IsKthBitSet(byte364, Flags.PuzzleSolvedFortuneTellerDoor.Bit),
                    Alchemy = dataStorage.PuzzlesSolved.Alchemy || IsKthBitSet(byte372, Flags.PuzzleSolvedAlchemy.Bit),
                    UFOSymbols = dataStorage.PuzzlesSolved.UFOSymbols || IsKthBitSet(byte377, Flags.PuzzleSolvedUFO.Bit),
                    AnansiMusicBoc = dataStorage.PuzzlesSolved.AnansiMusicBoc || IsKthBitSet(byte380, Flags.AnansiMusicBoxOpen.Bit),
                    Gallows = dataStorage.PuzzlesSolved.Gallows || IsKthBitSet(byte381, Flags.PuzzleSolvedGallows.Bit),
                    Mastermind = dataStorage.PuzzlesSolved.Mastermind || IsKthBitSet(byte377, Flags.PuzzleSolvedMasermind.Bit),
                    MarblePinball = dataStorage.PuzzlesSolved.MarblePinball || IsKthBitSet(byte360, Flags.PuzzleSolvedMarblePinball.Bit),
                    SkullDialDoor = dataStorage.PuzzlesSolved.SkullDialDoor || IsKthBitSet(ReadMemory(376, 1), 1),
                    OfficeElevator = dataStorage.PuzzlesSolved.OfficeElevator || elevatorOfficeSolved,
                    BedroomElevator = dataStorage.PuzzlesSolved.BedroomElevator || elevatorBedroomSolved,
                    ThreeFloorElevator = dataStorage.PuzzlesSolved.ThreeFloorElevator || elevatorThreeFloorSolved
                }
            };

            archipelago_Client.dataStorage = dataStorage;
            archipelago_Client.SaveData(archipelagoReceivedItems?.Count ?? 0);
        }
    }

    private void ArchipelagoPreventSaveLoad()
    {
        // If on the save/load screen and 
        if (roomNumber == 993 || roomNumber == 927)
        {
            if (roomNumberPrevious != 0) // There is a previous room number
            {
                WriteMemory(Addresses.PLAYER_LOCATION, roomNumberPrevious);
            }
            else // If no previous room number, that means the player attached after already being on the screen
                 // This means we can go ahead and move them to title screen
            {
                WriteMemory(Addresses.PLAYER_LOCATION, 910); // Set room to title
                
            }
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, roomNumber); // Set previous room number in memory to restart the title music if that is the screen the player was on
        }
    }

    private void ArchipelagoAvailableIxupi()
    {
        // Get ixupi captured list
        int ixupiCaptured = ReadMemory(-60, 2);

        if (!IsKthBitSet(ixupiCaptured, 7)) // Water not captured
        {
            if (roomNumber >= 9000 && roomNumber < 10000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WATER_LOBBY) ?? false)) // Lobby
            {
                WriteMemory((int)IxupiLocationOffsets.WATER, 9000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 9)) // Wax not captured
        {
            if (roomNumber >= 8000 && roomNumber < 9000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WAX_LIBRARY) ?? false)) // Library
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 8000);
            }
            else if (roomNumber >= 24000 && roomNumber < 25000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WAX_ANANSI) ?? false)) // Anansi Room
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 24000);
            }
            else if (roomNumber >= 22000 && roomNumber < 23000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WAX_SHAMAN) ?? false)) // Shaman Room
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 22000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 6)) // Ash not captured
        {
            if (roomNumber >= 6000 && roomNumber < 7000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.ASH_OFFICE) ?? false)) // Office
            {
                WriteMemory((int)IxupiLocationOffsets.ASH, 6000);
            }
            else if (roomNumber >= 21000 && roomNumber < 22000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.ASH_BURIAL) ?? false)) // Burial Room
            {
                WriteMemory((int)IxupiLocationOffsets.ASH, 21000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 3)) // Oil not captured
        {
            if (roomNumber >= 11000 && roomNumber < 12000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.OIL_PREHISTORIC) ?? false)) // Prehistoric
            {
                WriteMemory((int)IxupiLocationOffsets.OIL, 11000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 8)) // Cloth not captured
        {
            if (roomNumber >= 20000 && roomNumber < 21000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.CLOTH_EGYPT) ?? false)) // Egypt
            {
                WriteMemory((int)IxupiLocationOffsets.CLOTH, 20000);
            }
            else if (roomNumber >= 21000 && roomNumber < 22000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.CLOTH_BURIAL) ?? false)) // Burial Room
            {
                WriteMemory((int)IxupiLocationOffsets.CLOTH, 21000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 4)) // Wood not captured
        {
            if (roomNumber >= 7000 && roomNumber < 8000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WOOD_WORKSHOP) ?? false)) // Workshop
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 7000);
            }
            else if (roomNumber >= 27000 && roomNumber < 28000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WOOD_BLUE_MAZE) ?? false)) // Blue Maze
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 36000);
            }
            else if (roomNumber >= 24000 && roomNumber < 25000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WOOD_PEGASUS) ?? false)) // Pegasus Room
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 24000);
            }
            else if (roomNumber >= 23000 && roomNumber < 24000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.WOOD_GODS) ?? false)) // Gods Room
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 23000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 1)) // Crystal not captured
        {
            if (roomNumber >= 9000 && roomNumber < 10000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.CRYSTAL_LOBBY) ?? false)) // Lobby
            {
                WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 9000);
            }
            else if (roomNumber >= 12000 && roomNumber < 13000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.CRYSTAL_OCEAN) ?? false)) // Ocean
            {
                WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 12000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 0)) // Sand not captured
        {
            if (roomNumber >= 19000 && roomNumber < 20000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.SAND_GREENHOUSE) ?? false)) // Greenhouse
            {
                WriteMemory((int)IxupiLocationOffsets.SAND, 19000);
            }
            else if (roomNumber >= 12000 && roomNumber < 13000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.SAND_OCEAN) ?? false)) // Ocean
            {
                WriteMemory((int)IxupiLocationOffsets.SAND, 12000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 2)) // Metal not captured
        {
            if (roomNumber >= 17000 && roomNumber < 18000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.METAL_PROJECTOR) ?? false)) // Projector
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 17000);
            }
            else if (roomNumber >= 37000 && roomNumber < 38000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.METAL_BEDROOM) ?? false)) // Bedroom
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 37000);
            }
            else if (roomNumber >= 11000 && roomNumber < 12000 && (archipelagoReceivedItems?.Contains((int)APItemID.FILLER.METAL_PREHISTORIC) ?? false)) // Prehistoric
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 11000);
            }
        }

        // Remove captured Ixupi, this needs to be called or else ixupi can get stuck in the game
        ArchipelagoRemoveCapturedIxupi();
    }

    private void OutsideAccess()
    {
        int generatorByte = ReadMemory(361, 1);
        if (!archipelagoGeneratorSwitchOn && IsKthBitSet(generatorByte, 5)) // Check if switch is pulled, if so set flag
        {
            archipelagoGeneratorSwitchOn = true;
        }

        if (roomNumber == 2000 && archipelagoGeneratorSwitchOn) // Check if looking at door and switch flag is true
        {
            if (!archipelagoGeneratorSwitchScreenRefreshed) // If screen hasn't already been refreshed
            {
                WriteMemory(361, SetKthBit(generatorByte, 5, false)); // Turn off switch in memory
                
                WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 990); // Refresh screen
                archipelagoGeneratorSwitchScreenRefreshed = true; // Set refresh flag
            }
        }
        else if (roomNumber != 2000 && archipelagoGeneratorSwitchOn) // If not looking at door and switch flag set
        {
            WriteMemory(361, SetKthBit(generatorByte, 5, true)); // Turn switch byte back on in memory
            archipelagoGeneratorSwitchScreenRefreshed = false; // Turn off refresh flag
        }
    }

    private void FlashTravel()
    {
        SetKthBitMemoryOneByte(Flags.FlashbackMenuActive, false);
    }

    private void ArchipelagoRemoveCapturedIxupi()
    {
        int ixupiCaptured = ReadMemory(-60, 2);

        // Remove captured ixupi from game
        if (IsKthBitSet(ixupiCaptured, 0)) // Sand Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.SAND, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 1)) // Crystal Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CRYSTAL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 2)) // Metal Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.METAL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 3)) // Oil Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 4)) // Wood Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WOOD, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 5)) // Lightning Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.LIGHTNING, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 6)) // Ash Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.ASH, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 7)) // Water Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WATER, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 8)) // Cloth Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CLOTH, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 9)) // Wax Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WAX, 0);
        }
    }

    private void ArchipelagoSetFlagBit(int offset, int bitNumber)
    {
        int tempValue = ReadMemory(offset, 1);
        tempValue = SetKthBit(tempValue, bitNumber, true);
        WriteMemory(offset, tempValue);
    }
    private void ArchipelagoSetFlagBit(FlagBit flag)
    {
        ArchipelagoSetFlagBit(flag.Offset, flag.Bit);
    }

    private void ArchipelagoLoadFlags()
    {
        // Get checked locations list
        List<long> LocationsChecked = archipelago_Client?.GetLocationsCheckedArchipelagoServer() ?? new();
        ArchipelagoDataStorage? dataStorage = archipelago_Client?.dataStorage;

        int ixupiCaptured = 0;
        int ixupiCapturedAmount = 0;

        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 114)) // Puzzle Solved Combination Lock +174 Bit 2
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.CombinationLock == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedComboLock);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID)) // Puzzle Solved Gears +169 Bit 8
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Gears == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedGears);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 1)) // Puzzle Solved Stonehenge +169 Bit 7
        {                                                             // Generator Switch on +169 Bit 6
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Stonehenge == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedStonehenge); // Stonehenge Solved
                ArchipelagoSetFlagBit(Flags.GeneratorSwitch); // Generator Switch
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 2)) // Puzzle Solved Workshop Drawers +179 Bit 8
        {                                                             // Drawer Open +168 Bit 8  
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.WorkshopDrawers == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedWorkshopDrawers); // Puzzle Solved
                ArchipelagoSetFlagBit(Flags.WorkshopDrawersOpen); // Drawer Open
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 3)) // Puzzle Solved Library Statue +170 Bit 8
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.LibraryStatue == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedLibraryStatue);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 4)) // Puzzle Solved Theater Door +16C Bit 4
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.TheaterDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedTheaterDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 5)) // Puzzle Solved Clock Tower Door +16C Bit 2
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ClockTowerDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedClocktowerDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 6)) // Puzzle Solved Clock Chains +17C Bit 6
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ClockChains == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedClockChains);  // Puzzle Solved
                WriteMemoryTwoBytes(1708, 530); // Set clock tower time
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 7)) // Puzzle Solved Atlantis +168 Bit 6
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Atlantis == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedAtlantis);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 8)) // Puzzle Solved Organ +168 Bit 7
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Organ == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedOrgan);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 9)) // Puzzle Solved Maze Door +16C Bit 1
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.MazeDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedMazeDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 10)) // Puzzle Solved Columns of RA +16D Bit 7
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ColumnsOfRA == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedColumnsOfRA);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 11)) // Puzzle Solved Burial Door +16D Bit 6
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.BurialDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedBurialDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 12)) // Puzzle Solved Chinese Solitaire +17D Bit 5
        {                                                              // Drawer open +16D Bit 3
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ChineseSolitaire == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedChineseSolitaire); // Puzzle Solved
                ArchipelagoSetFlagBit(Flags.ChineseSolitaireDrawersOpen); // Drawer Open
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 13)) // Puzzle Solved Shaman Drums +16D Bit 2
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ShamanDrums == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedShamanDrums);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 14)) // Puzzle Solved Lyre +16D Bit 1
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Lyre == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedLyre);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 15)) // Puzzle Solved Red Door +16C Bit 8
        {
            if (archipelagoCollectBehavior == CollectBehavior.SOLVE_ALL ||
                dataStorage?.PuzzlesSolved.RedDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedRedDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 16)) // Puzzle Solved Fortune Teller Door +16C Bit 6
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.FortuneTellerDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedFortuneTellerDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 17)) // Puzzle Solved Alchemy +174 Bit 6
        {                                                              // Box Opened +17D Bit 3
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Alchemy == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedAlchemy); // Puzzle Solved
                ArchipelagoSetFlagBit(Flags.AlchemyShippingBoxOpen); // Box Open
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 18)) // Puzzle Solved UFO Symbols +179 Bit 4
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.UFOSymbols == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedUFO);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 19)) // Puzzle Solved Anansi Music Box +17C Bit 8
        {                                                              // Song set on jukebox +179 Bit 6
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.AnansiMusicBoc == true)
            {
                ArchipelagoSetFlagBit(Flags.AnansiMusicBoxOpen); // Music Box Open
                ArchipelagoSetFlagBit(Flags.JukeboxSet); // Jukebox Set
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 20)) // Puzzle Solved Gallows +17D Bit 7
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Gallows == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedGallows);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 21)) // Puzzle Solved Mastermind +179 Bit 7
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.Mastermind == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedMasermind);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 22)) // Puzzle Solved Marble Pinball +168 Bit 5
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.MarblePinball == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedMarblePinball);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 23)) // Puzzle Solved Skull Dial Door +178 Bit 2
        {
            if (archipelagoCollectBehavior == CollectBehavior.SOLVE_ALL ||
                dataStorage?.PuzzlesSolved.SkullDialDoor == true)
            {
                ArchipelagoSetFlagBit(Flags.PuzzleSolvedSkullDialDoor);
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 110)) // Puzzle Solved Office Elevator
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.OfficeElevator == true)
            {
                elevatorOfficeSolved = true;
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 111)) // Puzzle Solved Bedroom Elevator
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.BedroomElevator == true)
            {
                elevatorBedroomSolved = true;
            }
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 112)) // Puzzle Solved Three Floor Elevator
        {
            if (archipelagoCollectBehavior != CollectBehavior.SOLVE_NONE ||
                dataStorage?.PuzzlesSolved.ThreeFloorElevator == true)
            {
                elevatorThreeFloorSolved = true;
            }
        }

        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 24)) // Flashback Memory Obtained Beth's Ghost +16C Bit 3
        {
            ArchipelagoSetFlagBit(Flags.FlashbackBethGhost);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 25)) // Flashback Memory Obtained Merrick's Ghost +16C Bit 5
        {
            ArchipelagoSetFlagBit(Flags.FlashbackMerrickGhost);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 26)) // Flashback Memory Obtained Windlenot's Ghost +169 Bit 3
        {
            ArchipelagoSetFlagBit(Flags.FlashbackWindlenotGhost);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 27)) // Flashback Memory Obtained Ancient Astrology +170 Bit 2
        {
            ArchipelagoSetFlagBit(Flags.FlashbackAncientAstrology);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 28)) // Flashback Memory Obtained Scrapbook +170 Bit 1
        {
            ArchipelagoSetFlagBit(Flags.FlashbackScrapbook);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 29)) // Flashback Memory Obtained Museum Brochure +175 Bit 8
        {
            ArchipelagoSetFlagBit(Flags.FlashbackMuseumBrochure);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 30)) // Flashback Memory Obtained In Search of the Unexplained +178 Bit 6
        {
            ArchipelagoSetFlagBit(Flags.FlashbackInSearchOfTheUnexplained);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 31)) // Flashback Memory Obtained Egyptian Hieroglyphics Explained +169 Bit 4
        {
            ArchipelagoSetFlagBit(Flags.FlashbackEgyptionHieroglyphics);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 32)) // Flashback Memory Obtained South American Pictographs +175 Bit 7
        {
            ArchipelagoSetFlagBit(Flags.FlashbackSouthAmericanPictographs);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 33)) // Flashback Memory Obtained Mythology of the Stars +175 Bit 6
        {
            ArchipelagoSetFlagBit(Flags.FlashbackMythologyOfTheStars);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 34)) // Flashback Memory Obtained Black Book +175 Bit 5
        {
            ArchipelagoSetFlagBit(Flags.FlashbackBlackBook);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 35)) // Flashback Memory Obtained Theater Movie +175 Bit 4
        {                                                              // Theater Curtain Open flag +168 Bit 3
            ArchipelagoSetFlagBit(Flags.FlashbackTheaterMovie); // Flashback
            ArchipelagoSetFlagBit(Flags.TheaterCurtainOpen); // Curtain Open
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 36)) // Flashback Memory Obtained Museum Blueprints +175 Bit 3
        {
            ArchipelagoSetFlagBit(Flags.FlashbackMuseumBlueprints);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 37)) // Flashback Memory Obtained Beth's Address Book +175 Bit 2
        {
            ArchipelagoSetFlagBit(Flags.FlashbackBethAddressBook);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 38)) // Flashback Memory Obtained Merrick's Notebook +175 Bit 1
        {
            ArchipelagoSetFlagBit(Flags.FlashbackMerrickNotebook);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 39)) // Flashback Memory Obtained Professor Windlenot's Diary +174 Bit 8
        {
            ArchipelagoSetFlagBit(Flags.FlashbackWindlenotDiary);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 49)) // Final Riddle: Final Riddle: Fortune Teller +179 Bit 3
        {
            ArchipelagoSetFlagBit(Flags.FinalRiddleFortuneTeller);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 50)) // Final Riddle: Final Riddle: Planets Aligned +179 Bit 2
        {
            ArchipelagoSetFlagBit(Flags.FinalRiddlePlanetsAligned);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 52)) // Final Riddle: Final Riddle: Beth's Body Page 17 +178 Bit 8
        {
            ArchipelagoSetFlagBit(Flags.FinalRiddleBethBodyPage17);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 53)) // Final Riddle: Guillotine Dropped +178 Bit 7
        {
            ArchipelagoSetFlagBit(Flags.FinalRiddleGuillotine);
        }
        if (LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 113)) // Ixupi Captured Lightning
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 5, true);
            ixupiCapturedAmount += 1;
        }

        // Set a default floor number for three floor elevator to remove a crash should the player logout in the elevator
        WriteMemory(916, 1);

        // Early beth setting
        SetKthBitMemoryOneByte(Flags.BethElectricalPanelAvailable, archipelago_Client?.slotDataSettingEarlyBeth ?? true);

        // Check if slot has already goaled, if so prevent final cutscene from playing again
        if(archipelago_Client != null)
        {
            finalCutsceneTriggered = archipelago_Client.CheckSlotGoalStatus();
        }
    }

    private void ArchipelagoPlacePieces()
    {
        if (!archipelagoCurrentlyLoadingData)
        {
            new Thread(() =>
            {

                int ixupiCaptured = ReadMemory(-60, 2);

                for (int i = 0; i < APItemID.AP_POTS_COUNT; i++)
                {
                    if (archipelagoPiecePlaced[i] == false && (archipelagoReceivedItems?.Contains(ARCHIPELAGO_BASE_ITEM_ID + i) ?? true))
                    {
                        // Check if ixupi is captured, if so don't place it
                        if (!((i % 10 == 0) && IsKthBitSet(ixupiCaptured, 7)) && // Water isn't captured
                            !((i % 10 == 1) && IsKthBitSet(ixupiCaptured, 9)) && // Wax isn't captured
                            !((i % 10 == 2) && IsKthBitSet(ixupiCaptured, 6)) && // Ash isn't captured
                            !((i % 10 == 3) && IsKthBitSet(ixupiCaptured, 3)) && // Oil isn't captured
                            !((i % 10 == 4) && IsKthBitSet(ixupiCaptured, 8)) && // Cloth isn't captured
                            !((i % 10 == 5) && IsKthBitSet(ixupiCaptured, 4)) && // Wood isn't captured
                            !((i % 10 == 6) && IsKthBitSet(ixupiCaptured, 1)) && // Crystal isn't captured
                            !((i % 10 == 7) && IsKthBitSet(ixupiCaptured, 5)) && // Lightning isn't captured
                            !((i % 10 == 8) && IsKthBitSet(ixupiCaptured, 0)) && // Earth isn't captured
                            !((i % 10 == 9) && IsKthBitSet(ixupiCaptured, 2))    // Metal isn't captured
                        )
                        {
                            ArchipelagoFindWhereToPlace(200 + i);
                        }

                        archipelagoPiecePlaced[i] = true;
                    }
                }
            }).Start();
        }
    }

    private void ArchipelagoFindWhereToPlace(int piece)
    {
        string pieceName = "";
        // Determine which piece is being placed
        if (piece < 220 ||
            piece >= 220 && (archipelagoReceivedItems?.Contains(ARCHIPELAGO_BASE_ITEM_ID + piece - 200) ?? false))
        {
            pieceName = ConvertPotNumberToString(piece) ?? "";
        }
        else
        {
            // Completed pot piece isn't received pot piece, then just use the top piece
            pieceName = ConvertPotNumberToString(piece - 10) ?? "";
        }

        string locationName = "";
        if (archipelago_Client != null)
        {
            // Figure out the matching Location
            foreach (var storage in archipelago_Client.storagePlacementsDict)
            {
                if (storage.Value == pieceName)
                {
                    locationName = storage.Key;
                }
            }
        }

        // Now that we have the location name, turn that into location value
        int locationValue = locationName switch
        {
            string name when TryParseEnumMember<PotLocation>(name, out var location) => (int)location,
            "Anansi" or "Anansi Music Box" => 16,
            _ => 0
        };

        // Place piece
        // First check if there a piece already located in the location. If so place the piece instead in its location
        if (ReadMemory(locationValue * 8, 1) == 0) // Not taken, place piece
        {
            WriteMemory(locationValue * 8, piece);
        }
        else // Taken
        {
            int pieceAlreadyHere = ReadMemory(locationValue * 8, 1);
            WriteMemory(locationValue * 8, piece);
            ArchipelagoFindWhereToPlace(pieceAlreadyHere);
        }
    }

    private void ArchipelagoStageChecks()
    {
        // Get checked locations list
        List<long> LocationsChecked = archipelago_Client?.GetLocationsCheckedArchipelagoServer() ?? new();

        // Checks based on flag memory
        foreach (var tuple in flagMemoryList)
        {
            int archipelagoID = ARCHIPELAGO_BASE_LOCATION_ID + tuple.Item1;
            int MemoryOffset = tuple.Item2.Offset;
            int Bit = tuple.Item2.Bit;
            int Size = tuple.Item3;

            if (!LocationsChecked.Contains(archipelagoID) && IsKthBitSet(ReadMemory(MemoryOffset, Size), Bit))
            {
                if (!archipelagoChecksReadyToSend.Contains(archipelagoID))
                {
                    archipelagoChecksReadyToSend.Add(archipelagoID);
                }
            }
        }

        // Checks based on score points memory
        foreach (var tuple in pointsMemoryList)
        {
            int archipelagoID = ARCHIPELAGO_BASE_LOCATION_ID + tuple.Item1;
            int MemoryOffset = tuple.Item2;

            if (!LocationsChecked.Contains(archipelagoID) && ReadMemory(MemoryOffset, 2) == 0)
            {
                if (!archipelagoChecksReadyToSend.Contains(archipelagoID))
                {
                    archipelagoChecksReadyToSend.Add(archipelagoID);
                }
            }
        }

        // Checks based on information plaque memory
        foreach (var tuple in informationPlaqueMemoryList)
        {
            int archipelagoID = ARCHIPELAGO_BASE_LOCATION_ID + tuple.Item1;
            int plaqueMemoryOffset = tuple.Item2;

            if (!LocationsChecked.Contains(archipelagoID) && ReadMemory(plaqueMemoryOffset, 2) == 0)
            {
                if (!archipelagoChecksReadyToSend.Contains(archipelagoID))
                {
                    archipelagoChecksReadyToSend.Add(archipelagoID);
                }
            }
        }

        // Checks based on room number
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 51) && archipelagoCheckStoneTablet) // Final Riddle: Norse God Stone Message, no bit so if on the screen send the check
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 51))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 51);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 61) && archipelagoCheckBasilisk) // Puzzle Hint Found: Basilisk Bone Fragments
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 61))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 61);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 63) && archipelagoCheckSirenSong) // Puzzle Hint Found: Sirens Song Heard
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 63))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 63);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 64) && archipelagoCheckEgyptianSphinx) // Puzzle Hint Found: Egyptian Sphinx Heard
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 64))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 64);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 65) && archipelagoCheckGallowsPlaque) // Puzzle Hint Found: Gallows Information Plaque
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 65))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 65);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 67) && archipelagoCheckGeoffreyWriting) // Puzzle Hint Found: Geoffrey Elevator Writing
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 67))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 67);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 109) && archipelagoCheckPlaqueUFO) // Information Plaque: (UFO) Aliens
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 109))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 109);
            }
        }

        // Checks based on variables
        
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 110) && elevatorOfficeSolved && archipelagoElevatorSettings) // Puzzle Solved Office Elevator
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 110))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 110);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 111) && elevatorBedroomSolved && archipelagoElevatorSettings) // Puzzle Solved Bedroom Elevator
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 111))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 111);
            }
        }
        if (!LocationsChecked.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 112) && elevatorThreeFloorSolved && archipelagoElevatorSettings) // Puzzle Solved Three Floor Elevator
        {
            if (!archipelagoChecksReadyToSend.Contains(ARCHIPELAGO_BASE_LOCATION_ID + 112))
            {
                archipelagoChecksReadyToSend.Add(ARCHIPELAGO_BASE_LOCATION_ID + 112);
            }
        }
        
    }

    static readonly List<(int, FlagBit, int)> flagMemoryList = new() // (Archipelago ID, Memory offset, Bit, Size)
    {
        (0, Flags.PuzzleSolvedGears, 1), // Puzzle Solved Gears +169 Bit 8
        (1, Flags.PuzzleSolvedStonehenge, 1), // Puzzle Solved Stonehenge +169 Bit 7
        (2, Flags.PuzzleSolvedWorkshopDrawers, 1), // Puzzle Solved Workshop Drawers +179 Bit 8
        (3, Flags.PuzzleSolvedLibraryStatue, 1), // Puzzle Solved Library Statue +170 Bit 8
        (4, Flags.PuzzleSolvedTheaterDoor, 1), // Puzzle Solved Theater Door +16C Bit 4
        (5, Flags.PuzzleSolvedClocktowerDoor, 1), // Puzzle Solved Clock Tower Door +16C Bit 2
        (6, Flags.PuzzleSolvedClockChains, 1), // Puzzle Solved Clock Chains +17C Bit 6
        (7, Flags.PuzzleSolvedAtlantis, 1), // Puzzle Solved Atlantis +168 Bit 6
        (8, Flags.PuzzleSolvedOrgan, 1), // Puzzle Solved Organ +168 Bit 7
        (9, Flags.PuzzleSolvedMazeDoor, 1), // Puzzle Solved Maze Door +16C Bit 1
        (10, Flags.PuzzleSolvedColumnsOfRA, 1), // Puzzle Solved Columns of RA +16D Bit 7
        (11, Flags.PuzzleSolvedBurialDoor, 1), // Puzzle Solved Burial Door +16D Bit 
        (12, Flags.PuzzleSolvedChineseSolitaire, 1), // Puzzle Solved Chinese Solitaire +17D Bit 5
        (13, Flags.PuzzleSolvedShamanDrums, 1), // Puzzle Solved Shaman Drums +16D Bit 2
        (14, Flags.PuzzleSolvedLyre, 1), // Puzzle Solved Lyre +16D Bit 1
        (15, Flags.PuzzleSolvedRedDoor, 1), // Puzzle Solved Red Door +16C Bit 8
        (16, Flags.PuzzleSolvedFortuneTellerDoor, 1), // Puzzle Solved Fortune Teller Door +16C Bit 6
        (17, Flags.PuzzleSolvedAlchemy, 1), // Puzzle Solved Alchemy +174 Bit 6
        (18, Flags.PuzzleSolvedUFO, 1), // Puzzle Solved UFO Symbols +179 Bit 4
        (19, Flags.AnansiMusicBoxOpen, 1), // Puzzle Solved Anansi Music Box +17C Bit 8
        (20, Flags.PuzzleSolvedGallows, 1), // Puzzle Solved Gallows +17D Bit 7
        (21, Flags.PuzzleSolvedMasermind, 1), // Puzzle Solved Mastermind +179 Bit 7
        (22, Flags.PuzzleSolvedMarblePinball, 1), // Puzzle Solved Marble Pinball +168 Bit 5
        (23, Flags.PuzzleSolvedSkullDialDoor, 1), // Puzzle Solved Skull Dial Door +178 Bit 2
        (24, Flags.FlashbackBethGhost, 1), // Flashback Memory Obtained Beth's Ghost +16C Bit 3
        (25, Flags.FlashbackMerrickGhost, 1), // Flashback Memory Obtained Merrick's Ghost +16C Bit 5
        (26, Flags.FlashbackWindlenotGhost, 1), // Flashback Memory Obtained Windlenot's Ghost +169 Bit 3
        (27, Flags.FlashbackAncientAstrology, 1), // Flashback Memory Obtained Ancient Astrology +170 Bit 2
        (28, Flags.FlashbackScrapbook, 1), // Flashback Memory Obtained Scrapbook +170 Bit 1
        (29, Flags.FlashbackMuseumBrochure, 1), // Flashback Memory Obtained Museum Brochure +175 Bit 8
        (30, Flags.FlashbackInSearchOfTheUnexplained, 1), // Flashback Memory Obtained In Search of the Unexplained +178 Bit 6
        (31, Flags.FlashbackEgyptionHieroglyphics, 1), // Flashback Memory Obtained Egyptian Hieroglyphics Explained +169 Bit 4
        (32, Flags.FlashbackSouthAmericanPictographs, 1), // Flashback Memory Obtained South American Pictographs +175 Bit 7
        (33, Flags.FlashbackMythologyOfTheStars, 1), // Flashback Memory Obtained Mythology of the Stars +175 Bit 6
        (34, Flags.FlashbackBlackBook, 1), // Flashback Memory Obtained Black Book +175 Bit 5
        (35, Flags.FlashbackTheaterMovie, 1), // Flashback Memory Obtained Theater Movie +175 Bit 4
        (36, Flags.FlashbackMuseumBlueprints, 1), // Flashback Memory Obtained Museum Blueprints +175 Bit 3
        (37, Flags.FlashbackBethAddressBook, 1), // Flashback Memory Obtained Beth's Address Book +175 Bit 2
        (38, Flags.FlashbackMerrickNotebook, 1), // Flashback Memory Obtained Merrick's Notebook +175 Bit 1
        (39, Flags.FlashbackWindlenotDiary, 1), // Flashback Memory Obtained Professor Windlenot's Diary +174 Bit 8
        (40, Flags.IxupiCapturedWater, 2), // Ixupi Captured Water -3B Bit 8
        (41, Flags.IxupiCapturedWax, 2), // Ixupi Captured Wax -3B Bit 10
        (42, Flags.IxupiCapturedAsh, 2), // Ixupi Captured Ash -3B Bit 7
        (43, Flags.IxupiCapturedOil, 2), // Ixupi Captured Oil -3B Bit 4
        (44, Flags.IxupiCapturedCloth, 2), // Ixupi Captured Cloth -3B Bit 9
        (45, Flags.IxupiCapturedWood, 2), // Ixupi Captured Wood -3B Bit 5
        (46, Flags.IxupiCapturedCrystal, 2), // Ixupi Captured Crystal -3B Bit 2
        (47, Flags.IxupiCapturedSand, 2), // Ixupi Captured Sand -3B Bit 1
        (48, Flags.IxupiCapturedMetal, 2), // Ixupi Captured Metal -3B Bit 3
        (49, Flags.FinalRiddleFortuneTeller, 1), // Final Riddle: Fortune Teller +179 Bit 3
        (50, Flags.FinalRiddlePlanetsAligned, 1), // Final Riddle: Planets Aligned +179 Bit 2
        (52, Flags.FinalRiddleBethBodyPage17, 1), // Final Riddle: Beth's Body Page 17 +178 Bit 8
        (53, Flags.FinalRiddleGuillotine, 1), // Final Riddle: Guillotine Dropped +178 Bit 7
        (54, Flags.MailboxOpen, 1), // Puzzle Hint Found: Mailbox +171 Bit 7
        (113, Flags.IxupiCapturedLightning, 2), // Ixupi Captured Lightning -3B Bit 6
        (114, Flags.PuzzleSolvedComboLock, 1), // Puzzle Solved Combination Lock +174 Bit 2
    };

    static readonly List<(int, int)> pointsMemoryList = new() // (Archipelago ID, Memory offset)
    {
        (55, 1244), // Puzzle Hint Found: Orange Symbol +4DC
        (56, 1248), // Puzzle Hint Found: Silver Symbol +4E0
        (57, 1252), // Puzzle Hint Found: Green Symbol +4E4
        (58, 1256), // Puzzle Hint Found: White Symbol +4E8
        (59, 1260), // Puzzle Hint Found: Brown Symbol +4EC
        (60, 1264), // Puzzle Hint Found: Tan Symbol +4F0
        (62, 1276), // Puzzle Hint Found: Atlantis Map +4FC
        (66, 1176), // Puzzle Hint Found: Mastermind Information Plaque
        (68, 1384), // Puzzle Hint Found: Shaman Security Camera +568
        (69, 1500), // Puzzle Hint Found: Tape Recorder Heard +5DC
        (115, 1340), // Puzzle Hint Found: Beth's Note +53C
    };

    static readonly List<(int, int)> informationPlaqueMemoryList = new() // (Archipelago ID, Memory offset,)
    {
        (70, Points.InformationPlaque.TRANSFORMING_MASKS),                             //Lobby
        (71, Points.InformationPlaque.JADE_SKULL),                                     //Lobby
        (72, Points.InformationPlaque.BRONZE_UNICORN),                                 //Prehistoric
        (73, Points.InformationPlaque.GRIFFIN),                                        //Prehistoric
        (74, Points.InformationPlaque.EAGLES_NEST),                                    //Prehistoric
        (75, Points.InformationPlaque.LARGE_SPIDER),                                   //Prehistoric
        (76, Points.InformationPlaque.STARFISH),                                       //Prehistoric
        (77, Points.InformationPlaque.QUARTZ_CRYSTAL),                                 //Ocean
        (78, Points.InformationPlaque.POSEIDON),                                       //Ocean
        (79, Points.InformationPlaque.COLOSSUS_OF_RHODES),                             //Ocean
        (80, Points.InformationPlaque.POSEIDONS_TEMPLE),                               //Ocean
        (81, Points.InformationPlaque.SUBTERRANEON_WORLD),                             //Underground Maze)
        (82, Points.InformationPlaque.DERO),                                           //Underground Maze)
        (83, Points.InformationPlaque.TOMB_OF_THE_IXUPI),                              //Egypt
        (84, Points.InformationPlaque.THE_SPHINX),                                     //Egypt
        (85, Points.InformationPlaque.CURSE_OF_ANUBIS),                                //Egypt
        (86, Points.InformationPlaque.NORSE_BURIAL_SHIP),                              //Burial
        (87, Points.InformationPlaque.PARACAS_BURIAL_BUNDLES),                         //Burial
        (88, Points.InformationPlaque.SPECTACULAR_COFFINS_OF_GHANA),                   //Burial
        (89, Points.InformationPlaque.CREMATION),                                      //Burial
        (90, Points.InformationPlaque.ANIMAL_CREMATORIUM),                             //Burial
        (91, Points.InformationPlaque.SWITCH_DOCTORS_OF_THE_CONGO),                    //Shaman
        (92, Points.InformationPlaque.SAROMBE_DOCTOR_OF_MOZAMBIQUE),                   //Shaman
        (93, Points.InformationPlaque.FISHERMANS_CANOE_GOD),                           //Gods
        (94, Points.InformationPlaque.MAYAN_GODS),                                     //Gods
        (95, Points.InformationPlaque.THOR),                                           //Gods
        (96, Points.InformationPlaque.CELTIC_JANUS_SCULPTURE),                         //Gods
        (97, Points.InformationPlaque.SUMERIAN_BULL_GOD_AN),                           //Gods
        (98, Points.InformationPlaque.SUMERIAN_LYRE),                                  //Gods
        (99, Points.InformationPlaque.CHUEN),                                          //Gods
        (100, Points.InformationPlaque.AFRICAN_CREATION_MYTH),                         //Anansi 
        (101, Points.InformationPlaque.APOPHIS_THE_SERPENT),                           //Anansi 
        (102, Points.InformationPlaque.DEATH),                                         //Anansi
        (103, Points.InformationPlaque.CYCLOPS),                                       //Pegasus
        (104, Points.InformationPlaque.LYCANTHROPY),                                   //Werewolf
        (105, Points.InformationPlaque.COINCIDENCE_OR_EXTRATERRESTRIAL_VISITS),        //UFO
        (106, Points.InformationPlaque.PLANETS),                                       //UFO
        (107, Points.InformationPlaque.ASTRONOMICAL_CONTRUCTION),                      //UFO
        (108, Points.InformationPlaque.GUILLOTINE)                                     //Torture
    };

    private void ArchipelagoSendChecks()
    {
        // Check if shivers is still open
        CheckAttachState();

        // If shivers is still open proceed with the check
        if (shiversProcess != null)
        {
            foreach (int archipelagoID in new List<int>(archipelagoChecksReadyToSend))
            {
                archipelago_Client?.SendCheck(archipelagoID);
                archipelagoChecksReadyToSend.Remove(archipelagoID);
            }
        }
    }

    private void ArchipelagoModifyScripts()
    {
        if (scriptsLocated == false && processHandle != UIntPtr.Zero)
        {
            // Locate scripts
            LocateAllScripts();
        }

        if (scriptsLocated)
        {
            if (!scriptAlreadyModified)
            {
                if (roomNumber == 2330) // Underground Lake Room Door 
                {
                    ArchipelagoScriptRemoveCode(2330, 350, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM) ?? false);
                }
                if (roomNumber == 3010) // Underground Lake Room Door Boat Side
                {
                    ArchipelagoScriptRemoveCode(3010, 321, 142, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM) ?? false);
                }
                else if (roomNumber == 4630) // Office Elevator bottom
                {
                    ArchipelagoScriptRemoveCode(4630, 160, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR) ?? false);
                }
                else if (roomNumber == 6300) // Office Elevator top
                {
                    ArchipelagoScriptRemoveCode(6300, 226, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR) ?? false);
                }
                else if (roomNumber == 6030) // Office door and crawl space to bedroom elevator
                {
                    bool flag6030 = archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE) ?? false; // Door
                    ArchipelagoScriptRemoveCode(6030, 626, 137, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 629, 142, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 632, 137, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 635, 42, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 637, 197, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 640, 42, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 642, 197, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 645, 143, flag6030);

                    ArchipelagoScriptRemoveCode(6030, 609, 142, archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false); // crawl space
                }
                else if (roomNumber == 6260) // Workshop Door
                {
                    ArchipelagoScriptRemoveCode(6260, 344, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.WORKSHOP) ?? false);
                }
                else if (roomNumber == 8030) // Library Door Library Side
                {
                    ArchipelagoScriptRemoveCode(8030, 326, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.LIBRARY) ?? false);
                }
                else if (roomNumber == 9010) // Office Door Lobby Side
                {
                    ArchipelagoScriptRemoveCode(9010, 207, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OFFICE) ?? false);
                }
                else if (roomNumber == 9470) // Library Door Lobby Side
                {
                    ArchipelagoScriptRemoveCode(9470, 408, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.LIBRARY) ?? false);
                }
                else if (roomNumber == 9570) // Egypt Door From Lobby Side
                {
                    ArchipelagoScriptRemoveCode(9570, 274, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.EGYPT) ?? false);
                }
                else if (roomNumber == 9630) // Tar River Crawl Space, Lobby side
                {
                    ArchipelagoScriptRemoveCode(9630, 200, 142, archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false);
                }
                else if (roomNumber == 15260) // Tar River Crawl Space, Tar River side
                {
                    bool flag15260 = archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false;
                    ArchipelagoScriptRemoveCode(15260, 92, 49, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 94, 142, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 97, 65, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 99, 135, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 102, 65, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 104, 30, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 106, 204, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 109, 30, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 111, 204, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 114, 135, flag15260);
                }
                else if (roomNumber == 20040) // Egypt Door From Egypt Side
                {
                    // Normal door method doesn't work, so polygon is set to 0 at all coordinates
                    bool flag9570 = archipelagoReceivedItems?.Contains((int)APItemID.KEYS.EGYPT) ?? false;
                    ArchipelagoScriptRemoveCode(20040, 468, 79, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 470, 18, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 472, 183, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 475, 18, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 477, 182, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 480, 134, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 488, 79, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 490, 19, flag9570);
                }
                else if (roomNumber == 9590) // Prehistoric Door
                {
                    ArchipelagoScriptRemoveCode(9590, 250, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PREHISTORIC) ?? false);
                }
                else if (roomNumber == 10101) // Three Floor Elevator - Blue Maze Bottom
                {
                    ArchipelagoScriptRemoveCode(10101, 160, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false);
                }
                else if (roomNumber == 10290) // Generator Door
                {
                    if (archipelagoReceivedItems?.Contains((int)APItemID.KEYS.GENERATOR) ?? false)
                    {
                        ArchipelagoScriptRemoveCode(10290, 152, 51, true);
                        ArchipelagoScriptRemoveCode(10290, 153, 56, true);
                    }
                    else
                    {
                        ArchipelagoScriptRemoveCode(10290, 152, 0, true);
                        ArchipelagoScriptRemoveCode(10290, 153, 72, true);
                    }
                }
                else if (roomNumber == 11120) // Ocean Door
                {
                    ArchipelagoScriptRemoveCode(11120, 374, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.OCEAN) ?? false);
                }
                else if (roomNumber == 11320) // Greenhouse Door
                {
                    ArchipelagoScriptRemoveCode(11320, 225, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.GREENHOUSE) ?? false);
                }

                else if (roomNumber == 18230) // Projector Room Door
                {
                    ArchipelagoScriptRemoveCode(18230, 126, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PROJECTOR) ?? false);
                }
                else if (roomNumber == 18240) // Theater Back Hallways Crawlspace
                {
                    ArchipelagoScriptRemoveCode(18240, 132, 142, archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false); // crawl space
                }
                else if (roomNumber == 20150) // Egypt Crawlspace from Egypt Side
                {
                    // polygon is set to 0 at all coordinates
                    bool flag20150 = archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false;
                    ArchipelagoScriptRemoveCode(20150, 158, 73, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 160, 51, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 162, 173, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 165, 51, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 167, 171, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 170, 125, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 172, 171, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 175, 126, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 177, 77, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 179, 126, flag20150);
                }
                else if (roomNumber == 23590) // Lyre easy
                {
                    int easierLyreCount = Math.Min(archipelagoReceivedItems?.Count(item => item == ((int)APItemID.FILLER.EASIER_LYRE)) ?? 0, 9);
                    ArchipelagoScriptRemoveCode(23590, 2158, 12 - easierLyreCount, true); // Works slightly different each round completed is +1 with a +2 offset. So 2 rounds required is 4.
                }
                else if (roomNumber == 27023) // Egypt Crawlspace from Blue Hallways Side
                {
                    // polygon is set to 0 at all coordinates
                    bool flag27023 = archipelagoReceivedItems?.Contains((int)APItemID.ABILITIES.CRAWLING) ?? false;
                    ArchipelagoScriptRemoveCode(27023, 138, 50, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 140, 21, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 142, 63, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 144, 132, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 147, 194, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 150, 135, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 153, 205, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 156, 20, flag27023);
                }
                else if (roomNumber == 21440) // Shaman Door
                {
                    // Normal door method doesn't work, so polygon is set to 0 at all coordinates
                    bool flag21440 = archipelagoReceivedItems?.Contains((int)APItemID.KEYS.SHAMAN) ?? false;
                    ArchipelagoScriptRemoveCode(21440, 335, 80, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 337, 16, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 339, 183, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 342, 16, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 344, 182, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 347, 136, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 350, 81, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 352, 136, flag21440);
                }
                else if (roomNumber == 26310) // Janitor Closet Door
                {
                    ArchipelagoScriptRemoveCode(26310, 230, 142, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.JANITOR_CLOSET) ?? false);
                }
                else if (roomNumber == 27211) // Three Floor Elevator - Blue Maze Bottom
                {
                    ArchipelagoScriptRemoveCode(27211, 160, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false);
                }
                else if (roomNumber == 29450) // UFO Door, UFO Side
                {
                    if (archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UFO) ?? false)
                    {
                        ArchipelagoScriptRemoveCode(29450, 122, 56, true);
                    }
                    else
                    {
                        ArchipelagoScriptRemoveCode(29450, 122, 72, true);
                    }
                }
                else if (roomNumber == 30010) // UFO Door, Inventions Side
                {
                    // Had issues modifying the script the normal way, so used the door open flag instead
                    int currentValue = ReadMemory(368, 1);
                    currentValue = SetKthBit(currentValue, 4, !archipelagoReceivedItems?.Contains((int)APItemID.KEYS.UFO) ?? true); // Set this to false when key obtained
                    WriteMemory(368, currentValue);
                    // Reload the screen
                    WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 990);

                    scriptAlreadyModified = true;
                    lastScriptModified = 30010;
                }
                else if (roomNumber == 30430) // Torture Room Door
                {
                    bool flag30430 = archipelagoReceivedItems?.Contains((int)APItemID.KEYS.TORTURE) ?? false;
                    ArchipelagoScriptRemoveCode(30430, 172, 97, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 174, 32, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 176, 162, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 179, 32, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 181, 162, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 184, 142, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 187, 96, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 189, 142, flag30430);
                }
                else if (roomNumber == 32450) // Puzzle Room Door
                {
                    ArchipelagoScriptRemoveCode(32450, 258, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.PUZZLE) ?? false);
                }
                else if (roomNumber == 33500) // Three Floor Elevator - Blue Maze Top
                {
                    ArchipelagoScriptRemoveCode(33500, 176, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false);
                    ArchipelagoScriptRemoveCode(33500, 190, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ?? false);
                }
                else if (roomNumber == 37300) // Bedroom Door
                {
                    ArchipelagoScriptRemoveCode(37300, 205, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.BEDROOM) ?? false);
                }
                else if (roomNumber == 38130) // Bedroom Elevator
                {
                    ArchipelagoScriptRemoveCode(38130, 160, 179, archipelagoReceivedItems?.Contains((int)APItemID.KEYS.BEDROOM_ELEVATOR) ?? false);
                }
            }
            else
            {
                if (roomNumber != lastScriptModified)
                {
                    scriptAlreadyModified = false;
                    lastScriptModified = -1;
                }
            }
        }
    }

    private void ArchipelagoScriptRemoveCode(int scriptNumber, int offset, int valueToWriteWhenPassable, bool keyOrCrawlingObtained)
    {
        // Grab the location script
        UIntPtr? loadedScriptAddress = LoadedScriptAddress(processHandle, scriptsFound, scriptNumber);

        if (loadedScriptAddress.HasValue)
        {
            // Write changes to the script
            if (keyOrCrawlingObtained)
            {
                WriteMemoryAnyAddress(loadedScriptAddress.Value, offset, valueToWriteWhenPassable); // b3, 179 in decimal
            }
            else
            {
                WriteMemoryAnyAddress(loadedScriptAddress.Value, offset, 0);
            }

            // Force a script reload by setting the previous room again
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, roomNumberPrevious);

            // Force a screen redraw as well to fix health meter every time we reach a door. This is a very band-aid fix, rather get a list of screens we are allowed to redraw on
            // and then force a redraw when health meter is adjusted
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 922);

            scriptAlreadyModified = true;
            lastScriptModified = scriptNumber;
        }
    }
    
    private void GetRoomNumber()
    {
        // Monitor Room Number
        if (MyAddress != UIntPtr.Zero && processHandle != UIntPtr.Zero) // Throws an exception if not checked in release mode.
        {
            int tempRoomNumber = ReadMemory(Addresses.PLAYER_LOCATION, 2);

            if (tempRoomNumber != roomNumber)
            {
                roomNumberPrevious = roomNumber;
                roomNumber = tempRoomNumber;
                liveSplit?.RoomChange(roomNumberPrevious, roomNumber);
                if (roomNumber == 922 && !(archipelago_Client?.IsConnected ?? false))
                {
                    numberIxupiCaptured = 0;
                }
            }
            Dispatcher.Invoke(() =>
            {
                mainWindow.label_roomPrev.Content = roomNumberPrevious;
                mainWindow.label_room.Content = roomNumber;
            });
        }
    }

    private void PotSyncRedraw()
    {
        // If looking at pot then set the previous room to the menu to force a screen redraw on the pot
        if (POT_ROOMS.Contains(roomNumber) || roomNumber == 24380 && IsKthBitSet(ReadMemory(380,1),8)) // Anansi and anansi is open
        {
            WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 990);
        }
    }

    private bool CheckScreenRedrawAllowed()
    {
        return REDRAW_ROOMS.Contains(roomNumber) || roomNumber == 24380 && IsKthBitSet(ReadMemory(380, 1), 8); // Anansi Music Box and Box is closed
    }

    private void CheckOil()
    {
        // Make sure if oil is captured it stays captured
        if (!currentlyTeleportingPlayer)
        {
            int ixupiCaptureRead = ReadMemory(-60, 2);
            int oilLocation = ReadMemory((int)IxupiLocationOffsets.OIL, 2);

            if (IsKthBitSet(ixupiCaptureRead, (int)Ixupi.OIL) && oilLocation != 0)
            {
                WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
            }
        }
    }

    private void RoomShuffle()
    {
        RoomTransition? transition = roomTransitions.FirstOrDefault(transition =>
            roomNumberPrevious == transition.From && roomNumber == transition.DefaultTo // && lastTransitionUsed != transition
        );

        // Fix Torture Room Door Bug
        FixTortureDoorBug();

        if (transition != null)
        {
            lastTransitionUsed = transition;

            if (transition.ElevatorFloor.HasValue)
            {
                WriteMemory(916, transition.ElevatorFloor.Value);
            }

            if (transition.DefaultTo != transition.NewTo)
            {
                currentlyTeleportingPlayer = true;

                // Respawn Ixupi
                RespawnIxupi(transition.NewTo);

                // Stop Audio to prevent soft locks
                StopAudio(transition.NewTo);

                currentlyTeleportingPlayer = false;
                roomNumber = transition.NewTo;
            }
        }
    }

    private void RespawnIxupi(int destinationRoom)
    {
        int rngRoll;

        if (destinationRoom is 9020 or 9450 or 9680 or 9600 or 9560 or 9620 or 25010) // Water Lobby/Toilet
        {
            if (ReadMemory((int)IxupiLocationOffsets.WATER, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WATER, 9000); // Fountain
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WATER, 25000); // Toilet
                }
            }
        }

        if (destinationRoom is 8000 or 8250 or 24750 or 24330) // Wax Library/Anansi
        {
            if (ReadMemory((int)IxupiLocationOffsets.WAX, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 8000); // Library
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 22000); // Shaman
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 24000); // Anansi
                }
            }
        }

        if (destinationRoom is 6400 or 6270 or 6020 or 38100) // Ash Office
        {
            if (ReadMemory((int)IxupiLocationOffsets.ASH, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.ASH, 6000); // Office
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.ASH, 21000); // Burial
                }
            }
        }

        if (destinationRoom is 11240 or 11100 or 11020) // Oil Prehistoric
        {
            if (ReadMemory((int)IxupiLocationOffsets.OIL, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.OIL, 11000); // Animals
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.OIL, 14000); // Tar River
                }
            }
        }

        if (destinationRoom is 7010 or 24280 or 24180) // Wood Workshop/Pegasus
        {
            if (ReadMemory((int)IxupiLocationOffsets.WOOD, 2) != 0)
            {
                rngRoll = rng.Next(0, 4);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 7000); // Workshop
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 23000); // Gods Room
                }
                else if (rngRoll == 2)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 24000); // Pegasus
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 36000); // Back Hallways
                }
            }
        }

        if (destinationRoom is 12230 or 12010) // Crystal Ocean
        {
            if (ReadMemory((int)IxupiLocationOffsets.CRYSTAL, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 9000); // Lobby
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 12000); // Ocean
                }
            }
        }

        if (destinationRoom is 12230 or 12010 or 19040) // Sand Ocean/Plants
        {
            if (ReadMemory((int)IxupiLocationOffsets.SAND, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.SAND, 12000); // Ocean
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.SAND, 19000); // Plants
                }
            }
        }

        if (destinationRoom is 17010 or 37010) // Metal Projector Room/Bedroom
        {
            if (ReadMemory((int)IxupiLocationOffsets.METAL, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 11000); // Prehistoric
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 17000); // Projector Room
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 37000); // Bedroom
                }
            }
        }
    }

    private void FixTortureDoorBug()
    {
        if (roomNumber == 32076 && roomNumberPrevious != 32076)
        {
            int currentValue = ReadMemory(368, 1);
            currentValue = SetKthBit(currentValue, 4, false);
            WriteMemory(368, currentValue);
        }
    }

    private void ElevatorSettings()
    {
        // Elevators Stay Solved
        if (settingsElevatorsStaySolved || archipelagoElevatorSettings)
        {
            // Check if an elevator has been solved
            if (ReadMemory(912, 1) != elevatorSolveCountPrevious)
            {
                // Determine which elevator was solved
                if (roomNumber == 6300 || roomNumber == 4630)
                {
                    elevatorOfficeSolved = true;
                }
                else if (roomNumber == 38130 || roomNumber == 37360)
                {
                    elevatorBedroomSolved = true;
                }
                else if (roomNumber == 10101 || roomNumber == 27211 || roomNumber == 33500)
                {
                    elevatorThreeFloorSolved = true;
                }
            }

            // Check if approaching an elevator and that elevator is solved, if so open the elevator and force a screen redraw
            // Check if elevator is already open or not
            int currentElevatorState = ReadMemory(361, 1);
            if (IsKthBitSet(currentElevatorState, 1) != true)
            {
                if (((roomNumber == 6290 || roomNumber == 4620) && elevatorOfficeSolved) ||
                    ((roomNumber == 38110 || roomNumber == 37330) && elevatorBedroomSolved) ||
                    ((roomNumber == 10100 || roomNumber == 27212 || roomNumber == 33140) && elevatorThreeFloorSolved))

                {
                    // Set Elevator Open Flag
                    // Set previous room to menu to force a redraw on elevator
                    currentElevatorState = SetKthBit(currentElevatorState, 1, true);
                    WriteMemory(361, currentElevatorState);
                    WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 990);
                }
            }
            else
            // If the elevator state is already open, check if its supposed to be. If not close it. This can happen when elevators are included in the room shuffle
            // As you don't step off the elevator in the normal spot, so the game doesn't auto close the elevator
            {
                if (((roomNumber == 6290 || roomNumber == 4620) && !elevatorOfficeSolved) ||
                    ((roomNumber == 38110 || roomNumber == 37330) && !elevatorBedroomSolved) ||
                    ((roomNumber == 10100 || roomNumber == 27212 || roomNumber == 33140) && !elevatorThreeFloorSolved))
                {
                    currentElevatorState = SetKthBit(currentElevatorState, 1, false);
                    WriteMemory(361, currentElevatorState);
                    WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 990);
                }
            }
        }

        // Only 4x4 elevators. Must place after elevators open flag
        if (settingsOnly4x4Elevators)
        {
            WriteMemory(912, 0);
        }

        elevatorSolveCountPrevious = ReadMemory(912, 1);
    }

    private void EarlyLightning()
    {
        // ------Basement------
        int lightningLocation = ReadMemory(236, 2);

        // If in basement and Lightning location isn't 0. (0 means he has been captured already)
        if (roomNumber == 39010 && lightningLocation != 0)
        {
            WriteMemory(236, 39000);
        }

        HasGameFinished();
    }

    private void HasGameFinished()
    {
        if (!finalCutsceneTriggered && numberIxupiCaptured >= 10)
        {
            // If moved properly to final cutscene, disable the trigger for final cutscene
            finalCutsceneTriggered = true;
            WriteMemory(Addresses.PLAYER_LOCATION, 935);
        }
    }

    public void AnywhereLightning()
    {
        // ------Lamp/Electric Chair------

        // Locate Scripts
        if (scriptsLocated == false && processHandle != UIntPtr.Zero)
        {
            // Locate scripts
            LocateAllScripts();
        }

        if (real925ScriptLocated != true && processHandle != UIntPtr.Zero)
        {
            // The pointer from the 925 pointer doesn't work, after an ixupi loads in a new pointer is created, this new one seems to work
            FindReal925Script();
        }
        else if (real925ScriptLocated == true && processHandle != UIntPtr.Zero)
        {
            // Allow lightning capturable in lamp and electric chair

            if (!scriptAlreadyModified)
            {
                if (roomNumber == 29190 || roomNumber == 32500) // Lamp or Electric Chair 
                {
                    ArchipelagoScriptRemoveCode(925000, 1246, 0, true);
                    ArchipelagoScriptRemoveCode(925000, 1247, 0, true);
                    ArchipelagoScriptRemoveCode(925000, 1254, 0, true);
                    ArchipelagoScriptRemoveCode(925000, 1255, 0, true);
                    scriptAlreadyModified = true;
                    lastScriptModified = roomNumber;
                }
            }
            else
            {
                if (roomNumber != lastScriptModified)
                {
                    scriptAlreadyModified = false;
                    lastScriptModified = -1;
                }
            }
        }
    }

    public void StopAudio(int destination)
    {
        const int WM_LBUTTON = 0x0201;

        int tempRoomNumber = 0;

        // Kill Tunnel Music
        int oilLocation = ReadMemory((int)IxupiLocationOffsets.OIL, 2); // Record where tar currently is
        WriteMemory((int)IxupiLocationOffsets.OIL, 11000); // Move Oil to Plants
        WriteMemory(Addresses.PLAYER_LOCATION, 11170); // Move Player to Plants
        WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 11180); // Set Player Previous Room to trigger oil nearby sound
        Thread.Sleep(30);
        WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, oilLocation);

        // Trigger Intro cutscene to stop audio
        while (tempRoomNumber != 930)
        {
            WriteMemory(Addresses.PLAYER_LOCATION, 930);
            Thread.Sleep(20);

            tempRoomNumber = ReadMemory(Addresses.PLAYER_LOCATION, 2);
        }

        // Set previous room so outside audio does not play at conclusion of cutscene
        WriteMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 922);

        // Force a mouse click to skip cutscene. Keep trying until it succeeds.
        int sleepTimer = 10;
        while (tempRoomNumber == 930)
        {
            Thread.Sleep(sleepTimer);
            tempRoomNumber = ReadMemory(Addresses.PLAYER_LOCATION, 2);
            PostMessage((UIntPtr)(long)shiversProcess?.MainWindowHandle, WM_LBUTTON, 1, MakeLParam(580, 320));
            PostMessage((UIntPtr)(long)shiversProcess?.MainWindowHandle, WM_LBUTTON, 0, MakeLParam(580, 320));
            sleepTimer += 10; // Make sleep timer longer every attempt so the user doesn't get stuck in a soft lock
        }

        while (true)
        {
            WriteMemory(Addresses.PLAYER_LOCATION, destination);
            Thread.Sleep(10);
            tempRoomNumber = ReadMemory(Addresses.PLAYER_PREVIOUS_LOCATION, 2);
            if (tempRoomNumber == destination)
            {
                break;
            }
        }
    }

    private void VanillaPlacePiece(IxupiPot potPiece, Random rng)
    {
        PotLocation locationRand = POT_LOCATIONS[rng.Next(POT_LOCATIONS.Count)];
        while (true)
        {
            if (locationRand > PotLocation.CLOCK_TOWER)
            {
                locationRand = PotLocation.DESK_DRAWER;
            }

            // Check if piece is cloth and location is janitors closest
            if (locationRand == PotLocation.JANITOR_CLOSET &&
                (potPiece == IxupiPot.CLOTH_BOTTOM || potPiece == IxupiPot.CLOTH_TOP))
            {
                locationRand += 1;
                continue;
            }

            // Checking oil is in the bathroom or tar river
            if ((locationRand == PotLocation.TAR_RIVER || locationRand == PotLocation.JANITOR_CLOSET) &&
                (potPiece == IxupiPot.OIL_BOTTOM || potPiece == IxupiPot.OIL_TOP))
            {
                locationRand += 1;
                continue;
            }

            // For extra locations, is disabled in vanilla
            if (EXTRA_LOCATIONS.Contains(locationRand))
            {
                locationRand += 1;
                continue;
            }

            // Check if location is already filled
            if (Locations[(int)locationRand] != 0)
            {
                locationRand += 1;
                continue;
            }

            break;
        }
        Locations[(int)locationRand] = potPiece;
    }

    private void LoadInScriptList()
    {
        // Clear completeScriptList if not empty
        completeScriptList.Clear();

        // Load in the list of script numbers
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = "Shivers_Randomizer.resources.ScriptList.txt";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int number = int.Parse(line);
                completeScriptList.Add(number);
            }
        }

        archipelagoCompleteScriptList = completeScriptList.ToList(); // Used in archipelago to determine invalid room numbers loaded from data storage
    }

    private void LocateAllScripts()
    {
        // Load in a fresh set of scripts and clear out any old found scripts
        LoadInScriptList();
        scriptsFound.Clear();

        // Locate Scripts
        // This should find most of the scripts
        LocateScript(4280);
        LocateScript(9170);
        LocateScript(13349);
        LocateScript(31520);

        // If any left then search specifically
        int iterations = 0;
        while (completeScriptList.Count > 5 && iterations < 10)
        {
            // Select randomly so that its not always getting stuck not able to find the first script in the list
            int randomIndex = rng.Next(0, completeScriptList.Count);
            LocateScript(completeScriptList[randomIndex]);
            iterations += 1;
        }

        scriptsFound.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        scriptsLocated = true;
    }

    private void LocateScript(int scriptToFind)
    {
        // Signature to scan for
        // byte[] toFind = new byte[] { 0x73, 0x63, 0x72, 0x69, 0x70, 0x74, 0x2E, 0x33, 0x33, 0x32, 0x30, 0x30 };
        byte[] toFind = new byte[7 + scriptToFind.ToString().Length];
        toFind[0] = 0x73;// 'Script.'
        toFind[1] = 0x63;
        toFind[2] = 0x72;
        toFind[3] = 0x69;
        toFind[4] = 0x70;
        toFind[5] = 0x74;
        toFind[6] = 0x2E;


        for (int i = 0; i < scriptToFind.ToString().Length; i++)
        {
            toFind[i + 7] = (byte)scriptToFind.ToString()[i];
        }

        UIntPtr testAddress = AobScan2(processHandle, toFind);

        // Find start of memory block
        for (int i = 1; i < 20000; i++)
        {
            // Locate several FF in a row
            if (ReadMemoryAnyAddress(testAddress, i * -16, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 1, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 2, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 3, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 4, 1) == 255)
            {
                testAddress -= 16 * i;
                break;
            }
        }

        if (testAddress != UIntPtr.Zero)
        {
            char[] letters = new char[6];

            for (int i = 0; i < 2500; i++)
            {
                int result = 0;

                // There are other files in the memory blocks, scripts heaps vocab font palette message. If its script continue, if its not script increment i, 
                // if its nothing break since it must be the end of the memory block
                for (int j = 0; j < 6; j++)
                {
                    letters[j] = (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + j, 1);
                }

                if (letters[0] != 115 && letters[1] != 99 && letters[2] != 114 && letters[3] != 105 && letters[4] != 112 && letters[5] != 116) // Not Script
                {
                    if ((letters[0] != 112 && letters[1] != 105 && letters[2] != 99) &&// Not pic
                        (letters[0] != 104 && letters[1] != 101 && letters[2] != 97 && letters[3] != 112) && // Not heap
                        (letters[0] != 102 && letters[1] != 111 && letters[2] != 110 && letters[3] != 116) && // Not font
                        (letters[0] != 118 && letters[1] != 111 && letters[2] != 99 && letters[3] != 97 && letters[4] != 98) && // Not vocab
                        (letters[0] != 112 && letters[1] != 97 && letters[2] != 108 && letters[3] != 101 && letters[4] != 116 && letters[5] != 116) && // Not palette
                        (letters[0] != 109 && letters[1] != 101 && letters[2] != 115 && letters[3] != 115 && letters[4] != 97 && letters[5] != 103) // Not message
                        )
                    {
                        break;
                    }
                    continue;
                }

                // If it is a script, grab the script number
                char[] charArray2 = new char[] { (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 7, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 8, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 9, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 10, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 11, 1)};

                // Convert chars into ints
                foreach (char c in charArray2)
                {
                    if (c >= '0' && c <= '9') // Check if character is a numeric digit
                    {
                        int digitValue = c - '0'; // Convert character to int value
                        result = (result * 10) + digitValue; // Combine int values
                    }
                }

                // Add the script number and memory address to list
                // I cannot figure out why palettes are not getting caught in the filter above above, so remove them manually
                if (result != 409 && result != 999)
                {
                    scriptsFound.Add(Tuple.Create(result, testAddress + i * 128 + 80));

                    // Remove the found script from are full list
                    completeScriptList.Remove(result);
                }
            }
        }
    }

    private void FindReal925Script()
    {
        // Force the ixupi behavior script to load in
        WriteMemory((int)IxupiLocationOffsets.ASH, 6000); // Spawn ash in fireplace
        WriteMemory(Addresses.PLAYER_LOCATION, 6280); // Move to fireplace
        Thread.Sleep(30);
        WriteMemory(Addresses.PLAYER_LOCATION, roomNumber); // Move player back

        
        UIntPtr? tempAddress = LoadedScriptAddress(processHandle, scriptsFound, 925);

        // If the behavior script loaded in then that means the 2nd real ptr was created, locate it
        if (tempAddress != UIntPtr.Zero)
        {
            // Signature to scan for
            byte[] toFind = new byte[17];
            toFind[0] = 0x73;// 'Script.'
            toFind[1] = 0x63;
            toFind[2] = 0x72;
            toFind[3] = 0x69;
            toFind[4] = 0x70;
            toFind[5] = 0x74;
            toFind[6] = 0x2E;
            toFind[7] = 0x39;
            toFind[8] = 0x32;
            toFind[9] = 0x35;
            toFind[10] = 0x00;
            toFind[11] = 0xFF; // Wild card byte
            toFind[12] = 0xFF; // Wild card byte
            toFind[13] = 0xFF; // Wild card byte
            toFind[14] = 0x00;
            toFind[15] = 0x00;
            toFind[16] = 0x68;

            UIntPtr tempAddress2 = AobScanWithWildCard(processHandle, toFind);

            if (tempAddress2 != UIntPtr.Zero)
            {
                real925ScriptLocated = true;
                for (int i = 0; i < scriptsFound.Count; i++)
                {
                    if (scriptsFound[i].Item1 == 925)
                    {
                        // Tuple is immutable so just add another entry and use that instead
                        scriptsFound.Add(Tuple.Create(925000, tempAddress2));
                        break;
                    }
                }
            }
        }
    }

    public void WriteMemory(int offset, int value)
    {
        AppHelpers.WriteMemoryAnyAddress(processHandle, MyAddress, offset, value);
    }

    private void WriteMemoryTwoBytes(int offset, int value)
    {
        uint bytesWritten = 0;
        uint numberOfBytes = 2;

        WriteProcessMemory(processHandle, (ulong)(MyAddress + offset), BitConverter.GetBytes(value), numberOfBytes, ref bytesWritten);
    }

    private void WriteMemoryAnyAddress(UIntPtr anyAddress, int offset, int value)
    {
        AppHelpers.WriteMemoryAnyAddress(processHandle, anyAddress, offset, value);
    }

    public int ReadMemory(int offset, int numbBytesToRead)
    {
        return AppHelpers.ReadMemoryAnyAddress(processHandle, MyAddress, offset, numbBytesToRead);
    }

    private int ReadMemoryAnyAddress(UIntPtr anyAddress, int offset, int numbBytesToRead)
    {
        return AppHelpers.ReadMemoryAnyAddress(processHandle, anyAddress, offset, numbBytesToRead);
    }

    // Sets the kth bit on Memory with the specified offset. 0 indexed
    private void SetKthBitMemoryOneByte(int memoryOffset, int k, bool set)
    {
        WriteMemory(memoryOffset, SetKthBit(ReadMemory(memoryOffset, 1), k, set));
    }
    private void SetKthBitMemoryOneByte(FlagBit flag, bool set)
    {
        SetKthBitMemoryOneByte(flag.Offset, flag.Bit, set);
    }

    private void CheckAttachState()
    {
        // Check that the process is still Shivers, if so disconnect archipelago and livesplit
        Process tempProcess = Process.GetProcessById(shiversProcess?.Id ?? 0);

        if (shiversProcess != null && !tempProcess.MainWindowTitle.Contains("Shivers") && !tempProcess.MainWindowTitle.Contains("Status"))
        {
            archipelago_Client?.Close();
            liveSplit?.Disconnect();

            shiversProcess = null;
            processHandle = UIntPtr.Zero;
            MyAddress = UIntPtr.Zero;
            AddressLocated = false;
            mainWindow.button_Attach.IsEnabled = true;
            scriptsLocated = false;
            real925ScriptLocated = false;
            archipelagoChecksReadyToSend.Clear();
            archipelagoCompleteScriptList.Clear();
            completeScriptList.Clear();
            scriptsFound.Clear();
        }
    }
}

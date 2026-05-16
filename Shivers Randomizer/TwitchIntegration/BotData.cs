using Shivers_Randomizer.enums;
using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Shivers_Randomizer.TwitchIntegration;

public class TeleportLocation
{
    public string DisplayName { get; set; }
    public int LocationId { get; set; }
    public int Cost { get; set; }

    public TeleportLocation(string displayName, int locationId, int cost)
    {
        DisplayName = displayName;
        LocationId = locationId;
        Cost = cost;
    }
}
public struct PuzzleInfo
{
    internal MemoryMap.FlagBit Flag { get; }
    public int Cost { get; }

    internal PuzzleInfo(MemoryMap.FlagBit flag, int cost)
    {
        Flag = flag;
        Cost = cost;
    }
}

internal struct IxupiInfo
{
    internal MemoryMap.FlagBit Flag { get; }
    public IxupiPot PotBaseID { get; }
    public int Cost { get; }

    internal IxupiInfo(MemoryMap.FlagBit flag, IxupiPot potBaseID, int cost)
    {
        Flag = flag;
        PotBaseID = potBaseID;
        Cost = cost;
    }
}

public class PuzzleViewModel : INotifyPropertyChanged
{
    public string Name { get; set; }
    internal MemoryMap.FlagBit Flag { get; set; }

    private int cost;
    public int Cost
    {
        get => cost;
        set
        {
            if (cost != value)
            {
                cost = value;
                OnPropertyChanged(nameof(Cost));
                // update the BotData live
                BotData.PuzzleFlags[Name] = (Flag, cost);
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class IxupiViewModel : INotifyPropertyChanged
{
    public string Name { get; set; }
    internal IxupiInfo Info { get; set; }

    private int cost;
    public int Cost
    {
        get => cost;
        set
        {
            if (cost != value)
            {
                cost = value;
                OnPropertyChanged(nameof(Cost));
                // Update the BotData live
                BotData.IxupiFlags[Name] = new IxupiInfo(Info.Flag, Info.PotBaseID, cost);
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class BotData
{
    public static Dictionary<string, TeleportLocation> TeleportLocations { get; set; } = new Dictionary<string, TeleportLocation>(StringComparer.OrdinalIgnoreCase)
    {
        { "anansi", new TeleportLocation("Anansi", 24410, 50) },
        { "bedroom", new TeleportLocation("Bedroom", 37010, 150) },
        { "blue hallway", new TeleportLocation("Blue Hallway", 27070, 50) },
        { "burial", new TeleportLocation("Burial", 21190, 50) },
        { "clock tower", new TeleportLocation("Clock Tower", 35110, 70) },
        { "egypt", new TeleportLocation("Egypt", 20060, 40) },
        { "fortune teller", new TeleportLocation("Fortune Teller", 28000, 50) },
        { "generator", new TeleportLocation("Generator", 39010, 70) },
        { "gods", new TeleportLocation("Gods", 23800, 50) },
        { "greenhouse", new TeleportLocation("Greenhouse", 19040, 70) },
        { "inventions", new TeleportLocation("Inventions", 30170, 50) },
        { "janitor closet", new TeleportLocation("Janitor Closet", 25010, 50) },
        { "library", new TeleportLocation("Library", 8000, 50) },
        { "lobby", new TeleportLocation("Lobby", 9020, 30) },
        { "ocean", new TeleportLocation("Ocean", 12010, 70) },
        { "office", new TeleportLocation("Office", 6130, 50) },
        { "outside", new TeleportLocation("Outside", 1012, 150) },
        { "prehistoric", new TeleportLocation("Prehistoric", 11020, 50) },
        { "projector", new TeleportLocation("Projector", 17010, 60) },
        { "puzzle", new TeleportLocation("Puzzle", 31020, 70) },
        { "shaman", new TeleportLocation("Shaman", 22020, 50) },
        { "tar river", new TeleportLocation("Tar River", 13344, 100) },
        { "theater", new TeleportLocation("Theater", 16000, 50) },
        { "torture", new TeleportLocation("Torture", 32010, 50) },
        { "ufo", new TeleportLocation("UFO", 29010, 50) },
        { "underground lake", new TeleportLocation("Underground Lake", 3020, 140) },
        { "workshop", new TeleportLocation("Workshop", 7010, 60) }
    };

    internal static Dictionary<string, (MemoryMap.FlagBit Flag, int Cost)> PuzzleFlags { get; set; } = new Dictionary<string, (MemoryMap.FlagBit, int)>(StringComparer.OrdinalIgnoreCase)
{

        { "alchemy", (MemoryMap.Flags.PuzzleSolvedAlchemy, 50) },
        { "atlantis", (MemoryMap.Flags.PuzzleSolvedAtlantis, 20) },
        { "burial door", (MemoryMap.Flags.PuzzleSolvedBurialDoor, 30) },
        { "chinese solitaire", (MemoryMap.Flags.PuzzleSolvedChineseSolitaire, 50) },
        //{ "clock chains", (MemoryMap.Flags.PuzzleSolvedClockChains, 10) },
        { "clocktower door", (MemoryMap.Flags.PuzzleSolvedClocktowerDoor, 30) },
        { "columns of ra", (MemoryMap.Flags.PuzzleSolvedColumnsOfRA, 20) },
        { "combo lock", (MemoryMap.Flags.PuzzleSolvedComboLock, 20) },
        { "fortune teller door", (MemoryMap.Flags.PuzzleSolvedFortuneTellerDoor, 100) },
        { "gallows", (MemoryMap.Flags.PuzzleSolvedGallows, 20) },
        { "gears", (MemoryMap.Flags.PuzzleSolvedGears, 20) },
        //{ "library statue", (MemoryMap.Flags.PuzzleSolvedLibraryStatue, 10) },
        { "lyre", (MemoryMap.Flags.PuzzleSolvedLyre, 200) },
        { "marble pinball", (MemoryMap.Flags.PuzzleSolvedMarblePinball, 40) },
        { "mastermind", (MemoryMap.Flags.PuzzleSolvedMasermind, 150) },
        { "maze door", (MemoryMap.Flags.PuzzleSolvedMazeDoor, 20) },
        { "organ", (MemoryMap.Flags.PuzzleSolvedOrgan, 20) },
        { "red door", (MemoryMap.Flags.PuzzleSolvedRedDoor, 150) },
        { "shaman drums", (MemoryMap.Flags.PuzzleSolvedShamanDrums, 20) },
        { "stonehenge", (MemoryMap.Flags.PuzzleSolvedStonehenge, 20) },
        { "ufo", (MemoryMap.Flags.PuzzleSolvedUFO, 35) },
        { "theater door", (MemoryMap.Flags.PuzzleSolvedTheaterDoor, 40) },
        { "workshop drawers", (MemoryMap.Flags.PuzzleSolvedWorkshopDrawers, 20) }
};

    internal static Dictionary<string, IxupiInfo> IxupiFlags { get; set; } =
        new Dictionary<string, IxupiInfo>(StringComparer.OrdinalIgnoreCase)
        {
        { "sand", new IxupiInfo(MemoryMap.Flags.IxupiCapturedSand, IxupiPot.SAND_BOTTOM, 125) },
        { "crystal", new IxupiInfo(MemoryMap.Flags.IxupiCapturedCrystal, IxupiPot.CRYSTAL_BOTTOM, 125) },
        { "metal", new IxupiInfo(MemoryMap.Flags.IxupiCapturedMetal, IxupiPot.METAL_BOTTOM, 125) },
        { "oil", new IxupiInfo(MemoryMap.Flags.IxupiCapturedOil, IxupiPot.OIL_BOTTOM, 125) },
        { "wood", new IxupiInfo(MemoryMap.Flags.IxupiCapturedWood, IxupiPot.WOOD_BOTTOM, 125) },
        { "lightning", new IxupiInfo(MemoryMap.Flags.IxupiCapturedLightning, IxupiPot.LIGHTNING_BOTTOM, 125) },
        { "ash", new IxupiInfo(MemoryMap.Flags.IxupiCapturedAsh, IxupiPot.ASH_BOTTOM, 125) },
        { "water", new IxupiInfo(MemoryMap.Flags.IxupiCapturedWater, IxupiPot.WATER_BOTTOM, 125) },
        { "cloth", new IxupiInfo(MemoryMap.Flags.IxupiCapturedCloth, IxupiPot.CLOTH_BOTTOM, 125) },
        { "wax", new IxupiInfo(MemoryMap.Flags.IxupiCapturedWax, IxupiPot.WAX_BOTTOM, 125) }
        };


    internal static readonly Dictionary<string, int[]> IxupiLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "water", new[] { 9000, 25000 } },
        { "wax", new[] { 8000, 22000, 24000 } },
        { "ash", new[] { 6000, 21000 } },
        { "oil", new[] { 11000, 14000 } },
        { "cloth", new[] { 20000, 21000, 25000 } },
        { "wood", new[] { 7000, 23000, 24000, 36000 } },
        { "crystal", new[] { 9000, 12000 } },
        { "lightning", new[] { 29000, 32000, 39000 } },
        { "sand", new[] { 12000, 19000 } },
        { "metal", new[] { 11000, 17000, 37000 } }
    };

    public static readonly HashSet<int> BannedRooms = new HashSet<int>
    {
        // Room numbers where !teleport, !reset, !release are disabled
        1162, // Combo Lock
        1160, // Gears
        1214, // Stonehenge
        4630, // Office Elevator Bottom
        6300, // Office Elevator Top
        38130, // Bedroom Elevator Bottom
        37360, // Bedroom Elevator Top
        7111, // Workshop Drawers
        9691, // Theater Door
        18250, // Geoffrey
        40260, // Clocktower Chains
        12600, // Atlantis
        12410, // Organ
        13010, // Underground Maze Door
        20510, // Columns of Raa Right
        20610, // Columns of Raa Left
        20311, // Egypt Maze Door
        21071, // Chinese Solitaire
        22180, // Shaman Drums
        23590, // Lyre
        23601, // Red Door
        24440, // Anansi Key
        24380, // Anansi Box
        29045, // UFO
        27090, // Horse
        30421, // Alchemy
        32059, // Gallows
        31090, // Mastermind
        31270, // Pinball
        11330, // Skull Dial: Eagles Nest
        20190, // Skull Dial: Egypt
        21400, // Skull Dial: Burial
        23650, // Skull Dial: Gods
        24170, // Skull Dial: Werewolf
        14170, // Skull Dial: Tar River
    };


}


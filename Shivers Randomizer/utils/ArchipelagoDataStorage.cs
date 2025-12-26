using System.Collections.Generic;

namespace Shivers_Randomizer.utils;

public record ArchipelagoPuzzlesSolved
(
    bool CombinationLock = false,
    bool Gears = false,
    bool Stonehenge = false,
    bool WorkshopDrawers = false,
    bool LibraryStatue = false,
    bool TheaterDoor = false,
    bool ClockTowerDoor = false,
    bool ClockChains = false,
    bool Atlantis = false,
    bool Organ = false,
    bool MazeDoor = false,
    bool ColumnsOfRA = false,
    bool BurialDoor = false,
    bool ChineseSolitaire = false,
    bool ShamanDrums = false,
    bool Lyre = false,
    bool RedDoor = false,
    bool FortuneTellerDoor = false,
    bool Alchemy = false,
    bool UFOSymbols = false,
    bool AnansiMusicBoc = false,
    bool Gallows = false,
    bool Mastermind = false,
    bool MarblePinball = false,
    bool SkullDialDoor = false,
    bool OfficeElevator = false,
    bool BedroomElevator = false,
    bool ThreeFloorElevator = false
);

public record AddressedValue(int Location, int Value);

public record ArchipelagoDataStorage
(
    ArchipelagoPuzzlesSolved PuzzlesSolved,
    Dictionary<string, AddressedValue> SkullDials,
    Dictionary<string, AddressedValue> IxupiDamage,
    int PlayerLocation = 930,
    bool Jukebox = false,
    bool TarRiverShortcut = false,
    int Health = 100,
    int HealItemsReceived = 0,
    int IxupiCapturedStates = 0
);

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shivers_Randomizer.utils;

internal static class MemoryMap
{
    public static class Addresses
    {
        public const int PLAYER_PREVIOUS_LOCATION = -0x1B0;
        public const int PLAYER_LOCATION = -0x1A8;
        public const int IXUPI_CAPTURED_NUMBER = 0x6B0;
    }

    public readonly struct FlagBit
    {
        public int Offset { get; }
        public int Bit { get; }

        public FlagBit(int offset, int bit)
        {
            Offset = offset;
            Bit = bit;
        }
    }
    public static class Flags
    {
        public static readonly FlagBit IxupiCapturedSand = new FlagBit(offset: -60, bit: 0);
        public static readonly FlagBit IxupiCapturedCrystal = new FlagBit(offset: -60, bit: 1);
        public static readonly FlagBit IxupiCapturedMetal = new FlagBit(offset: -60, bit: 2);
        public static readonly FlagBit IxupiCapturedOil = new FlagBit(offset: -60, bit: 3);
        public static readonly FlagBit IxupiCapturedWood = new FlagBit(offset: -60, bit: 4);
        public static readonly FlagBit IxupiCapturedLightning = new FlagBit(offset: -60, bit: 5);
        public static readonly FlagBit IxupiCapturedAsh = new FlagBit(offset: -60, bit: 6);
        public static readonly FlagBit IxupiCapturedWater = new FlagBit(offset: -60, bit: 7);
        public static readonly FlagBit IxupiCapturedCloth = new FlagBit(offset: -60, bit: 8);
        public static readonly FlagBit IxupiCapturedWax = new FlagBit(offset: -60, bit: 9);


        //Bit 0?                                                                                                //FLAG 15
        public static readonly FlagBit ProjectorReelWound = new FlagBit(offset: 360, bit: 1);                   //FLAG 14
        public static readonly FlagBit TheaterCurtainOpen = new FlagBit(offset: 360, bit: 2);                   //FLAG 13
        //Bit 3?                                                                                                //FLAG 12
        public static readonly FlagBit PuzzleSolvedMarblePinball = new FlagBit(offset: 360, bit: 4);            //FLAG 11
        public static readonly FlagBit PuzzleSolvedAtlantis = new FlagBit(offset: 360, bit: 5);                 //FLAG 10
        public static readonly FlagBit PuzzleSolvedOrgan = new FlagBit(offset: 360, bit: 6);                    //FLAG 9
        public static readonly FlagBit WorkshopDrawersOpen = new FlagBit(offset: 360, bit: 7);                  //FLAG 8


        //Bit 0?                                                                                                //FLAG 7
        public static readonly FlagBit ElevatorDoorOpen = new FlagBit(offset: 361, bit: 1);                     //FLAG 6
        public static readonly FlagBit FlashbackWindlenotGhost = new FlagBit(offset: 361, bit: 2);              //FLAG 5
        public static readonly FlagBit FlashbackEgyptionHieroglyphics = new FlagBit(offset: 361, bit: 3);       //FLAG 4
        public static readonly FlagBit GeneratorPanelOpen = new FlagBit(offset: 361, bit: 4);                   //FLAG 3
        public static readonly FlagBit GeneratorSwitch = new FlagBit(offset: 361, bit: 5);                      //FLAG 2
        public static readonly FlagBit PuzzleSolvedStonehenge = new FlagBit(offset: 361, bit: 6);               //FLAG 1
        public static readonly FlagBit PuzzleSolvedGears = new FlagBit(offset: 361, bit: 7);                    //FLAG 0


        public static readonly FlagBit PuzzleSolvedMazeDoor = new FlagBit(offset: 364, bit: 0);                 //FLAG 31 
        public static readonly FlagBit PuzzleSolvedClocktowerDoor = new FlagBit(offset: 364, bit: 1);           //FLAG 30 
        public static readonly FlagBit FlashbackBethGhost = new FlagBit(offset: 364, bit: 2);                   //FLAG 29 
        public static readonly FlagBit PuzzleSolvedTheaterDoor = new FlagBit(offset: 364, bit: 3);              //FLAG 28 
        public static readonly FlagBit FlashbackMerrickGhost = new FlagBit(offset: 364, bit: 4);                //FLAG 27 
        public static readonly FlagBit PuzzleSolvedFortuneTellerDoor = new FlagBit(offset: 364, bit: 5);        //FLAG 26 
        public static readonly FlagBit JanitorClosetClothesMoved = new FlagBit(offset: 364, bit: 6);            //FLAG 25 
        public static readonly FlagBit PuzzleSolvedRedDoor = new FlagBit(offset: 364, bit: 7);                  //FLAG 24 


        public static readonly FlagBit PuzzleSolvedLyre = new FlagBit(offset: 365, bit: 0);                     //FLAG 23
        public static readonly FlagBit PuzzleSolvedShamanDrums = new FlagBit(offset: 365, bit: 1);              //FLAG 22
        public static readonly FlagBit ChineseSolitaireDrawersOpen = new FlagBit(offset: 365, bit: 2);          //FLAG 21
        //Bit 3?                                                                                                //FLAG 20
        //Bit 4?                                                                                                //FLAG 19
        public static readonly FlagBit PuzzleSolvedBurialDoor = new FlagBit(offset: 365, bit: 5);               //FLAG 18
        public static readonly FlagBit PuzzleSolvedColumnsOfRA = new FlagBit(offset: 365, bit: 6);              //FLAG 17
        public static readonly FlagBit EgyptSarcaphagusOpen = new FlagBit(offset: 365, bit: 7);                 //FLAG 16


        public static readonly FlagBit FlashbackScrapbook = new FlagBit(offset: 368, bit: 0);                   //FLAG 47
        public static readonly FlagBit FlashbackAncientAstrology = new FlagBit(offset: 368, bit: 1);            //FLAG 46
        public static readonly FlagBit FlashbackMenuActive = new FlagBit(offset: 368, bit: 2);                  //FLAG 45
        //Bit 3?                                                                                                //FLAG 44
        public static readonly FlagBit DoorOpen = new FlagBit(offset: 368, bit: 4);                             //FLAG 43
        public static readonly FlagBit FountainOn = new FlagBit(offset: 368, bit: 5);                           //FLAG 42
        public static readonly FlagBit LobbyTarRiverCrawlspaceAvailable = new FlagBit(offset: 368, bit: 6);     //FLAG 41
        public static readonly FlagBit PuzzleSolvedLibraryStatue = new FlagBit(offset: 368, bit: 7);            //FLAG 40


        public static readonly FlagBit SubtitlesOn = new FlagBit(offset: 369, bit: 0);                          //FLAG 39
        public static readonly FlagBit FullscreenModeOn = new FlagBit(offset: 369, bit: 1);                     //FLAG 38
        public static readonly FlagBit BoatControlsStonehengeSideEnabled = new FlagBit(offset: 369, bit: 2);    //FLAG 37
        public static readonly FlagBit BoatControlsMuseumSideEnabled = new FlagBit(offset: 369, bit: 3);        //FLAG 36
        //Bit 4                                                                                                 //FLAG 35
        public static readonly FlagBit LibraryLadderPosition = new FlagBit(offset: 369, bit: 5);                //FLAG 34
        public static readonly FlagBit MailboxOpen = new FlagBit(offset: 369, bit: 6);                          //FLAG 33
        //Bit 7?                                                                                                //FLAG 32


        public static readonly FlagBit ElectricChairOn = new FlagBit(offset: 372, bit: 0);                      //FLAG 63
        public static readonly FlagBit PuzzleSolvedComboLock = new FlagBit(offset: 372, bit: 1);                //FLAG 62
        public static readonly FlagBit PlanetsAligned = new FlagBit(offset: 372, bit: 2);                       //FLAG 61
        public static readonly FlagBit AutoSaveOFF = new FlagBit(offset: 372, bit: 3);                          //FLAG 60
        public static readonly FlagBit AshFirstRerollEncountered = new FlagBit(offset: 372, bit: 4);            //FLAG 59
        public static readonly FlagBit PuzzleSolvedAlchemy = new FlagBit(offset: 372, bit: 5);                  //FLAG 58
        public static readonly FlagBit FlashbackFinale = new FlagBit(offset: 372, bit: 6);                      //FLAG 57
        public static readonly FlagBit FlashbackWindlenotDiary = new FlagBit(offset: 372, bit: 7);              //FLAG 56


        public static readonly FlagBit FlashbackMerrickNotebook = new FlagBit(offset: 373, bit: 0);             //FLAG 55
        public static readonly FlagBit FlashbackBethAddressBook = new FlagBit(offset: 373, bit: 1);             //FLAG 54
        public static readonly FlagBit FlashbackMuseumBlueprints = new FlagBit(offset: 373, bit: 2);            //FLAG 53
        public static readonly FlagBit FlashbackTheaterMovie = new FlagBit(offset: 373, bit: 3);                //FLAG 52
        public static readonly FlagBit FlashbackBlackBook = new FlagBit(offset: 373, bit: 4);                   //FLAG 51
        public static readonly FlagBit FlashbackMythologyOfTheStars = new FlagBit(offset: 373, bit: 5);         //FLAG 50
        public static readonly FlagBit FlashbackSouthAmericanPictographs = new FlagBit(offset: 373, bit: 6);    //FLAG 49
        public static readonly FlagBit FlashbackMuseumBrochure = new FlagBit(offset: 373, bit: 7);              //FLAG 48


        public static readonly FlagBit LobbyPanelSlideAvailable = new FlagBit(offset: 376, bit: 0);             //FLAG 79
        public static readonly FlagBit PuzzleSolvedSkullDialDoor = new FlagBit(offset: 376, bit: 1);            //FLAG 78
        public static readonly FlagBit ProjectorPlayButtonAvailable = new FlagBit(offset: 376, bit: 2);         //FLAG 77
        public static readonly FlagBit TarRiverPointsAwarded = new FlagBit(offset: 376, bit: 3);                //FLAG 76
        public static readonly FlagBit InventionsEntranceWoodPanelRemoved = new FlagBit(offset: 376, bit: 4);   //FLAG 75
        public static readonly FlagBit FlashbackInSearchOfTheUnexplained = new FlagBit(offset: 376, bit: 5);    //FLAG 74
        public static readonly FlagBit FinalRiddleGuillotine = new FlagBit(offset: 376, bit: 6);                //FLAG 73
        public static readonly FlagBit FinalRiddleBethBodyPage17 = new FlagBit(offset: 376, bit: 7);            //FLAG 72


        public static readonly FlagBit DebugFlagAutoSolveMarblePinball = new FlagBit(offset: 377, bit: 0);      //FLAG 71
        public static readonly FlagBit FinalRiddlePlanetsAligned = new FlagBit(offset: 377, bit: 1);            //FLAG 70
        public static readonly FlagBit FinalRiddleFortuneTeller = new FlagBit(offset: 377, bit: 2);             //FLAG 69
        public static readonly FlagBit PuzzleSolvedUFO = new FlagBit(offset: 377, bit: 3);                      //FLAG 68
        public static readonly FlagBit AnansiSarcophagusOpen = new FlagBit(offset: 377, bit: 4);                //FLAG 67
        public static readonly FlagBit JukeboxSet = new FlagBit(offset: 377, bit: 5);                           //FLAG 66
        public static readonly FlagBit PuzzleSolvedMasermind = new FlagBit(offset: 377, bit: 6);                //FLAG 65
        public static readonly FlagBit PuzzleSolvedWorkshopDrawers = new FlagBit(offset: 377, bit: 7);          //FLAG 64


        //Bit 0? Unused?
        //Bit 1? Unused?
        //Bit 2?                                                                                                //FLAG 93
        //Bit 3?                                                                                                //FLAG 92
        public static readonly FlagBit AnansiMusicPointsAwarded = new FlagBit(offset: 380, bit: 4);             //FLAG 91
        public static readonly FlagBit PuzzleSolvedClockChains = new FlagBit(offset: 380, bit: 5);              //FLAG 90
        public static readonly FlagBit PotPieceInInventory = new FlagBit(offset: 380, bit: 6);                  //FLAG 89
        public static readonly FlagBit AnansiMusicBoxOpen = new FlagBit(offset: 380, bit: 7);                   //FLAG 88


        public static readonly FlagBit ExploreMode = new FlagBit(offset: 381, bit: 0);                          //FLAG 87
        public static readonly FlagBit PolaroidCameraPictureDispensed = new FlagBit(offset: 381, bit: 1);       //FLAG 86
        public static readonly FlagBit AlchemyShippingBoxOpen = new FlagBit(offset: 381, bit: 2);               //FLAG 85
        public static readonly FlagBit AnansiKeyTurned = new FlagBit(offset: 381, bit: 2);                      //FLAG 84
        public static readonly FlagBit PuzzleSolvedChineseSolitaire = new FlagBit(offset: 381, bit: 4);         //FLAG 83
        public static readonly FlagBit PuzzleSolvedGallows = new FlagBit(offset: 381, bit: 5);                  //FLAG 82
        public static readonly FlagBit GallowsPanelOpen = new FlagBit(offset: 381, bit: 6);                     //FLAG 81
        public static readonly FlagBit BethElectricalPanelAvailable = new FlagBit(offset: 381, bit: 7);         //FLAG 80

    }

    public static class Points
    {
        public static class InformationPlaque
        {
            public const int TRANSFORMING_MASKS = 1012;                              //Lobby
            public const int JADE_SKULL = 1016;                                      //Lobby
            public const int BRONZE_UNICORN = 1020;                                  //Prehistoric
            public const int GRIFFIN = 1024;                                         //Prehistoric
            public const int EAGLES_NEST = 1028;                                     //Prehistoric
            public const int LARGE_SPIDER = 1032;                                    //Prehistoric
            public const int STARFISH = 1036;                                        //Prehistoric
            public const int QUARTZ_CRYSTAL = 1040;                                  //Ocean
            public const int POSEIDON = 1044;                                        //Ocean
            public const int COLOSSUS_OF_RHODES = 1052;                              //Ocean
            public const int POSEIDONS_TEMPLE = 1056;                                //Ocean
            public const int SUBTERRANEON_WORLD = 1060;                              //Underground Maze)
            public const int DERO = 1064;                                            //Underground Maze)
            public const int TOMB_OF_THE_IXUPI = 1068;                               //Egypt
            public const int THE_SPHINX = 1072;                                      //Egypt
            public const int CURSE_OF_ANUBIS = 1080;                                 //Egypt
            public const int NORSE_BURIAL_SHIP = 1084;                               //Burial
            public const int PARACAS_BURIAL_BUNDLES = 1088;                          //Burial
            public const int SPECTACULAR_COFFINS_OF_GHANA = 1092;                    //Burial
            public const int CREMATION = 1096;                                       //Burial
            public const int ANIMAL_CREMATORIUM = 1100;                              //Burial
            public const int SWITCH_DOCTORS_OF_THE_CONGO = 1104;                     //Shaman
            public const int SAROMBE_DOCTOR_OF_MOZAMBIQUE = 1112;                    //Shaman
            public const int FISHERMANS_CANOE_GOD = 1116;                            //Gods
            public const int MAYAN_GODS = 1120;                                      //Gods
            public const int THOR = 1124;                                            //Gods
            public const int CELTIC_JANUS_SCULPTURE = 1128;                          //Gods
            public const int SUMERIAN_BULL_GOD_AN = 1132;                            //Gods
            public const int SUMERIAN_LYRE = 1136;                                   //Gods
            public const int CHUEN = 1140;                                           //Gods
            public const int AFRICAN_CREATION_MYTH = 1144;                           //Anansi 
            public const int APOPHIS_THE_SERPENT = 1148;                             //Anansi 
            public const int DEATH = 1152;                                           //Anansi
            public const int CYCLOPS = 1156;                                         //Pegasus
            public const int LYCANTHROPY = 1160;                                     //Werewolf
            public const int COINCIDENCE_OR_EXTRATERRESTRIAL_VISITS = 1164;          //UFO
            public const int PLANETS = 1168;                                         //UFO
            public const int ASTRONOMICAL_CONTRUCTION = 1172;                        //UFO
            public const int GUILLOTINE = 1180;                                      //Torture
        }

    }
}

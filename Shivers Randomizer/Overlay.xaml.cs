using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for Overlay.xaml
/// </summary>
public partial class Overlay : Window
{
    public readonly App app;
    public string flagset = "";
    public UIntPtr hwnd;

    public Overlay(App app)
    {
        InitializeComponent();
        this.app = app;
        Loaded += (s, e) =>
        {
            hwnd = (UIntPtr)(long)(new WindowInteropHelper(this).Handle);
        };
    }

    public readonly SolidColorBrush brushLime = new(Colors.Lime);
    public readonly SolidColorBrush brushTransparent = new(Colors.Transparent);

    public void UpdateFlagset()
    {
        flagset = " ";
        if (app.settingsIncludeAsh) { flagset += "A"; }
        if (app.settingsIncludeLightning) { flagset += "I"; }
        if (app.settingsEarlyBeth) { flagset += "B"; }
        if (app.settingsExtraLocations) { flagset += "O"; }
        if (app.settingsExcludeLyre) { flagset += "Y"; }
        if (app.settingsRedDoor) { flagset += "D"; }
        if (app.settingsOnly4x4Elevators) { flagset += "4"; }
        if (app.settingsElevatorsStaySolved) { flagset += "S"; }
        if (app.settingsEarlyLightning)  { flagset += "G"; }
        if (app.settingsRoomShuffle) { flagset += "R"; }
        if (app.settingsIncludeElevators) { flagset += "E"; }
        if (app.settingsFullPots) { flagset += "F"; }
        if (app.settingsUnlockEntrance) { flagset += "U"; }
        if (app.settingsAnywhereLightning) { flagset += "L"; }
        if (flagset == " ") { flagset = ""; }
    }

    public void SetInfo()
    {
        string infoString = "";
        if (app.Seed != 0) { infoString = app.Seed.ToString(); }
        if (app.setSeedUsed) { infoString += " Set Seed"; }
        if (app.settingsMultiplayer) { infoString += " Multiplayer"; }
        if (app.settingsVanilla)
        {
            flagset = "";
            infoString += " Vanilla";
        }
        else
        {
            UpdateFlagset();
            if (app.settingsFirstToTheOnlyFive) { infoString += " FTTOF"; }
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        labelOverlay.Content = $"{infoString}{flagset} v{version}";
    }

    public void OverlayArchipelago()
    {
        if (app.archipelago_Client?.IsConnected ?? false)
        {
            labelOverlay.Content = "Connected to Archipelago";
        }
        else
        {
            labelOverlay.Content = "Not connected to Archipelago";
        }
    }

    public void GeoffreyPuzzleSolution(bool visible, double gameWidth, double gameHeight)
    {
        if(visible)
        {
            overlayGrid.Visibility = Visibility.Visible;

            double scaleWidth = (double)gameWidth / 320;
            double scaleHeight = (double)gameHeight / 200;

            //Calculate border size and window scale
            double borderWidth = 0;
            double borderHeight = 0;
            double windowRatio = gameWidth / gameHeight;
            if (windowRatio > 1.3333)
            {
                borderWidth = (gameWidth - (gameHeight * 1.3333)) / 2;
                scaleWidth = (gameWidth - borderWidth * 2) / 320;
            }
            else
            {
                borderHeight = (gameHeight - (gameWidth / 1.3333)) / 2;
                scaleHeight = (gameHeight - borderHeight * 2) / 200;
            }

            //Move overlay rectangle and scale it
            overlayGrid.SetValue(Canvas.LeftProperty, borderWidth + scaleWidth * 120);
            overlayGrid.SetValue(Canvas.TopProperty, borderHeight + scaleHeight * 91);
            overlayRectangle.Width = 85 * scaleWidth;
            overlayRectangle.Height = 54 * scaleHeight;


            // Scale the font size of overlayText based on the rectangle size
            overlayText.FontSize = overlayRectangle.Width / 6;
        }
        else
        {
            overlayGrid.Visibility = Visibility.Hidden;
        }

    }
}

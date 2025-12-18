using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Shivers_Randomizer.utils.AppHelpers;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for AttachPopup.xaml
/// </summary>
public partial class DevMenu : Window
{
    private readonly App app;

    public DevMenu(App app)
    {
        InitializeComponent();
        this.app = app;
    }

    private void button_Teleport_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int location))
        {
            if(app.MyAddress != UIntPtr.Zero)
            {
                app.StopAudio(location);
            }
        }
    }

    public void update_Pot_Locations()
    {
        UpdateLabelContentAndColor(LabelStorageDeskDrawer, 0);
        UpdateLabelContentAndColor(LabelStorageWorkshopDrawers, 8);
        UpdateLabelContentAndColor(LabelStorageLibraryCabinet, 16);
        UpdateLabelContentAndColor(LabelStorageLibraryStatue, 24);
        UpdateLabelContentAndColor(LabelStorageSlide, 32);
        UpdateLabelContentAndColor(LabelStorageTransformingMask, 40);
        UpdateLabelContentAndColor(LabelStorageEaglesNest, 48);
        UpdateLabelContentAndColor(LabelStorageOcean, 56);
        UpdateLabelContentAndColor(LabelStorageTarRiver, 64);
        UpdateLabelContentAndColor(LabelStorageTheater, 72);
        UpdateLabelContentAndColor(LabelStorageGreenhouse, 80);
        UpdateLabelContentAndColor(LabelStorageEgypt, 88);
        UpdateLabelContentAndColor(LabelStorageChineseSolitaire, 96);
        UpdateLabelContentAndColor(LabelStorageShamanHut, 104);
        UpdateLabelContentAndColor(LabelStorageLyre, 112);
        UpdateLabelContentAndColor(LabelStorageSkeleton, 120);
        UpdateLabelContentAndColor(LabelStorageAnansiMusicBox, 128);
        UpdateLabelContentAndColor(LabelStorageJanitorCloset, 136);
        UpdateLabelContentAndColor(LabelStorageUFO, 144);
        UpdateLabelContentAndColor(LabelStorageAlchemy, 152);
        UpdateLabelContentAndColor(LabelStorageSkullBridge, 160);
        UpdateLabelContentAndColor(LabelStorageGallows, 168);
        UpdateLabelContentAndColor(LabelStorageClockTower, 176);
    }


    public static readonly SolidColorBrush[] ElementBrushes =
{
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 168, 252)),   // 0
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 250, 205)), // 1
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)), // 2
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 174, 201)), // 3
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0)),     // 4
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 205, 50)),   // 5
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(173, 216, 230)), // 6
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 51)),  // 7
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 164, 96)),  // 8
        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255))  // 9
    };

    void UpdateLabelContentAndColor(Label label, int memoryOffset)
    {
        int potID = app.ReadMemory(memoryOffset, 1);
        label.Content = ConvertPotNumberToString(potID);
        if (label.Content != null)
        {
            label.Foreground = ElementBrushes[potID % 10];
        }
        else
        {
            label.Content = "";
        }
    }
}

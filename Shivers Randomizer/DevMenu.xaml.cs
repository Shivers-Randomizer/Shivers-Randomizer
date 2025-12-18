using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
}

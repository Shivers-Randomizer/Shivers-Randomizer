using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shivers_Randomizer;
/// <summary>
/// Interaction logic for ColorPicker.xaml
/// </summary>
public partial class ColorPicker : Window
{
    private Dictionary<string, Color> currentColors;
    private Dictionary<string, Label> targetLabels;
    private string selectedTarget = null;
    private double currentBrightness = 1.0;

    public ColorPicker()
    {
        InitializeComponent();
        currentColors = new Dictionary<string, Color>(defaultColors);

        labelLocation.Foreground = new SolidColorBrush(currentColors["Location"]);
        labelAsh.Foreground = new SolidColorBrush(currentColors["Ash"]);
        labelCloth.Foreground = new SolidColorBrush(currentColors["Cloth"]);
        labelCrystal.Foreground = new SolidColorBrush(currentColors["Crystal"]);
        labelLightning.Foreground = new SolidColorBrush(currentColors["Lightning"]);
        labelMetal.Foreground = new SolidColorBrush(currentColors["Metal"]);
        labelOil.Foreground = new SolidColorBrush(currentColors["Oil"]);
        labelSand.Foreground = new SolidColorBrush(currentColors["Sand"]);
        labelWater.Foreground = new SolidColorBrush(currentColors["Water"]);
        labelWax.Foreground = new SolidColorBrush(currentColors["Wax"]);
        labelWood.Foreground = new SolidColorBrush(currentColors["Wood"]);

        targetLabels = new Dictionary<string, Label>()
        {
            ["Background"] = labelBackground,
            ["Location"] = labelLocation,
            ["Ash"] = labelAsh,
            ["Cloth"] = labelCloth,
            ["Crystal"] = labelCrystal,
            ["Lightning"] = labelLightning,
            ["Metal"] = labelMetal,
            ["Oil"] = labelOil,
            ["Sand"] = labelSand,
            ["Water"] = labelWater,
            ["Wax"] = labelWax,
            ["Wood"] = labelWood
        };

        ColorSpectrum.MouseLeftButtonDown += ColorSpectrum_MouseLeftButtonDown;
        ColorSpectrum.MouseMove += ColorSpectrum_MouseMove;
        ColorSpectrum.MouseLeftButtonUp += ColorSpectrum_MouseLeftButtonUp;

        RenderColorSpectrum();
    }

    private Dictionary<string, Color> defaultColors = new()
    {
        ["Background"] = Color.FromRgb(51, 51, 51),
        ["Location"] = Color.FromRgb(160, 190, 130),
        ["Ash"] = Color.FromRgb(180, 180, 180),
        ["Cloth"] = Colors.Red,
        ["Crystal"] = Color.FromRgb(173, 216, 230),
        ["Lightning"] = Color.FromRgb(255, 255, 51),
        ["Metal"] = Colors.White,
        ["Oil"] = Color.FromRgb(255, 174, 201),
        ["Sand"] = Color.FromRgb(244, 164, 96),
        ["Water"] = Color.FromRgb(5, 168, 252),
        ["Wax"] = Color.FromRgb(255, 250, 205),
        ["Wood"] = Color.FromRgb(50, 205, 50)
    };



    private void Label_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Label label && label.Content is string labelKey)
        {
            selectedTarget = labelKey;
            ColorSpectrum.IsEnabled = true;
        }
    }

    private void ApplyColorToLabel(string targetName, Color color)
    {
        // Update dictionary
        if (currentColors.ContainsKey(targetName))
            currentColors[targetName] = color;

        if (targetName == "Background")
        {
            // Update the Window's background brush
            this.Background = new SolidColorBrush(color);
            stackPanelColorTarget.Background = new SolidColorBrush(color);
        }
        else
        {
            // Update label foreground color if label exists
            if (targetLabels.TryGetValue(targetName, out var label))
            {
                label.Foreground = new SolidColorBrush(color);
            }
        }
    }



    private void RenderColorSpectrum()
    {
        int width = (int)ColorSpectrum.Width;
        int height = (int)ColorSpectrum.Height;

        WriteableBitmap bmp = new(width, height, 96, 96, PixelFormats.Bgra32, null);
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];

        for (int y = 0; y < height; y++)
        {
            double saturation = 1.0 - (y / (double)(height - 1)); // Top = full saturation, bottom = no saturation

            for (int x = 0; x < width; x++)
            {
                double hue = (x / (double)(width - 1)) * 360.0; // Hue from 0 to 360
                Color color = FromHsv(hue, saturation, currentBrightness); // Full brightness

                int index = (y * stride) + (x * 4);
                pixels[index + 0] = color.B;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.R;
                pixels[index + 3] = 255; // Alpha
            }
        }

        bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        ColorSpectrum.Source = bmp;
    }

    private static Color FromHsv(double hue, double saturation, double value)
    {
        int hi = (int)(hue / 60.0) % 6;
        double f = (hue / 60.0) - Math.Floor(hue / 60.0);

        value = value * 255;
        byte v = (byte)(value);
        byte p = (byte)(value * (1 - saturation));
        byte q = (byte)(value * (1 - f * saturation));
        byte t = (byte)(value * (1 - (1 - f) * saturation));

        return hi switch
        {
            0 => Color.FromRgb(v, t, p),
            1 => Color.FromRgb(q, v, p),
            2 => Color.FromRgb(p, v, t),
            3 => Color.FromRgb(p, q, v),
            4 => Color.FromRgb(t, p, v),
            _ => Color.FromRgb(v, p, q),
        };
    }




    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        currentColors = new Dictionary<string, Color>(defaultColors);

        // Update all labels with reset colors
        foreach (var kvp in currentColors)
        {
            ApplyColorToLabel(kvp.Key, kvp.Value);
        }
    }




    private bool isPickingColor = false;

    private void ColorSpectrum_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (selectedTarget == null)
            return;

        isPickingColor = true;
        ColorSpectrum.CaptureMouse();
        UpdateColorFromPosition(e.GetPosition(ColorSpectrum));
    }

    private void ColorSpectrum_MouseMove(object sender, MouseEventArgs e)
    {
        if (isPickingColor)
        {
            UpdateColorFromPosition(e.GetPosition(ColorSpectrum));
        }
    }

    private void ColorSpectrum_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isPickingColor)
        {
            isPickingColor = false;
            ColorSpectrum.ReleaseMouseCapture();
        }
    }

    private void UpdateColorFromPosition(Point position)
    {
        var bmp = ColorSpectrum.Source as BitmapSource;
        if (bmp == null)
            return;

        int x = (int)position.X;
        int y = (int)position.Y;

        x = Math.Clamp(x, 0, bmp.PixelWidth - 1);
        y = Math.Clamp(y, 0, bmp.PixelHeight - 1);

        var pixels = new byte[4];
        int stride = bmp.PixelWidth * 4;
        bmp.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, stride, 0);

        var color = Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);

        ApplyColorToLabel(selectedTarget, color);
    }
}

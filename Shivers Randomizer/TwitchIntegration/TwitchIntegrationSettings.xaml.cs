using Shivers_Randomizer.TwitchIntegration;
using System.Linq;
using System.Windows;

namespace Shivers_Randomizer;

public partial class TwitchIntegrationSettings : Window
{
    private TwitchIntegrationMain main;

    public TwitchIntegrationSettings(TwitchIntegrationMain main)
    {
        InitializeComponent();
        this.main = main;

        txtPointsPerMinute.Text = main.pointsPerMinute.ToString();

        dgTeleports.ItemsSource = BotData.TeleportLocations.Values.ToList();

        dgPuzzles.ItemsSource = BotData.PuzzleFlags.Select(kvp => new PuzzleViewModel { Name = kvp.Key, Flag = kvp.Value.Flag, Cost = kvp.Value.Cost }).ToList();

        dgIxupi.ItemsSource = BotData.IxupiFlags.Select(kvp => new IxupiViewModel { Name = kvp.Key, Info = kvp.Value, Cost = kvp.Value.Cost }).ToList();
    }

    private void DataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
    {
        var grid = sender as System.Windows.Controls.DataGrid;
        grid?.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
    }

    private void txtPointsPerMinute_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (int.TryParse(txtPointsPerMinute.Text, out int value))
        {
            if (value >= 0)
            {
                main.pointsPerMinute = value;
            }
        }
    }

    private void NumberOnly(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void BlockSpace(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Space)
        {
            e.Handled = true;
        }
    }
}
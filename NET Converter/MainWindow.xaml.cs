using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NET_Converter.Data;

namespace NET_Converter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeAppendixCombo();
    }

    private void InitializeAppendixCombo()
    {
        AppendixCombo.ItemsSource = new[]
        {
            "",
            "windows",
            "linux",
            "macos",
            "android",
            "ios",
            "maccatalyst",
            "tvos",
            "browser"
        };
        AppendixCombo.SelectedIndex = 0;
    }

    private async void Open_Project_Click(object sender, RoutedEventArgs e)
    {
        DisableMenuItems();

        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "C# Project Files (*.csproj)|*.csproj",
            Title = "Select a .csproj File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ProjectPathText.Text = openFileDialog.FileName;
            try
            {
                string sourceFramework = await Project.AnalyzeProjectAsync(openFileDialog.FileName);
                if (sourceFramework != "Target Framework not found.")
                {
                    NETVersionText.Text = sourceFramework;
                    NETVersionText.Background = Brushes.White;
                    TargetFrameworkCombo.ItemsSource = NETHelper.GetAvailableNETTargetVersions();
                    TargetFrameworkCombo.SelectedIndex = 0;
                    TargetFrameworkCombo.IsEnabled = true;
                }
                else
                {
                    NETVersionText.Text = "Source framework couldn't be determined";
                    NETVersionText.Background = Brushes.LightPink;
                }
            }
            catch (Exception ex)
            {
                NETVersionText.Text = $"Error analyzing project: {ex.Message}";
                NETVersionText.Background = Brushes.LightPink;
            }
        }

        EnableMenuItems();
    }

    private async void Migrate_Project_Click(object sender, RoutedEventArgs e)
    {
        DisableMenuItems();

        try
        {
            if (TargetFrameworkCombo.SelectedItem is string targetVersionString)
            {
                var targetVersion = NETHelper.ParseTargetVersion(targetVersionString);
                var appendix = AppendixCombo.SelectedItem as string ?? throw new Exception("No valid appendix selected.");
                await Project.MigrateProjectAsync(targetVersion, appendix);
                MessageBox.Show("Project migration completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a target framework.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error migrating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        EnableMenuItems();
    }

    private void DisableMenuItems()
    {
        foreach (var item in MainMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsEnabled = false;
            }
        }
    }

    private void EnableMenuItems()
    {
        foreach (var item in MainMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.IsEnabled = true;
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Restore_Project_Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Project.UndoMigration();
            MessageBox.Show("Project backup restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error restoring project backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

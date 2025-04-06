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
using static NET8ExceptionHandler.ExceptionManagement;
using System.Diagnostics;

namespace NET_Converter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private CancellationTokenSource? _cancellationTokenSource = null;

    public MainWindow()
    {
        CreateExceptionManagement(App.Current, AppDomain.CurrentDomain, true, true);
        InitializeComponent();
        InitializeAppendixCombo();
        StartPollingForDotNetVersions();
    }

    private void InitializeAppendixCombo()
    {
        AppendixCombo.ItemsSource = NETHelper.Appendices;
        AppendixCombo.SelectedIndex = 0;
    }

    private void TargetFrameworkCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TargetFrameworkCombo.SelectedItem is string selectedVersion)
        {
            if (selectedVersion.Contains("(not installed)"))
            {
                InstallFrameworkButton.Visibility = Visibility.Visible;
            }
            else
            {
                InstallFrameworkButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void InstallFrameworkButton_Click(object sender, RoutedEventArgs e)
    {
        if (TargetFrameworkCombo.SelectedItem is string selectedVersion)
        {
            var netVersion = selectedVersion.Replace(".NET ", "").Replace(" (not installed)", "");
            var installUrl = $"https://dotnet.microsoft.com/download/dotnet/{netVersion}";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = installUrl,
                UseShellExecute = true
            };

            Process? process = Process.Start(processStartInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
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
                var targetVersion = NETHelper.ParseTargetVersion(targetVersionString.Split(' ')[0]);
                var appendix = AppendixCombo.SelectedItem as string ?? throw new Exception("No valid appendix selected.");
                await Project.MigrateProjectAsync(targetVersion, appendix);
                EHMsgBox(LogEntryTypes.INFO, "Project migration completed successfully.");
            }
            else
            {
                EHMsgBox(LogEntryTypes.WARNING, "Please select a target framework.");
            }
        }
        catch (Exception ex)
        {
            EHMsgBox(LogEntryTypes.WARNING, $"Error migrating project: {ex.Message}");
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

    private async void Restore_Project_Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Project.UndoMigration();
            EHMsgBox(LogEntryTypes.INFO, "Project backup restored successfully.");
        }
        catch (Exception ex)
        {
            EHMsgBox(LogEntryTypes.WARNING, $"Error restoring project backup: {ex.Message}");
        }
    }

    private void StartPollingForDotNetVersions()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            int loopCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken);
                if (loopCount < 100)
                {
                    // Increment the loop count
                    loopCount++;
                    continue; // Skip the rest of the loop
                }

                await Dispatcher.InvokeAsync(async () =>
                {
                    var installedSdks = await NETHelper.ListDotNetSdksAsync();
                    var parsedSdkVersions = NETHelper.ParseSdkVersions(installedSdks);
                    var availableVersions = NETHelper.AvailableNETTargetVersions.Select(version =>
                    {
                        var netVersion = version.Replace(".NET ", "net");
                        var isInstalled = parsedSdkVersions.Contains(netVersion);
                        return isInstalled ? version : $"{version} (not installed)";
                    }).ToList();

                    if (TargetFrameworkCombo.ItemsSource is List<string> previouslyAvailableVersions 
                    && availableVersions.SequenceEqual(previouslyAvailableVersions))
                        return; // No change in available versions

                    TargetFrameworkCombo.ItemsSource = availableVersions;
                });

                loopCount = 0; // Reset the loop count after 10 iterations
            }
        }, cancellationToken);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        base.OnClosed(e);
    }
}

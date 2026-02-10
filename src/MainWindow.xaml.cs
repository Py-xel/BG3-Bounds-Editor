using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Collections.Generic; // Required for Dictionary

namespace BG3_Bounds_Editor;

public partial class MainWindow : Window
{
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private readonly HashSet<string> ExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "CrossplayUI",
    "DiceSet_01",
    "DiceSet_02",
    "DiceSet_03",
    "DiceSet_06",
    "Engine",
    "Gustav",
    "GustavDev",
    "GustavX",
    "Honour",
    "HonourX",
    "MainUI",
    "ModBrowser",
    "PhotoMode",
    "Shared",
    "SharedDev",
    "UserTemp"
};


    private void PopulateProjectDropdown(string mainPath, string? lastSelectedProject = null)
    {
        if (string.IsNullOrWhiteSpace(mainPath) || !Directory.Exists(mainPath))
            return;

        try
        {
            ProjectComboBox.Items.Clear();

            string publicPath = Path.Combine(mainPath, "Public");

            if (!Directory.Exists(publicPath))
            {
                MessageBox.Show(
                    $"The required folder could not be found:\n\n{publicPath}\n\n" + "Make sure you have selected the correct Data folder!", "Required Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subDirectories = Directory.GetDirectories(publicPath)
                .Select(dir => Path.GetFileName(dir))
                .Where(folderName => !ExcludedFolders.Contains(folderName))
                .OrderBy(folderName => folderName); // optional: sort alphabetically

            foreach (string folderName in subDirectories)
            {
                ProjectComboBox.Items.Add(folderName);
            }

            // Restore last selected project
            if (!string.IsNullOrEmpty(lastSelectedProject) &&
                ProjectComboBox.Items.Contains(lastSelectedProject))
            {
                ProjectComboBox.SelectedItem = lastSelectedProject;
            }
            else if (ProjectComboBox.Items.Count > 0)
            {
                ProjectComboBox.SelectedIndex = 0;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                "Access to the required folder was denied.\n\n" + "Please check your permissions and try again.\n\n" + $"Details: {ex.Message}", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "An unexpected error occurred while loading required folders.\n\n" + $"Details: {ex.Message}", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void SaveSettings()
    {
        var data = new Dictionary<string, string>
        {
            { "BG3 Mod Folder Path", PathTextBox.Text },
            { "Last Selected Project", ProjectComboBox.SelectedItem?.ToString() ?? "" }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(_configPath, json);
    }

    private void LoadSettings()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                string json = File.ReadAllText(_configPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("BG3 Mod Folder Path", out JsonElement pathElement))
                {
                    string mainPath = pathElement.GetString() ?? "";
                    PathTextBox.Text = mainPath;

                    // Load projects after setting the text
                    string lastProject = "";
                    if (root.TryGetProperty("Last Selected Project", out JsonElement projectElement))
                    {
                        lastProject = projectElement.GetString() ?? "";
                    }

                    PopulateProjectDropdown(mainPath, lastProject);
                }
            }
            catch { /* Handle corrupted JSON */ }
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Baldur's Gate 3 data directory",
            InitialDirectory = PathTextBox.Text
        };

        if (dialog.ShowDialog() == true)
        {
            PathTextBox.Text = dialog.FolderName;
            PopulateProjectDropdown(dialog.FolderName);
            SaveSettings(); // Save everything immediately
        }
    }

    // Triggered when the user manually changes the dropdown selection
    private void ProjectComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SaveSettings();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
}

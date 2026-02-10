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

    private void PopulateProjectDropdown(string mainPath, string? lastSelectedProject = null)
    {
        if (string.IsNullOrWhiteSpace(mainPath) || !Directory.Exists(mainPath)) return;

        try
        {
            ProjectComboBox.Items.Clear();
            string[] subDirectories = Directory.GetDirectories(mainPath);

            foreach (string dir in subDirectories)
            {
                string folderName = Path.GetFileName(dir);
                ProjectComboBox.Items.Add(folderName);
            }

            // Restore the last selected project if it still exists
            if (!string.IsNullOrEmpty(lastSelectedProject) && ProjectComboBox.Items.Contains(lastSelectedProject))
            {
                ProjectComboBox.SelectedItem = lastSelectedProject;
            }
            else if (ProjectComboBox.Items.Count > 0)
            {
                ProjectComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading project folders: {ex.Message}");
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

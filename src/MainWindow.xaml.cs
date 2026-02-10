using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;

namespace BG3_Bounds_Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    private void SaveSettings(string path)
    {
        var data = new Dictionary<string, string>
    {
        { "BG3 Mod Folder Path", path }
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
                if (doc.RootElement.TryGetProperty("BG3 Mod Folder Path", out JsonElement pathElement))
                {
                    PathTextBox.Text = pathElement.GetString();
                }
            }
            catch { /* Handle corrupted JSON if necessary */ }
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Baldur's Gate 3 data directory",
            InitialDirectory = PathTextBox.Text // Starts where they last were
        };

        if (dialog.ShowDialog() == true)
        {
            PathTextBox.Text = dialog.FolderName;
            SaveSettings(dialog.FolderName);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
}
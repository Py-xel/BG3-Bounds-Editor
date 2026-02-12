using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Windows.Controls;
using System.Xml.Linq;
using LSLib.LS;
using LSLib.LS.Enums;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.ComponentModel;

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

    private string MainDataPath = "";

    private void PopulateProjectDropdown(string mainPath, string? lastSelectedProject = null)
    {
        MainDataPath = mainPath;

        ProjectComboBox.Items.Clear();
        LSFComboBox.Items.Clear();

        if (string.IsNullOrWhiteSpace(mainPath) || !Directory.Exists(mainPath)) return;

        try
        {
            ProjectComboBox.Items.Clear();
            string publicPath = Path.Combine(mainPath, "Public");

            if (!Directory.Exists(publicPath))
            {
                // LOG: ERROR HERE
                //MessageBox.Show($"Folder not found:\n\n{publicPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subDirectories = Directory.GetDirectories(publicPath)
                .Select(dir => Path.GetFileName(dir))
                .Where(name => !ExcludedFolders.Contains(name))
                .OrderBy(name => name);

            foreach (string folderName in subDirectories)
                ProjectComboBox.Items.Add(folderName);

            if (!string.IsNullOrEmpty(lastSelectedProject) && ProjectComboBox.Items.Contains(lastSelectedProject))
                ProjectComboBox.SelectedItem = lastSelectedProject;
            else if (ProjectComboBox.Items.Count > 0)
                ProjectComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            // LOG: ERROR HERE
            //MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem == null)
            return;

        string selectedProject = ProjectComboBox.SelectedItem.ToString()!;
        PopulateLSFDropdown(selectedProject);
    }
    private void PopulateLSFDropdown(string selectedProject)
    {
        LSFComboBox.Items.Clear();
        if (string.IsNullOrEmpty(selectedProject) || string.IsNullOrEmpty(MainDataPath)) return;

        try
        {
            string assetsPath = Path.Combine(MainDataPath, "Public", selectedProject, "Content");
            if (!Directory.Exists(assetsPath)) return;

            var lsfFiles = Directory.EnumerateFiles(assetsPath, "*.lsf", SearchOption.AllDirectories)
                .Select(file => Path.GetFileName(file))
                .OrderBy(name => name)
                .ToList(); // Keep as a list

            foreach (string file in lsfFiles)
                LSFComboBox.Items.Add(file);

            if (LSFComboBox.Items.Count > 0)
                LSFComboBox.SelectedIndex = 0;
        }
        catch (Exception) { LSFComboBox.Items.Clear(); }
    }

    private void LSFSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = LSFSearchBox.Text.ToLower();

        // Get the default view for the ComboBox items
        ICollectionView view = CollectionViewSource.GetDefaultView(LSFComboBox.Items);

        if (view != null)
        {
            view.Filter = item =>
            {
                if (string.IsNullOrWhiteSpace(filter)) return true;
                return item.ToString()!.ToLower().Contains(filter);
            };
        }

        // Auto-select the first result of the filtered list
        if (LSFComboBox.Items.Count > 0) LSFComboBox.SelectedIndex = 0;
    }

    /* USER SETTINGS */
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

    /* WINDOW CONTROL */
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) this.DragMove();
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
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string selectedProject = ProjectComboBox.SelectedItem.ToString();
        string selectedFile = LSFComboBox.SelectedItem.ToString();
        string lsfPath = Path.Combine(MainDataPath, "Public", selectedProject, "Content", selectedFile);

        string tempLsx = Path.ChangeExtension(lsfPath, ".lsx");

        try
        {
            ConvertLsfToLsxInternal(lsfPath, tempLsx);
            EditLsxBounds(tempLsx, MinBoundTextBox.Text, MaxBoundTextBox.Text);
            ConvertLsxToLsfInternal(tempLsx, lsfPath);

            // Delete .lsx if prompted
            if (KeepLsxCheckBox.IsChecked == false && File.Exists(tempLsx))
            {
                File.Delete(tempLsx);
            }

            // LOG: SUCCESS HERE
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during processing: {ex.Message}");
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

    /* CONVERSION */
    private void ConvertLsfToLsxInternal(string inputPath, string outputPath)
    {
        try
        {
            var loadParams = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
            var conversionParams = new ResourceConversionParameters
            {
                LSF = LSFVersion.VerBG3Patch3,
                LSX = LSXVersion.V4,
                PrettyPrint = true
            };

            Resource resource = ResourceUtils.LoadResource(inputPath, loadParams);
            ResourceUtils.SaveResource(resource, outputPath, conversionParams);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"LSLib Error: {ex.Message}");
        }
    }
    private void ConvertLsxToLsfInternal(string inputPath, string outputPath)
    {
        var loadParams = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
        var conversionParams = new ResourceConversionParameters
        {
            LSF = LSFVersion.VerBG3Patch3,
            LSX = LSXVersion.V4
        };

        Resource resource = ResourceUtils.LoadResource(inputPath, loadParams);
        ResourceUtils.SaveResource(resource, outputPath, ResourceFormat.LSF, conversionParams);
    }
    private void EditLsxBounds(string lsxPath, string minVal, string maxVal)
    {
        XDocument doc = XDocument.Load(lsxPath);

        // .lsf id must be "VisualBank" only!
        var visualBankRegion = doc.Descendants("region")
            .FirstOrDefault(r => (string)r.Attribute("id") == "VisualBank");

        if (visualBankRegion == null)
        {
            throw new Exception("This file is not a valid VisualBank resource.");
        }

        bool minUpdated = false;
        bool maxUpdated = false;

        var attributes = doc.Descendants("attribute");
        foreach (var attr in attributes)
        {
            string? id = attr.Attribute("id")?.Value;

            if (id == "BoundsMin")
            {
                attr.SetAttributeValue("value", minVal);
                minUpdated = true;
            }
            else if (id == "BoundsMax")
            {
                attr.SetAttributeValue("value", maxVal);
                maxUpdated = true;
            }
        }

        if (!minUpdated || maxUpdated == false)
        {
            throw new Exception("Could not find BoundsMin or BoundsMax attributes in this file.");
        }

        doc.Save(lsxPath);
    }

    /* INPUT SANITIZATION */
    private void BoundsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // The \- escapes the hyphen so it's treated as a character, not a range
        Regex regex = new Regex("[^0-9.\\-\\s]+");
        e.Handled = regex.IsMatch(e.Text);
    }
    private void BoundsTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            string text = (string)e.DataObject.GetData(DataFormats.Text);

            // Convert any European-style commas to dots before validation
            string sanitized = text.Replace(',', '.');

            Regex regex = new Regex("[^0-9.\\-\\s]+");
            if (regex.IsMatch(sanitized))
            {
                e.CancelCommand();
            }
        }
    }
}

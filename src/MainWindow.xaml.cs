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
using System.Windows.Documents;
using System.Windows.Media;

namespace BG3_Bounds_Editor;

public partial class MainWindow : Window
{
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public MainWindow()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
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
                LogToConsole($"Invalid folder: '{publicPath}' — Make sure to select the correct Data Folder!", LogType.Warning);
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
            LogToConsole($"Error loading project: {ex.Message} — Did you select the correct Data Folder?", LogType.Error);
        }
    }
    private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem == null) return;

        string selectedProject = ProjectComboBox.SelectedItem.ToString()!;
        PopulateLSFDropdown(selectedProject);
        SaveSettings();
    }
    private void KeepLsxCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }
    private void PopulateLSFDropdown(string selectedProject)
    {
        LSFComboBox.Items.Clear();
        if (string.IsNullOrEmpty(selectedProject) || string.IsNullOrEmpty(MainDataPath)) return;

        try
        {
            string assetsPath = Path.Combine(MainDataPath, "Public", selectedProject, "Content");

            if (!Directory.Exists(assetsPath))
            {
                LogToConsole($"Content folder not found for project: {selectedProject}", LogType.Warning);
                return;
            }

            var lsfFiles = Directory.EnumerateFiles(assetsPath, "*.lsf", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(assetsPath, file))
                .OrderBy(name => name)
                .ToList();

            if (lsfFiles.Count == 0)
            {
                LogToConsole($"No .lsf files found in project: {selectedProject}", LogType.Warning);
                return;
            }

            foreach (string file in lsfFiles)
                LSFComboBox.Items.Add(file);

            if (LSFComboBox.Items.Count > 0)
                LSFComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            LSFComboBox.Items.Clear();
            LogToConsole($"Error loading .lsf file(s): {ex.Message}", LogType.Error);
        }
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
    private bool _boundsSwapped = false;
    private void SwapBoundsButton_Click(object sender, RoutedEventArgs e)
    {
        _boundsSwapped = !_boundsSwapped;
        SwapBoundsControls();
        SaveSettings();
    }
    private void SwapBoundsControls()
    {
        if (BoundsStackPanel.Children.Count < 4)
            return;

        var minLabel = BoundsStackPanel.Children[0];
        var minBox = BoundsStackPanel.Children[1];
        var maxLabel = BoundsStackPanel.Children[2];
        var maxBox = BoundsStackPanel.Children[3];

        BoundsStackPanel.Children.Clear();

        BoundsStackPanel.Children.Add(maxLabel);
        BoundsStackPanel.Children.Add(maxBox);
        BoundsStackPanel.Children.Add(minLabel);
        BoundsStackPanel.Children.Add(minBox);
    }
    private void SaveSettings()
    {
        try
        {
            var configData = new Dictionary<string, object>
        {
            { "BG3 Mod Folder Path", PathTextBox.Text },
            { "Last Selected Project", ProjectComboBox.SelectedItem?.ToString() ?? "" },
            { "Keep .lsx after conversion", KeepLsxCheckBox.IsChecked ?? false },
            { "Bounds Swapped", _boundsSwapped }
        };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(configData, options);

            // Use the centralized path variable
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            LogToConsole($"Failed to save settings: {ex.Message}", LogType.Warning);
        }
    }
    private void LoadSettings()
    {
        if (!File.Exists(_configPath)) return;

        try
        {
            string json = File.ReadAllText(_configPath);
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("BG3 Mod Folder Path", out JsonElement pathElement))
            {
                string mainPath = pathElement.GetString() ?? "";
                PathTextBox.Text = mainPath;

                string lastProject = "";
                if (root.TryGetProperty("Last Selected Project", out JsonElement projectElement))
                {
                    lastProject = projectElement.GetString() ?? "";
                }

                PopulateProjectDropdown(mainPath, lastProject);
            }

            if (root.TryGetProperty("Keep .lsx after conversion", out JsonElement checkElement))
            {
                KeepLsxCheckBox.IsChecked = checkElement.GetBoolean();
            }

            // Fixed Casing: "Bounds Swapped" to match SaveSettings
            if (root.TryGetProperty("Bounds Swapped", out JsonElement swapElement))
            {
                _boundsSwapped = swapElement.GetBoolean();
                if (_boundsSwapped)
                {
                    SwapBoundsControls();
                }
            }
        }
        catch (Exception ex)
        {
            LogToConsole($"Could not load config: {ex.Message}", LogType.Error);
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
            SaveSettings();
        }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string rawMin = MinBoundTextBox.Text;
        string rawMax = MaxBoundTextBox.Text;

        string trimmedMin = rawMin.Trim();
        string trimmedMax = rawMax.Trim();

        // Guard Clause
        if (!IsInputValid(trimmedMin) || !IsInputValid(trimmedMax))
        {
            LogToConsole("Operation aborted: — Invalid input! Values must adhere to the following pattern: (e.g. -1.0 2.5 0).", LogType.Error);
            return;
        }

        if (rawMin != trimmedMin || rawMax != trimmedMax)
        {
            LogToConsole("Trailing characters removed!", LogType.Info);
        }

        string selectedProject = ProjectComboBox.SelectedItem.ToString();
        string selectedFile = LSFComboBox.SelectedItem.ToString();
        string lsfPath = Path.Combine(MainDataPath, "Public", selectedProject, "Content", selectedFile);
        string tempLsx = Path.ChangeExtension(lsfPath, ".lsx");

        try
        {
            ConvertLsfToLsxInternal(lsfPath, tempLsx);
            EditLsxBounds(tempLsx, trimmedMin, trimmedMax);
            ConvertLsxToLsfInternal(tempLsx, lsfPath);

            if (KeepLsxCheckBox.IsChecked == false && File.Exists(tempLsx))
            {
                File.Delete(tempLsx);
            }
        }
        catch (Exception ex)
        {
            LogToConsole($"Processing Error: {ex.Message}", LogType.Error);
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
            LogToConsole($"LSLib encountered an error: {ex.Message}", LogType.Error);
        }
    }
    private void ConvertLsxToLsfInternal(string inputPath, string outputPath)
    {
        var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            // Force the conversion process to use dots for decimals
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            var loadParams = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
            var conversionParams = new ResourceConversionParameters
            {
                LSF = LSFVersion.VerBG3Patch3,
                LSX = LSXVersion.V4
            };

            // LSLib will now correctly parse '1.012' from the XML
            Resource resource = ResourceUtils.LoadResource(inputPath, loadParams);
            ResourceUtils.SaveResource(resource, outputPath, ResourceFormat.LSF, conversionParams);
        }
        catch (Exception ex)
        {
            LogToConsole($"LSLib encountered an error: {ex.Message}", LogType.Error);
        }
        finally
        {
            // Always restore the user's system culture
            System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
    private void EditLsxBounds(string lsxPath, string minVal, string maxVal)
    {
        try
        {
            XDocument doc = XDocument.Load(lsxPath);

            // .lsf id must be "VisualBank" only!
            var visualBankRegion = doc.Descendants("region")
                .FirstOrDefault(r => (string)r.Attribute("id") == "VisualBank");

            if (visualBankRegion == null)
            {
                LogToConsole("Validation Failed: Region 'VisualBank' not found. This file might not contain visual data.", LogType.Error);
                return;
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

            if (!minUpdated || !maxUpdated)
            {
                LogToConsole($"Could not find attributes [BoundsMin] and [BoundsMax] in: {Path.GetFileName(lsxPath)}", LogType.Error);
                return;
            }

            doc.Save(lsxPath);

            string fileName = Path.GetFileName(lsxPath).Replace(".lsx", ".lsf");
            LogToConsole($"Updated: {fileName}" + $"\n    > New BoundsMin: [{minVal}]\n    > new BoundsMax: [{maxVal}]", LogType.Success);
        }
        catch (Exception ex)
        {
            LogToConsole($"An unexpected error occurred while editing: {ex.Message}", LogType.Error);
        }
    }

    /* INPUT SANITIZATION */
    private bool IsInputValid(string input)
    {
        string pattern = @"^-?\d+(\.\d+)? -?\d+(\.\d+)? -?\d+(\.\d+)?$";
        return Regex.IsMatch(input.Trim(), pattern);
    }
    private void BoundsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Block any character that isn't a digit, dot, hyphen, or space
        Regex regex = new Regex("[^0-9.\\- ]+");
        e.Handled = regex.IsMatch(e.Text);
    }
    private void BoundsTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Explicitly allow the space bar even if PreviewTextInput misses it
        if (e.Key == Key.Space)
        {
            e.Handled = false;
        }
    }

    /* LOGGING */
    public enum LogType { Info, Warning, Error, Success }
    private void LogToConsole(string message, LogType type = LogType.Info)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string fullDate = DateTime.Now.ToString("yyyy-MM-dd");
        string tag = $"[{type.ToString().ToUpper()}]";
        string logLine = $"[{fullDate} {timestamp}] {tag} {message}";

        try
        {
            File.AppendAllText("log.txt", logLine + Environment.NewLine);
        }
        catch (Exception)
        {

        }

        Dispatcher.Invoke(() =>
        {
            Brush color = type switch
            {
                LogType.Info => new SolidColorBrush(Color.FromRgb(75, 145, 219)),
                LogType.Warning => new SolidColorBrush(Color.FromRgb(219, 181, 75)),
                LogType.Error => new SolidColorBrush(Color.FromRgb(219, 75, 75)),
                LogType.Success => new SolidColorBrush(Color.FromRgb(75, 219, 116)),
                _ => Brushes.White
            };

            string prefix = ConsoleBlock.Inlines.Count > 0 ? "\n" : "";

            Run logEntry = new Run($"{prefix}> [{timestamp}] {tag} {message}")
            {
                Foreground = color
            };

            ConsoleBlock.Inlines.Add(logEntry);

            var scrollViewer = ConsoleBlock.Parent as ScrollViewer;
            scrollViewer?.ScrollToEnd();
        });
    }
}

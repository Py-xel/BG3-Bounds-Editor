using System.Windows;

namespace BG3_Bounds_Editor;

public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"Unhandled Error: {e.Exception.Message}\n\nStack Trace: {e.Exception.StackTrace}", "Critical Error");
            e.Handled = true;
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error: {ex.Message}");
        }
    }
}

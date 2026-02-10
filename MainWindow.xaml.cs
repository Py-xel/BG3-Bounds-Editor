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

namespace BG3_Bounds_Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if(e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
}
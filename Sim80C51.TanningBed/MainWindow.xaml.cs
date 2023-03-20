using Sim80C51.Interfaces;
using System.Windows;

namespace Sim80C51.TanningBed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IHardwareWindow
    {
        public MainWindowModel Model => (MainWindowModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Model.Loaded(this);
        }
    }
}

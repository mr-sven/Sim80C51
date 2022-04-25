using Sim80C51.Toolbox.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace Sim80C51
{
    /// <summary>
    /// Interaction logic for SelectRamWindow.xaml
    /// </summary>
    public partial class SelectRamWindow : Window
    {
        public SelectRamWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            IconHelper.RemoveIcon(this);
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public ushort RamStartAddress
        {
            get 
            {
                return Convert.ToUInt16(RamStart.Text, 16);
            }
            set 
            {
                RamStart.Text = value.ToString("X4");
            }
        }

        public bool EnableM48T
        {
            get { return M48TEnabled.IsChecked == true; }
        }

        public int RamSize
        {
            get { return int.TryParse((RamSizeSelect.SelectedValue as ComboBoxItem)?.Content?.ToString(), out int value) ? value : 0; }
        }

        public bool HideSize 
        {
            get { return SizePanel.Visibility != Visibility.Visible; }
            set { SizePanel.Visibility = value ? Visibility.Collapsed : Visibility.Visible; }            
        }
    }
}

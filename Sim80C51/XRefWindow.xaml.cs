using Sim80C51.Toolbox.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sim80C51
{
    /// <summary>
    /// Interaction logic for XRefWindow.xaml
    /// </summary>
    public partial class XRefWindow : Window
    {
        public List<ushort> XRefs { get; set; } = new();

        public ushort Target;

        public XRefWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconHelper.RemoveIcon(this);
        }

        private void Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is ListBoxItem item && item.DataContext is ushort xref)
            {
                Target = xref;
                DialogResult = true;
            }
        }
    }
}

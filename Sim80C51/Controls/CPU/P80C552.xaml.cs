using Sim80C51.Interfaces;
using System.Windows.Controls;

namespace Sim80C51.Controls.CPU
{
    /// <summary>
    /// Interaction logic for P80C552.xaml
    /// </summary>
    public partial class P80C552 : UserControl, ICPUControl
    {
        public P80C552()
        {
            InitializeComponent();
        }

        public I80C51? CPUContext => DataContext as I80C51;
    }
}

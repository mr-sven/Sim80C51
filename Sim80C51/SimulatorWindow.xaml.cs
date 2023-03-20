using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Sim80C51
{
    /// <summary>
    /// Interaction logic for SimulatorWindow.xaml
    /// </summary>
    public partial class SimulatorWindow : Window
    {
        public SimulatorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            (DataContext as SimulatorWindowContext)?.Loaded(this);
        }

        GridViewColumnHeader? _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void LabelView_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                ListSortDirection direction;
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    Sort((headerClicked.Column.DisplayMemberBinding as Binding)!.Path.Path, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            (DataContext as SimulatorWindowContext)?.LabelView?.SortDescriptions.Clear();
            (DataContext as SimulatorWindowContext)?.LabelView?.SortDescriptions.Add(new SortDescription(sortBy, direction));
            (DataContext as SimulatorWindowContext)?.LabelView?.Refresh();
        }

        private void LabelView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            (DataContext as SimulatorWindowContext) ?.NavToAddressCommand.Execute(((sender as ListViewItem)?.Content as Common.ListingEntry)?.Address);
        }
    }
}

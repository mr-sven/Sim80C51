using Sim80C51.Toolbox.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sim80C51.Controls
{
    /// <summary>
    /// Interaction logic for ListingEditor.xaml
    /// </summary>
    public partial class ListingEditor : UserControl
    {
        public ListingEditorContext Model => (DataContext as ListingEditorContext)!;
        public ListingEditor()
        {
            InitializeComponent();
        }

        private void ListView_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.B:
                    Model.BreakPointCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.C:
                    Model.CreateCodeCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.J:
                    Model.JumpCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.K:
                    Model.UpdateCommentCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.L:
                    Model.UpdateLabelCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.S:
                    Model.CreateStringCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.U:
                    Model.UndefineCommand.Execute(Model.SelectedListingEntry);
                    break;
                case Key.X:
                    Model.ShowXRefsCommand.Execute(Model.SelectedListingEntry);
                    break;
                default:
                    break;
            }
        }

        private void ListingView_KeyDown(object sender, KeyEventArgs e)
        {
            Key[] ignore = new[]
            {
                Key.B,
                Key.C,
                Key.J,
                Key.K,
                Key.L,
                Key.S,
                Key.U,
                Key.X,
            };

            if (ignore.Contains(e.Key))
            {
                e.Handled = true;
            }
        }

        private void ListingView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListingView.SelectedItem != null)
            {   
                ListingView.ScrollIntoView(ListingView.SelectedItem);
                UIElement selectedElement = (UIElement)ListingView.ItemContainerGenerator.ContainerFromItem(ListingView.SelectedItem);
                if (selectedElement != null)
                {
                    ListingView.ScrollIntoView(ListingView.SelectedItem);
                    selectedElement.Focus();
                }
            }
        }

        public bool IsItemVisible(int Index)
        {
            if (ListingView.Items.Count == 0)
            {
                return false;
            }

            UIElement lbi = (UIElement)ListingView.ItemContainerGenerator.ContainerFromIndex(0);

            if (VisualTreeHelper.GetParent(lbi) is VirtualizingStackPanel vsp)
            {
                int FirstVisibleItem = (int)vsp.VerticalOffset;
                int VisibleItemCount = (int)vsp.ViewportHeight;
                return Index >= FirstVisibleItem && Index <= FirstVisibleItem + VisibleItemCount;
            }

            return false;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu)
            {
                foreach (object? item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        (menuItem.Command as RelayCommand)?.OnCanExecuteChanged();
                    }
                }
            }
        }

        /*
        public bool IsListScrolledUp()            
        {
            if (ListingView.Items.Count == 0)
            {
                return false;
            }

            UIElement lbi = (UIElement)ListingView.ItemContainerGenerator.ContainerFromIndex(0);

            if (VisualTreeHelper.GetParent(lbi) is VirtualizingStackPanel vsp)
            {
                int FirstVisibleItem = (int)vsp.VerticalOffset;
                return FirstVisibleItem <= 0;
            }
            return false;
        }

        public bool IsListScrolledDown(ListBox lb)

        {

            if (lb.Items.Count == 0)

                return false;

            ListBoxItem lbi = lb.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;

            VirtualizingStackPanel vsp = VisualTreeHelper.GetParent(lbi) as VirtualizingStackPanel;

            int FirstVisibleItem = (int)vsp.VerticalOffset;

            int VisibleItemCount = (int)vsp.ViewportHeight;

            if (VisibleItemCount + FirstVisibleItem == lb.Items.Count)

                return true;

            return false;

        }*/
    }
}

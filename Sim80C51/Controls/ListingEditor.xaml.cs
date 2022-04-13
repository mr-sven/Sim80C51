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
        public ListingEditor()
        {
            InitializeComponent();
        }

        private void ListView_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.C:
                    (DataContext as ListingEditorContext)?.CreateCode();
                    break;
                case Key.L:
                    (DataContext as ListingEditorContext)?.UpdateLabel();
                    break;
                case Key.K:
                    (DataContext as ListingEditorContext)?.UpdateComment();
                    break;
                case Key.J:
                    (DataContext as ListingEditorContext)?.Jump();
                    break;
                case Key.B:
                    (DataContext as ListingEditorContext)?.BreakPoint();
                    break;
                case Key.X:
                    (DataContext as ListingEditorContext)?.ShowXRefs();
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

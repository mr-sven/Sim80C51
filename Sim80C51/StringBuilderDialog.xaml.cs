using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sim80C51.Toolbox.Wpf;

namespace Sim80C51
{
    /// <summary>
    /// Interaction logic for StringBuilderDialog.xaml
    /// </summary>
    public partial class StringBuilderDialog : Window
    {
        private readonly ushort baseAddress;

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public StringBuilderDialog(string displayString, int startIdx, int endIdx, ushort baseAddress)
        {
            InitializeComponent();
            this.baseAddress = baseAddress;
            textBox.Document.Blocks.Clear();
            textBox.Document.Blocks.Add(new Paragraph(new Run(displayString)));

            StartIndex = startIdx;
            EndIndex = endIdx;
            startIndex.Text = $"{baseAddress + startIdx:X4}";
            endIndex.Text = $"{baseAddress + endIdx:X4}";

            TextPointer text = textBox.Document.ContentStart;
            while (text.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                text = text.GetNextContextPosition(LogicalDirection.Forward);
            }

            new TextRange(text.GetPositionAtOffset(startIdx), text.GetPositionAtOffset(endIdx + 1)).ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Aqua);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconHelper.RemoveIcon(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd).ClearAllProperties();

            TextPointer text = textBox.Document.ContentStart;
            while (text.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                text = text.GetNextContextPosition(LogicalDirection.Forward);
            }
            int startIdx = text.GetOffsetToPosition(textBox.Selection.Start);
            int endIdx = text.GetOffsetToPosition(textBox.Selection.End) - 1;
            StartIndex = startIdx;
            EndIndex = endIdx;
            startIndex.Text = $"{baseAddress + startIdx:X4}";
            endIndex.Text = $"{baseAddress + endIdx:X4}";

            new TextRange(textBox.Selection.Start, textBox.Selection.End).ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Aqua);
        }
    }
}

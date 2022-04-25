using Microsoft.Win32;
using Sim80C51.Toolbox.Wpf;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Sim80C51.Controls
{
    public class MemoryContext : NotifyPropertyChanged
    {
        public const int M48T_ADDRESS_YEAR = -1;
        public const int M48T_ADDRESS_MONTH = -2;
        public const int M48T_ADDRESS_DATE = -3;
        public const int M48T_ADDRESS_DAY = -4;
        public const int M48T_ADDRESS_HOURS = -5;
        public const int M48T_ADDRESS_MINUTES = -6;
        public const int M48T_ADDRESS_SECONDS = -7;
        public const int M48T_ADDRESS_CONTROL = -8;

        public const byte M48T_MASK_STOP = 0x80;
        public const byte M48T_MASK_WRITE = 0x80;
        public const byte M48T_MASK_READ = 0x40;

        public ObservableCollection<ByteRow>? Memory { get; set; }

        public bool MarkUpperInternalRam { get; set; } = false;

        public long Size { get; set; } = 0;

        private readonly System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private int memorySize = 0;

        public bool M48TMode => dispatcherTimer.IsEnabled;

        public ICommand SaveMemoryCommand { get; }

        public MemoryContext()
        {
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            SaveMemoryCommand = new RelayCommand(SaveMemoryCommandExecute);
        }

        private void SaveMemoryCommandExecute(object? obj)
        {
            SaveFileDialog saveFileDialog = new()
            {
                DefaultExt = "bin",
                Filter = "Binary Files (*.bin)|*.bin",
                Title = "Save Memory",
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow) == false)
            {
                return;
            }

            using FileStream file = File.OpenWrite(saveFileDialog.FileName);
            foreach (ByteRow row in Memory!)
            {
                file.Write(row.Row.ToArray());
            }
        }

        public byte this[int i]
        {
            get { return Memory![i / ByteRow.ROW_WIDTH][i % ByteRow.ROW_WIDTH]; }
            set { Memory![i / ByteRow.ROW_WIDTH][i % ByteRow.ROW_WIDTH] = value; }
        }

        private static byte SplitDateValue(int value)
        {
            return (byte)((value / 10) << 4 | (value % 10));
        }

        private void CheckSetDateAddress(int date_address_offset, byte value)
        {
            this[memorySize + date_address_offset] = value;
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            if (Memory == null)
            {
                return;
            }

            if ((this[memorySize + M48T_ADDRESS_SECONDS] & M48T_MASK_STOP) == M48T_MASK_STOP ||
                (this[memorySize + M48T_ADDRESS_CONTROL] & M48T_MASK_READ) == M48T_MASK_READ ||
                (this[memorySize + M48T_ADDRESS_CONTROL] & M48T_MASK_WRITE) == M48T_MASK_WRITE)
            {
                return;
            }

            CheckSetDateAddress(M48T_ADDRESS_YEAR, SplitDateValue(DateTime.Now.Year % 100));
            CheckSetDateAddress(M48T_ADDRESS_MONTH, SplitDateValue(DateTime.Now.Month));
            CheckSetDateAddress(M48T_ADDRESS_DATE, SplitDateValue(DateTime.Now.Day));
            CheckSetDateAddress(M48T_ADDRESS_DAY, (byte)DateTime.Now.DayOfWeek);
            CheckSetDateAddress(M48T_ADDRESS_HOURS, SplitDateValue(DateTime.Now.Hour));
            CheckSetDateAddress(M48T_ADDRESS_MINUTES, SplitDateValue(DateTime.Now.Minute));
            CheckSetDateAddress(M48T_ADDRESS_SECONDS, SplitDateValue(DateTime.Now.Second));
        }

        public void StartM48TMode()
        {
            if (Memory == null)
            {
                return;
            }

            memorySize = Memory.Count * ByteRow.ROW_WIDTH;
            dispatcherTimer.Start();
        }
    }
}

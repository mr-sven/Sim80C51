using Sim80C51.Toolbox.Wpf;
using System.Collections.ObjectModel;
using System.IO;

namespace Sim80C51
{
    public class ByteRow : NotifyPropertyChanged
    {
        public const int ROW_WIDTH = 16;

        public ObservableCollection<byte> Row { get => row; }
        private readonly ObservableCollection<byte> row;

        public int Index { get; }

        public string Ansi 
        { 
            get
            {
                string str = System.Text.Encoding.Default.GetString(Row.ToArray());
                return new string(str.Select(c => c < 0x20 ? '.' : c > 0x7f ? '.' : c).ToArray());
            }
        }

        public byte this[int i]
        {
            get { return Row[i]; }
            set { if (Row[i] != value) { Row[i] = value; DoPropertyChanged(nameof(Ansi)); } }
        }

        public ByteRow(int index, byte initValue = 0x00)
        {
            Index = index;
            row = new(Enumerable.Repeat(initValue, ROW_WIDTH).ToArray());
            Row.CollectionChanged += Row_CollectionChanged;
        }

        public ByteRow(int index, byte[] data)
        {
            Index = index;
            if (data.Length >= ROW_WIDTH)
            {
                row = new(data[..ROW_WIDTH]);
            }
            else
            {
                row = new(data);
                while (row.Count < ROW_WIDTH)
                {
                    row.Add(0x00);
                }
            }
            Row.CollectionChanged += Row_CollectionChanged;
        }

        private void Row_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DoPropertyChanged(nameof(Ansi));
        }

        public void UpdateFromBuffer(byte[] buf)
        {
            for (int i = 0; i < buf.Length && i < ROW_WIDTH; i++)
            {
                Row[i] = buf[i];
            }
        }

        public static ByteRow[] InitRows(int size, bool enableMT48 = false)
        {
            ByteRow[] result = new ByteRow[size / ROW_WIDTH];
            for (int i = 0; i < result.Length; i++)
            {
                if (i < result.Length / 2 || !enableMT48)
                {
                    result[i] = new ByteRow(i);
                }
                else if(i == result.Length-1)
                {
                    result[i] = new ByteRow(i, Enumerable.Repeat((byte)0xff, ROW_WIDTH / 2).ToArray());
                }
                else
                {
                    result[i] = new ByteRow(i, 0xff);
                }
            }
            return result;
        }

        public static ByteRow[] FromStream(Stream stream)
        {
            List<ByteRow> res = new();

            byte[] buf = new byte[ROW_WIDTH];
            int row = 0;

            while ((_ = stream.Read(buf, 0, buf.Length)) > 0)
            {
                res.Add(new(row++, buf));
            }

            return res.ToArray();
        }

        public static MemoryStream ToMemoryStream(IEnumerable<ByteRow> Rows)
        {
            MemoryStream ms = new();
            foreach (ByteRow row in Rows)
            {
                ms.Write(row.Row.ToArray());
            }
            ms.Position = 0;
            return ms;
        }
    }
}

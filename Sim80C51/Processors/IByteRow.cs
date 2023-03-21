using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Sim80C51.Processors
{
    public interface IByteRow : INotifyPropertyChanged
    {
        ObservableCollection<byte> Row { get; }
        int Index { get; }
        byte this[int index] { get; set; }
        string Ansi { get; }
        void UpdateFromBuffer(byte[] buf);
    }
}
using System.ComponentModel;

namespace Sim80C51.Interfaces
{
    public interface I80C51 : INotifyPropertyChanged
    {
        /// <summary>
        /// Program Counter
        /// </summary>
        ushort PC { get; set; }
        /// <summary>
        /// Cycle Counter
        /// </summary>
        ulong Cycles { get; set; }

        /// <summary>
        /// Data Pointer
        /// </summary>
        ushort DPTR { get; set; }

        /// <summary>
        /// Port 1
        /// </summary>
        byte P1 { get; set; }

        // TODO: add more registers as required
    }
}
using Sim80C51.Common;
using System.Collections.ObjectModel;

namespace Sim80C51.Processors
{
    public interface I80C51
    {
        /// <summary>
        /// Internal RAM Address space
        /// </summary>
        ObservableCollection<ByteRow> CoreMemory { get; }

        /// <summary>
        /// Program Counter
        /// </summary>
        ushort PC { get; set; }

        /// <summary>
        /// Call Stack
        /// </summary>
        ObservableCollection<ushort> CallStack { get; }

        /// <summary>
        /// Cycle Counter
        /// </summary>
        ulong Cycles { get; set; }

        /// <summary>
        /// Data Pointer
        /// </summary>
        ushort DPTR { get; set; }

        // TODO: add more registers as required

        /// <summary>
        /// Callback for MOVC calls to get code byte
        /// </summary>
        Func<ushort, byte>? GetCodeByte { get; set; }

        /// <summary>
        /// Callback for MOVX to get byte from XRAM
        /// </summary>
        Func<ushort, byte>? GetRamByte { get; set; }

        /// <summary>
        /// Callback for MOVX to write byte to XRAM
        /// </summary>
        Action<ushort, byte>? SetRamByte { get; set; }

        /// <summary>
        /// Process listing entry.
        /// May throw exceptions in case of wrong listing entrys
        /// </summary>
        /// <param name="entry">the listing entry</param>
        void Process(ListingEntry entry);

        void Reset();

        void RefreshUIProperies();

        void SaveAdditionalSettings(Dictionary<string, object> additionalSettings);
        void LoadAdditionalSettings(Dictionary<string, object> additionalSettings);
    }
}
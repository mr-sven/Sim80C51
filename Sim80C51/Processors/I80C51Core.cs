using Sim80C51.Interfaces;
using System.Collections.ObjectModel;

namespace Sim80C51.Processors
{
    public interface I80C51Core : I80C51
    {
        /// <summary>
        /// Internal RAM Address space
        /// </summary>
        ObservableCollection<IByteRow> CoreMemory { get; }

        /// <summary>
        /// Call Stack
        /// </summary>
        ObservableCollection<ICallStackEntry> CallStack { get; }

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
        void Process(IListingInstruction entry);

        /// <summary>
        /// Chip Reset, resets all registers and set PC to reset vector 0
        /// </summary>
        void Reset();

        void RefreshUIProperies();

        void SaveAdditionalSettings(IDictionary<string, object> additionalSettings);
        void LoadAdditionalSettings(IDictionary<string, object> additionalSettings);
    }
}
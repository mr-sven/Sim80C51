using Sim80C51.Interfaces;
using Sim80C51.Processors;
using Sim80C51.Toolbox.Wpf;

namespace Sim80C51.Common
{
    public class ListingEntry : NotifyPropertyChanged, IListingInstruction
    {
        public ushort Address { get => address; set { address = value; DoPropertyChanged(); } }
        private ushort address;

        public List<byte> Data { get => data; set { data = value; DoPropertyChanged(); } }
        private List<byte> data = [];

        public string DataString { get => dataString; private set { dataString = value; DoPropertyChanged(); } }
        private string dataString = string.Empty;

        public string? Label { get => label; set { label = value; DoPropertyChanged(); } }
        private string? label;

        public InstructionType Instruction { get => instruction; set { instruction = value; DoPropertyChanged(); } }
        private InstructionType instruction;

        public List<string> Arguments { get => arguments; set { arguments = value; DoPropertyChanged(); } }
        private List<string> arguments = [];

        public string ArgumentString { get => argumentString; private set { argumentString = value; DoPropertyChanged(); } }
        private string argumentString = string.Empty;

        public string? Comment { get => comment; set { comment = value; DoPropertyChanged(); } }
        private string? comment;

        // for call and jump
        public ushort TargetAddress { get => targetAddress; set { targetAddress = value; DoPropertyChanged(); } }
        private ushort targetAddress;

        public ushort Length => (ushort)Data.Count;

        public void UpdateStrings()
        {
            DataString = BitConverter.ToString([.. Data]).Replace('-', ' ');
            ArgumentString = string.Join(", ", arguments);
        }

        public override string ToString()
        {
            string result = Address.ToString("X4") + "  ";
            result += $"{DataString,-23}  ";
            result += string.Format("{0,-21}", string.IsNullOrEmpty(label) ? "" : label + ": ");
            result += $"{Instruction,-6}";
            result += $"{ArgumentString,-21}";
            result += string.IsNullOrEmpty(Comment) ? "" : " ; " + Comment;
            return result;
        }
    }
}

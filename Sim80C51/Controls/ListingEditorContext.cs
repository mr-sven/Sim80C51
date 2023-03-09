using Sim80C51.Common;
using Sim80C51.Toolbox.Wpf;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace Sim80C51.Controls
{
    public class ListingEditorContext : NotifyPropertyChanged
    {
        private static readonly InstructionType[] jumpInstructions = new[] {
            //Instruction.JMP, Address based Jump
            InstructionType.LJMP,
            InstructionType.AJMP,
            InstructionType.SJMP,
            InstructionType.ACALL,
            InstructionType.LCALL,
            InstructionType.DJNZ,
            InstructionType.JBC,
            InstructionType.JB,
            InstructionType.JNB,
            InstructionType.CJNE,
            InstructionType.JC,
            InstructionType.JNC,
            InstructionType.JZ,
            InstructionType.JNZ
        };

        private ListingFactory? factory;
        private BinaryReader? reader;

        public ListingCollection Listing { get; } = new();

        public ListingEntry? SelectedListingEntry { get => selectedListingEntry; set { selectedListingEntry = value; DoPropertyChanged(); } }
        private ListingEntry? selectedListingEntry;

        public ushort HighlightAddress { get => highlightAddress; set { highlightAddress = value; DoPropertyChanged(); } }
        private ushort highlightAddress = 0;

        public Action<ushort>? AddBreakPoint { get ; set; }

        public ListingEditorContext()
        {
        }

        #region UI Calls
        public void CreateCode()
        {
            if (SelectedListingEntry?.Instruction != InstructionType.DB || reader == null || factory == null)
            {
                return;
            }

            string? label = factory.TryGetIVLabel(SelectedListingEntry.Address);

            if (label == null)
            {
                InputDialog dlg = new("Set Label", "New Label:", $"code_{SelectedListingEntry.Address:X4}") { Owner = Application.Current.MainWindow };
                if (dlg.ShowDialog() == false)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(dlg.Answer))
                {
                    label = dlg.Answer;
                }
            }

            ushort currentAddress = SelectedListingEntry.Address;
            SelectedListingEntry = null;
            reader.BaseStream.Position = currentAddress;
            factory.Build(reader, label);

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

            SelectedListingEntry = Listing.GetByAddress(currentAddress);
        }

        public void UpdateLabel()
        {
            if (SelectedListingEntry == null)
            {
                return;
            }

            InputDialog dlg = new("Update Label", "Label:", SelectedListingEntry.Label ?? string.Empty) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() == false)
            {
                return;
            }

            // check updates
            if (string.IsNullOrEmpty(dlg.Answer) || dlg.Answer == SelectedListingEntry.Label)
            {
                return;
            }

            // check if exists
            if (Listing.GetByLabel(dlg.Answer) != null)
            {
                MessageBox.Show($"Label '{dlg.Answer}' exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Listing.SetLabel(SelectedListingEntry, dlg.Answer);
        }

        public void UpdateComment()
        {
            if (SelectedListingEntry == null)
            {
                return;
            }
            InputDialog dlg = new("Update Comment", "Comment:", SelectedListingEntry.Comment ?? string.Empty) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() == false)
            {
                return;
            }
            SelectedListingEntry.Comment = dlg.Answer;
        }

        public void Jump()
        {
            if (SelectedListingEntry == null)
            {
                return;
            }

            if (jumpInstructions.Contains(SelectedListingEntry.Instruction))
            {
                string targetLabel = SelectedListingEntry.Arguments.Last();
                SelectedListingEntry = Listing.GetByLabel(targetLabel);
            }
        }

        public void BreakPoint()
        {
            if (SelectedListingEntry == null)
            {
                return;
            }

            AddBreakPoint?.Invoke(SelectedListingEntry.Address);
        }

        public void ShowXRefs()
        {
            if (SelectedListingEntry == null || string.IsNullOrEmpty(SelectedListingEntry.Label))
            {
                return;
            }

            List<ListingEntry> xrefs = Listing.Where(e => e.Arguments.Count > 0 && e.Arguments.Last() == SelectedListingEntry.Label).ToList();

            //TODO: Show xref window
        }
        #endregion

        public ListingEntry? GetFromAddress(ushort address)
        {
            return Listing.GetByAddress(address);
        }

        public byte GetCodeByte(ushort address)
        {
            reader!.BaseStream.Position = address;
            return reader.ReadByte();
        }

        public void SetProcessorType(Type processorType)
        {
            factory = new(processorType);
            factory.WithListing(Listing);
        }

        #region Load and save Listing or binary
        public void LoadFromListingStream(Stream stream)
        {
            if (factory == null)
            {
                return;
            }

            Regex listingLineMatch = new(@"^(?<address>[0-9a-f]{4})  (?<data>([0-9a-f]{2} )+) +((?<label>[^ :]+): +)?(?<instruction>\w+) +(?<args>[^;]*)(;(?<comment>.*))?.*$", RegexOptions.IgnoreCase);

            using StreamReader sr = new(stream);
            string? line;

            int memsize = 0;
            Listing.Clear();
            while ((line = sr.ReadLine()) != null)
            {
                Match match = listingLineMatch.Match(line);
                if (match.Success)
                {
                    ListingEntry entry = new()
                    {
                        Address = Convert.ToUInt16(match.Groups["address"].Value, 16),
                        Data = match.Groups["data"].Value.Trim().Split(' ').Select(s => Convert.ToByte(s, 16)).ToList(),
                        Instruction = Enum.Parse<InstructionType>(match.Groups["instruction"].Value)
                    };
                    if (match.Groups.ContainsKey("label"))
                    {
                        entry.Label = match.Groups["label"].Value;
                    }
                    if (match.Groups.ContainsKey("args"))
                    {
                        entry.Arguments = match.Groups["args"].Value.Trim().Split(',').Select(s => s.Trim()).ToList();
                    }
                    if (match.Groups.ContainsKey("comment"))
                    {
                        entry.Comment = match.Groups["comment"].Value.Trim();
                    }
                    entry.UpdateStrings();

                    memsize = Math.Max(memsize, entry.Address + entry.Data.Count);

                    Listing.Add(entry);
                }
            }

            foreach (ListingEntry entry in Listing.Where(l => jumpInstructions.Contains(l.Instruction)))
            {
                string targetLabel = entry.Arguments.Last();
                if (Listing.GetByLabel(targetLabel) is ListingEntry target)
                {
                    entry.TargetAddress = target.Address;
                }
            }

            reader = ReaderFromListing(memsize);
        }

        public void LoadRomBinaryFile(string fileName)
        {
            if (factory == null)
            {
                return;
            }

            reader = new(new MemoryStream(File.ReadAllBytes(fileName)));
            Listing.Clear();
            factory.Build(reader);
        }

        public void LoadRomHexFile(string fileName)
        {
            if (factory == null)
            {
                return;
            }

            reader = ReaderFromHexFile(fileName);
            Listing.Clear();
            factory.Build(reader);
        }

        private static BinaryReader ReaderFromHexFile(string fileName)
        {
            StreamReader sr = new(fileName);
            IntelHexStructure? hex;
            int memsize = 0;
            while ((hex = IntelHex.Read(sr)) != null)
            {
                if (hex.type == IntelHex.IHEX_TYPE_DATA)
                {
                    memsize = Math.Max(memsize, hex.address + hex.dataLen);
                }
                else if (hex.type == IntelHex.IHEX_TYPE_EOF)
                {
                    break;
                }
                else
                {
                    throw new Exception("Invalid Hex file format");
                }
            }

            byte[] buffer = Enumerable.Repeat((byte)0xff, memsize).ToArray();

            sr.BaseStream.Position = 0;
            while ((hex = IntelHex.Read(sr)) != null)
            {
                if (hex.type == IntelHex.IHEX_TYPE_DATA)
                {
                    Array.Copy(hex.data, 0, buffer, hex.address, hex.dataLen);
                }
                else if (hex.type == IntelHex.IHEX_TYPE_EOF)
                {
                    break;
                }
                else
                {
                    throw new Exception("Invalid Hex file format");
                }
            }

            return new(new MemoryStream(buffer));
        }

        private BinaryReader ReaderFromListing(int memsize)
        {
            byte[] buffer = Enumerable.Repeat((byte)0xff, memsize).ToArray();

            foreach (ListingEntry entry in Listing)
            {
                Array.Copy(entry.Data.ToArray(), 0, buffer, entry.Address, entry.Data.Count);
            }

            return new(new MemoryStream(buffer));
        }

        public void SaveListingToStream(Stream stream)
        {
            using StreamWriter sw = new(stream, leaveOpen: true);
            foreach (ListingEntry entry in Listing.OrderBy(l => l.Address))
            {
                sw.WriteLine(entry.ToString());
            }
        }
        #endregion
    }
}

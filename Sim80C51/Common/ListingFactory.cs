using Sim80C51.Processors;
using Sim80C51.Processors.Attributes;
using Sim80C51.Toolbox;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Sim80C51.Common
{
    public class ListingFactory
    {
        public const byte CMD_NOP = 0x00;
        public const byte CMD_AJMP = 0x01;
        public const byte CMD_LJMP = 0x02;
        public const byte CMD_RR = 0x03;
        public const byte CMD_INC = 0x04;
        public const byte CMD_JBC = 0x10;
        public const byte CMD_ACALL = 0x11;
        public const byte CMD_LCALL = 0x12;
        public const byte CMD_RRC = 0x13;
        public const byte CMD_DEC = 0x14;
        public const byte CMD_JB = 0x20;
        public const byte CMD_RET = 0x22;
        public const byte CMD_RL = 0x23;
        public const byte CMD_ADD = 0x24;
        public const byte CMD_JNB = 0x30;
        public const byte CMD_RETI = 0x32;
        public const byte CMD_RLC = 0x33;
        public const byte CMD_ADDC = 0x34;
        public const byte CMD_JC = 0x40;
        public const byte CMD_ORL = 0x42;
        public const byte CMD_ANL = 0x52;
        public const byte CMD_JNC = 0x50;
        public const byte CMD_JZ = 0x60;
        public const byte CMD_XRL = 0x62;
        public const byte CMD_JNZ = 0x70;
        public const byte CMD_ORL_C = 0x72;
        public const byte CMD_JMP = 0x73;
        public const byte CMD_MOV_DATA = 0x74;
        public const byte CMD_SJMP = 0x80;
        public const byte CMD_ANL_C = 0x82;
        public const byte CMD_MOVC_PC = 0x83;
        public const byte CMD_DIV = 0x84;
        public const byte CMD_MOV_IRAM = 0x85;
        public const byte CMD_MOV_DPTR = 0x90;
        public const byte CMD_MOV_BIT = 0x92;
        public const byte CMD_MOVC_DPTR = 0x93;
        public const byte CMD_SUBB = 0x94;
        public const byte CMD_ORL_C2 = 0xA0;
        public const byte CMD_MOV_C = 0xA2;
        public const byte CMD_INC_DPTR = 0xA3;
        public const byte CMD_MUL = 0xA4;
        public const byte CMD_MOV_IRAM2 = 0xA6;
        public const byte CMD_ANL_C2 = 0xB0;
        public const byte CMD_CPL = 0xB2;
        public const byte CMD_CJNE = 0xB4;
        public const byte CMD_PUSH = 0xC0;
        public const byte CMD_CLR = 0xC2;
        public const byte CMD_SWAP = 0xC4;
        public const byte CMD_XCH = 0xC5;
        public const byte CMD_POP = 0xD0;
        public const byte CMD_SETB = 0xD2;
        public const byte CMD_DA = 0xD4;
        public const byte CMD_DJNZ_IRAM = 0xD5;
        public const byte CMD_XCHD = 0xD6;
        public const byte CMD_DJNZ = 0xD8;
        public const byte CMD_MOVX = 0xE0;
        public const byte CMD_CLR_A = 0xE4;
        public const byte CMD_MOV_A = 0xE5;
        public const byte CMD_MOVX_A = 0xF0;
        public const byte CMD_CPL_A = 0xF4;
        public const byte CMD_MOV_A2 = 0xF5;

        private const int DB_DEFAULT_SIZE = 8;

        public ListingCollection? Listing => listing;
        private ListingCollection? listing;

        private readonly Dictionary<string, SFRBitAttribute> ivBits = new();
        private readonly Dictionary<ushort, string> reverseSfrMap = new();
        private readonly Dictionary<ushort, string> reverseSfrBitMap = new();
        private readonly Dictionary<string, ushort> ivMap = new();

        public ListingFactory(Type processorType)
        {
            if (!processorType.GetInterfaces().Contains(typeof(Interfaces.I80C51)))
            {
                throw new ArgumentException("Type not implementing interface I80C51", nameof(processorType));
            }

            // build first register map
            foreach (PropertyInfo pInfo in processorType.GetProperties())
            {
                if (pInfo.GetCustomAttribute<SFRAttribute>() is SFRAttribute sfrAttr)
                {
                    reverseSfrMap.Add(sfrAttr.Address, pInfo.Name);

                    // ports 0-4 are also direct addressable
                    if (pInfo.Name == "P0" || pInfo.Name == "P1" || pInfo.Name == "P2" || pInfo.Name == "P3" || pInfo.Name == "P4")
                    {
                        for(int i = 0; i < 8; i++)
                        {
                            reverseSfrBitMap.Add((ushort)(sfrAttr.Address + i), pInfo.Name + "." + i);
                        }
                    }
                }
            }

            // build bitmap
            foreach (PropertyInfo pInfo in processorType.GetProperties())
            {
                if (pInfo.GetCustomAttribute<SFRBitAttribute>() is SFRBitAttribute sfrBitAttr && sfrBitAttr.Addressable)
                {
                    ushort regAddr = reverseSfrMap.FirstOrDefault(sf => sf.Value == sfrBitAttr.SFRName).Key;
                    reverseSfrBitMap.Add((ushort)(regAddr + sfrBitAttr.Bit), pInfo.Name);

                    if ((sfrBitAttr.SFRName == "IEN0" || sfrBitAttr.SFRName == "IEN1") && pInfo.Name != "EA")
                    {
                        ivBits.Add(pInfo.Name, sfrBitAttr);
                    }
                }
            }

            // build IV Map
            foreach (IVAttribute attr in processorType.GetCustomAttributes<IVAttribute>())
            {
                ivMap.Add(attr.Name, attr.Address);
            }
        }

        public ListingFactory WithListing(ListingCollection listing)
        {
            this.listing = listing;
            return this;
        }

        public string? TryGetIVLabel(ushort address)
        {
            if (ivMap.Any(i => i.Value == address))
            {
                return ivMap.FirstOrDefault(i => i.Value == address).Key;
            }
            return null;
        }

        public ListingCollection Build(BinaryReader br, string? label = "RESET")
        {
            listing ??= new();

            Stack<ListingEntry> branches = new();

            ListingEntry entry;

            ushort lastDptr = 0;

            try
            {
                while ((entry = ParseEntry(br, label)) != null)
                {
                    RemoveOverlappingDbEntries(entry.Address, (ushort)(entry.Address + entry.Data.Count));

                    if (!listing.Contains(entry.Address))
                    {
                        listing.Add(entry);
                    }

                    label = null;
                    sbyte signedAddress;

                    // collect calls, jumps and branches
                    switch (entry.Instruction)
                    {
                        case InstructionType.JMP:
                            // assume code jumps should be in range of 20
                            if (Math.Abs(entry.Address - lastDptr) < 20)
                            {
                                br.BaseStream.Position = lastDptr;
                                label = GetLabelForAddress(lastDptr);                                
                            }
                            break;
                        case InstructionType.LJMP:
                            br.BaseStream.Position = entry.TargetAddress;
                            label = GetLabelForAddress(entry.TargetAddress);
                            entry.Arguments.Add(label);
                            break;

                        case InstructionType.AJMP:
                            entry.TargetAddress = (ushort)((br.BaseStream.Position & 0xF800) | entry.TargetAddress);
                            br.BaseStream.Position = entry.TargetAddress;
                            label = GetLabelForAddress(entry.TargetAddress);
                            entry.Arguments.Add(label);
                            break;

                        case InstructionType.SJMP:
                            signedAddress = unchecked((sbyte)entry.TargetAddress);
                            entry.TargetAddress = (ushort)(br.BaseStream.Position + signedAddress);
                            br.BaseStream.Position = entry.TargetAddress;
                            label = GetLabelForAddress(entry.TargetAddress);
                            entry.Arguments.Add(label);
                            break;

                        case InstructionType.ACALL:
                            entry.TargetAddress = (ushort)((br.BaseStream.Position & 0xF800) | entry.TargetAddress);
                            entry.Arguments.Add(GetLabelForAddress(entry.TargetAddress));
                            branches.Push(entry);
                            break;

                        case InstructionType.LCALL:
                            entry.Arguments.Add(GetLabelForAddress(entry.TargetAddress));
                            branches.Push(entry);
                            break;

                        case InstructionType.DJNZ:
                        case InstructionType.JBC:
                        case InstructionType.JB:
                        case InstructionType.JNB:
                        case InstructionType.CJNE:
                        case InstructionType.JC:
                        case InstructionType.JNC:
                        case InstructionType.JZ:
                        case InstructionType.JNZ:
                            signedAddress = unchecked((sbyte)entry.TargetAddress);
                            entry.TargetAddress = (ushort)(br.BaseStream.Position + signedAddress);
                            entry.Arguments.Add(GetLabelForAddress(entry.TargetAddress));
                            branches.Push(entry);
                            break;

                        // Track MOV to IEN Registers for later IV Scan
                        case InstructionType.MOV:
                            if (entry.Arguments[0] == "IEN0")
                            {
                                CheckAddIv("IEN0", entry.Arguments[1], 7, branches);
                            }
                            else if (entry.Arguments[0] == "IEN1")
                            {
                                CheckAddIv("IEN1", entry.Arguments[1], 8, branches);
                            }

                            // store last DPTR Value for code Jumps (JMP)
                            if (entry.Arguments[0] == "DPTR")
                            {
                                lastDptr = C80C51.ParseIntermediateUShort(entry.Arguments[1]);
                            }
                            break;

                        // Track SETB to IEN Registers for later IV Scan
                        case InstructionType.SETB:
                            if (ivBits.ContainsKey(entry.Arguments[0]))
                            {
                                string ivName = entry.Arguments[0][1..];
                                if (ivMap.ContainsKey(ivName))
                                {
                                    branches.Push(new ListingEntry()
                                    {
                                        TargetAddress = ivMap[ivName],
                                        Arguments = new() { ivName }
                                    });
                                }
                            }
                            break;
                    }

                    entry.UpdateStrings();

                    ushort address = (ushort)br.BaseStream.Position;

                    RemoveStepDbEntry(address);

                    // check if next listing exists or function return to scan next branch from list
                    if (listing.Contains(address) || entry.Instruction == InstructionType.RET || entry.Instruction == InstructionType.RETI)
                    {
                        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(listing.GetByAddress(address)?.Label))
                        {
                            listing.SetLabel(address, label);
                        }

                        bool breakParser = true;
                        while (branches.Count > 0)
                        {
                            ListingEntry branch = branches.Pop();
                            label = branch.Arguments.Last();

                            RemoveStepDbEntry(branch.TargetAddress);

                            if (listing.GetByAddress(branch.TargetAddress) is ListingEntry listingEntry && string.IsNullOrEmpty(listingEntry.Label))
                            {
                                listing.SetLabel(branch.TargetAddress, label);
                            }
                            else if (!listing.Contains(branch.TargetAddress))
                            {
                                address = branch.TargetAddress;
                                br.BaseStream.Position = branch.TargetAddress;
                                breakParser = false;
                                break;
                            }
                        }

                        if (breakParser)
                        {
                            break;
                        }
                    }
                }

                RestoreDbEntries(br);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return listing;
        }

        private string GetLabelForAddress(ushort address)
        {
            if (listing!.GetByAddress(address) is ListingEntry entry && !string.IsNullOrEmpty(entry.Label))
            {
                return entry.Label;
            }
            return $"code_{address:X4}";
        }

        private void RemoveStepDbEntry(ushort address)
        {
            // check if next address is a DB, if yes remove
            if (listing!.GetByAddress(address) is ListingEntry entry && entry.Instruction == InstructionType.DB)
            {
                listing!.RemoveByAddress(address);
            }
        }

        private void RemoveOverlappingDbEntries(ushort start, ushort end)
        {
            ListingEntry[]? overlap = listing!.Where(e => e.Instruction == InstructionType.DB &&
                start < e.Address + e.Data.Count &&
                e.Address < end)?.ToArray();

            if (overlap == null || overlap.Length == 0)
            {
                return;
            }

            foreach(ListingEntry entry in overlap)
            {
                listing!.Remove(entry);
            }
        }

        private void CheckAddIv(string IEN, string valueStr, int maxBits, Stack<ListingEntry> branches)
        {
            byte value = C80C51.ParseIntermediateByte(valueStr);
            for (int i = 0; i < maxBits; i++)
            {
                string ivName = ivBits.FirstOrDefault(v => v.Value.SFRName == IEN && v.Value.Bit == i).Key;
                ivName = ivName[1..];
                byte mask = (byte)(1 << i);
                if ((value & mask) != mask)
                {
                    continue;
                }

                if (!ivMap.ContainsKey(ivName))
                {
                    continue;
                }

                branches.Push(new ListingEntry()
                {
                    TargetAddress = ivMap[ivName],
                    Arguments = new() { ivName }
                });
            }
        }

        public ListingEntry ParseEntry(BinaryReader br, string? label = null)
        {
            ushort address = (ushort)br.BaseStream.Position;
            byte instruction = br.ReadByte();

            ListingEntry result = new()
            {
                Address = address,
                Label = label,
                Data = new() { instruction }
            };

            switch (instruction)
            {
                case CMD_NOP:
                    result.Instruction = InstructionType.NOP;
                    break;

                case CMD_RET:
                    result.Instruction = InstructionType.RET;
                    break;

                case CMD_RETI:
                    result.Instruction = InstructionType.RETI;
                    break;

                #region Jumps
                case CMD_AJMP: // page0
                case CMD_AJMP + 0x20: // page1
                case CMD_AJMP + 0x40: // page2
                case CMD_AJMP + 0x60: // page3
                case CMD_AJMP + 0x80: // page4
                case CMD_AJMP + 0xA0: // page5
                case CMD_AJMP + 0xC0: // page6
                case CMD_AJMP + 0xE0: // page7
                    result.Instruction = InstructionType.AJMP;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = (ushort)((instruction & 0xe0) << 3 | result.Data[1]);
                    break;

                case CMD_LJMP:
                    result.Instruction = InstructionType.LJMP;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = (ushort)(result.Data[1] << 8 | result.Data[2]);
                    break;

                case CMD_SJMP:
                    result.Instruction = InstructionType.SJMP;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    break;

                case CMD_JMP:
                    result.Instruction = InstructionType.JMP;
                    result.Arguments.Add("@A+DPTR");
                    break;
                #endregion

                case CMD_ACALL: // page0
                case CMD_ACALL + 0x20: // page1
                case CMD_ACALL + 0x40: // page2
                case CMD_ACALL + 0x60: // page3
                case CMD_ACALL + 0x80: // page4
                case CMD_ACALL + 0xA0: // page5
                case CMD_ACALL + 0xC0: // page6
                case CMD_ACALL + 0xE0: // page7
                    result.Instruction = InstructionType.ACALL;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = (ushort)((instruction & 0xe0) << 3 | result.Data[1]);
                    break;

                case CMD_LCALL:
                    result.Instruction = InstructionType.LCALL;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = (ushort)(result.Data[1] << 8 | result.Data[2]);
                    break;

                case CMD_RR:
                    result.Instruction = InstructionType.RR;
                    result.Arguments.Add("A");
                    break;

                case CMD_RRC:
                    result.Instruction = InstructionType.RRC;
                    result.Arguments.Add("A");
                    break;

                case CMD_RL:
                    result.Instruction = InstructionType.RL;
                    result.Arguments.Add("A");
                    break;

                case CMD_RLC:
                    result.Instruction = InstructionType.RLC;
                    result.Arguments.Add("A");
                    break;

                case CMD_DIV:
                    result.Instruction = InstructionType.DIV;
                    result.Arguments.Add("AB");
                    break;

                case CMD_MUL:
                    result.Instruction = InstructionType.MUL;
                    result.Arguments.Add("AB");
                    break;

                case CMD_SWAP:
                    result.Instruction = InstructionType.SWAP;
                    result.Arguments.Add("A");
                    break;

                case CMD_DA:
                    result.Instruction = InstructionType.DA;
                    result.Arguments.Add("A");
                    break;

                case CMD_PUSH:
                    result.Instruction = InstructionType.PUSH;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_POP:
                    result.Instruction = InstructionType.POP;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_XCHD: // R0
                case CMD_XCHD + 0x01: // R1
                    result.Instruction = InstructionType.XCHD;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                #region CPL // complete
                case CMD_CPL:
                    result.Instruction = InstructionType.CPL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_CPL + 0x01: // C
                    result.Instruction = InstructionType.CPL;
                    result.Arguments.Add("C");
                    break;

                case CMD_CPL_A:
                    result.Instruction = InstructionType.CPL;
                    result.Arguments.Add("A");
                    break;
                #endregion

                #region MOVC // complete
                case CMD_MOVC_PC:
                    result.Instruction = InstructionType.MOVC;
                    result.Arguments.Add("A");
                    result.Arguments.Add("@A+PC");
                    break;

                case CMD_MOVC_DPTR:
                    result.Instruction = InstructionType.MOVC;
                    result.Arguments.Add("A");
                    result.Arguments.Add("@A+DPTR");
                    break;
                #endregion

                #region MOV // complete
                case CMD_MOV_DATA: // A
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_MOV_DATA + 0x01: // iram addr
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[2]));
                    break;

                case CMD_MOV_DATA + 0x02: // @R0
                case CMD_MOV_DATA + 0x03: // @R1
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_MOV_DATA + 0x04: // R0
                case CMD_MOV_DATA + 0x05: // R1
                case CMD_MOV_DATA + 0x06: // R2
                case CMD_MOV_DATA + 0x07: // R3
                case CMD_MOV_DATA + 0x08: // R4
                case CMD_MOV_DATA + 0x09: // R5
                case CMD_MOV_DATA + 0x0a: // R6
                case CMD_MOV_DATA + 0x0b: // R7
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_MOV_IRAM: // iram addr
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[2])); // command parameter in reverse order.
                    result.Arguments.Add(RamName(result.Data[1])); // command parameter in reverse order.
                    break;

                case CMD_MOV_IRAM + 0x01: // iram addr, @R0
                case CMD_MOV_IRAM + 0x02: // iram addr, @R1
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_MOV_IRAM + 0x03: // iram addr, R0
                case CMD_MOV_IRAM + 0x04: // iram addr, R1
                case CMD_MOV_IRAM + 0x05: // iram addr, R2
                case CMD_MOV_IRAM + 0x06: // iram addr, R3
                case CMD_MOV_IRAM + 0x07: // iram addr, R4
                case CMD_MOV_IRAM + 0x08: // iram addr, R5
                case CMD_MOV_IRAM + 0x09: // iram addr, R6
                case CMD_MOV_IRAM + 0x0a: // iram addr, R7
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;

                case CMD_MOV_IRAM2: // @R0, iram addr
                case CMD_MOV_IRAM2 + 0x01: // @R1, iram addr
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_MOV_IRAM2 + 0x02: // R0, iram addr
                case CMD_MOV_IRAM2 + 0x03: // R1, iram addr
                case CMD_MOV_IRAM2 + 0x04: // R2, iram addr
                case CMD_MOV_IRAM2 + 0x05: // R3, iram addr
                case CMD_MOV_IRAM2 + 0x06: // R4, iram addr
                case CMD_MOV_IRAM2 + 0x07: // R5, iram addr
                case CMD_MOV_IRAM2 + 0x08: // R6, iram addr
                case CMD_MOV_IRAM2 + 0x09: // R7, iram addr
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_MOV_A: // A, iram addr
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_MOV_A + 0x01:  // A, @R0
                case CMD_MOV_A + 0x02:  // A, @R1
                    result.Instruction = InstructionType.MOV;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_MOV_A + 0x03: // A, R0
                case CMD_MOV_A + 0x04: // A, R1
                case CMD_MOV_A + 0x05: // A, R2
                case CMD_MOV_A + 0x06: // A, R3
                case CMD_MOV_A + 0x07: // A, R4
                case CMD_MOV_A + 0x08: // A, R5
                case CMD_MOV_A + 0x09: // A, R6
                case CMD_MOV_A + 0x0a: // A, R7
                    result.Instruction = InstructionType.MOV;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;

                case CMD_MOV_DPTR:
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("DPTR");
                    result.Arguments.Add(string.Format("#{0:X4}h", result.Data[1] << 8 | result.Data[2]));
                    break;

                case CMD_MOV_BIT:
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    result.Arguments.Add("C");
                    break;

                case CMD_MOV_C:
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("C");
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_MOV_A2: // iram addr, A
                    result.Instruction = InstructionType.MOV;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add("A");
                    break;

                case CMD_MOV_A2 + 0x01:  // @R0, A
                case CMD_MOV_A2 + 0x02:  // @R1, A
                    result.Instruction = InstructionType.MOV;
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    result.Arguments.Add("A");
                    break;

                case CMD_MOV_A2 + 0x03: // R0, A
                case CMD_MOV_A2 + 0x04: // R1, A
                case CMD_MOV_A2 + 0x05: // R2, A
                case CMD_MOV_A2 + 0x06: // R3, A
                case CMD_MOV_A2 + 0x07: // R4, A
                case CMD_MOV_A2 + 0x08: // R5, A
                case CMD_MOV_A2 + 0x09: // R6, A
                case CMD_MOV_A2 + 0x0a: // R7, A
                    result.Instruction = InstructionType.MOV;
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    result.Arguments.Add("A");
                    break;
                #endregion

                #region ORL // complete
                case CMD_ORL:  // iram addr, A
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add("A");
                    break;

                case CMD_ORL + 0x01:  // iram addr, #data
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[2]));
                    break;

                case CMD_ORL + 0x02:  // A, #data
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_ORL + 0x03:  // A, iram addr
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_ORL + 0x04:  // A, @R0
                case CMD_ORL + 0x05:  // A, @R1
                    result.Instruction = InstructionType.ORL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_ORL + 0x06: // R0
                case CMD_ORL + 0x07: // R1
                case CMD_ORL + 0x08: // R2
                case CMD_ORL + 0x09: // R3
                case CMD_ORL + 0x0a: // R4
                case CMD_ORL + 0x0b: // R5
                case CMD_ORL + 0x0c: // R6
                case CMD_ORL + 0x0d: // R7
                    result.Instruction = InstructionType.ORL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;

                case CMD_ORL_C:  // C, bit addr
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("C");
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_ORL_C2:  // C, /bit addr
                    result.Instruction = InstructionType.ORL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("C");
                    result.Arguments.Add("/" + BitName(result.Data[1]));
                    break;
                #endregion

                #region ANL // complete
                case CMD_ANL:  // iram addr, A
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add("A");
                    break;

                case CMD_ANL + 0x01:  // iram addr, #data
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[2]));
                    break;

                case CMD_ANL + 0x02:  // A, #data
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_ANL + 0x03:  // A, iram addr
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_ANL + 0x04:  // A, @R0
                case CMD_ANL + 0x05:  // A, @R1
                    result.Instruction = InstructionType.ANL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_ANL + 0x06: // R0
                case CMD_ANL + 0x07: // R1
                case CMD_ANL + 0x08: // R2
                case CMD_ANL + 0x09: // R3
                case CMD_ANL + 0x0a: // R4
                case CMD_ANL + 0x0b: // R5
                case CMD_ANL + 0x0c: // R6
                case CMD_ANL + 0x0d: // R7
                    result.Instruction = InstructionType.ANL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;

                case CMD_ANL_C:  //  C,bit addr
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("C");
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_ANL_C2:  //  C,/bit addr
                    result.Instruction = InstructionType.ANL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("C");
                    result.Arguments.Add("/" + BitName(result.Data[1]));
                    break;
                #endregion

                #region XRL // complete
                case CMD_XRL:  // iram addr, A
                    result.Instruction = InstructionType.XRL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add("A");
                    break;

                case CMD_XRL + 0x01:  // iram addr, #data
                    result.Instruction = InstructionType.XRL;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[2]));
                    break;

                case CMD_XRL + 0x02:  // A, #data
                    result.Instruction = InstructionType.XRL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_XRL + 0x03:  // A, iram addr
                    result.Instruction = InstructionType.XRL;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_XRL + 0x04:  // A, @R0
                case CMD_XRL + 0x05:  // A, @R1
                    result.Instruction = InstructionType.XRL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_XRL + 0x06: // R0
                case CMD_XRL + 0x07: // R1
                case CMD_XRL + 0x08: // R2
                case CMD_XRL + 0x09: // R3
                case CMD_XRL + 0x0a: // R4
                case CMD_XRL + 0x0b: // R5
                case CMD_XRL + 0x0c: // R6
                case CMD_XRL + 0x0d: // R7
                    result.Instruction = InstructionType.XRL;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region DJNZ // complete
                case CMD_DJNZ_IRAM:
                    result.Instruction = InstructionType.DJNZ;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_DJNZ: // R0
                case CMD_DJNZ + 0x01: // R1
                case CMD_DJNZ + 0x02: // R2
                case CMD_DJNZ + 0x03: // R3
                case CMD_DJNZ + 0x04: // R4
                case CMD_DJNZ + 0x05: // R5
                case CMD_DJNZ + 0x06: // R6
                case CMD_DJNZ + 0x07: // R7
                    result.Instruction = InstructionType.DJNZ;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region CLR // complete
                case CMD_CLR: // bit
                    result.Instruction = InstructionType.CLR;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_CLR + 0x01: // C
                    result.Instruction = InstructionType.CLR;
                    result.Arguments.Add("C");
                    break;

                case CMD_CLR_A:
                    result.Instruction = InstructionType.CLR;
                    result.Arguments.Add("A");
                    break;
                #endregion

                #region SETB // complete
                case CMD_SETB: // bit addr
                    result.Instruction = InstructionType.SETB;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    break;

                case CMD_SETB + 0x01: // C
                    result.Instruction = InstructionType.SETB;
                    result.Arguments.Add("C");
                    break;
                #endregion

                #region Conditional Jumps
                case CMD_JBC:
                    result.Instruction = InstructionType.JBC;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_JB:
                    result.Instruction = InstructionType.JB;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_JNB:
                    result.Instruction = InstructionType.JNB;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(BitName(result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_JC:
                    result.Instruction = InstructionType.JC;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    break;

                case CMD_JNC:
                    result.Instruction = InstructionType.JNC;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    break;

                case CMD_JZ:
                    result.Instruction = InstructionType.JZ;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    break;

                case CMD_JNZ:
                    result.Instruction = InstructionType.JNZ;
                    result.Data.Add(br.ReadByte());
                    result.TargetAddress = result.Data[1];
                    break;
                #endregion

                #region INC // complete
                case CMD_INC: // A
                    result.Instruction = InstructionType.INC;
                    result.Arguments.Add("A");
                    break;

                case CMD_INC + 0x01: // iram addr
                    result.Instruction = InstructionType.INC;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_INC + 0x02: // @R0
                case CMD_INC + 0x03: // @R1
                    result.Instruction = InstructionType.INC;
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_INC + 0x04: // R0
                case CMD_INC + 0x05: // R1
                case CMD_INC + 0x06: // R2
                case CMD_INC + 0x07: // R3
                case CMD_INC + 0x08: // R4
                case CMD_INC + 0x09: // R5
                case CMD_INC + 0x0a: // R6
                case CMD_INC + 0x0b: // R7
                    result.Instruction = InstructionType.INC;
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;

                case CMD_INC_DPTR:
                    result.Instruction = InstructionType.INC;
                    result.Arguments.Add("DPTR");
                    break;
                #endregion

                #region DEC // complete
                case CMD_DEC: // A
                    result.Instruction = InstructionType.DEC;
                    result.Arguments.Add("A");
                    break;

                case CMD_DEC + 0x01: // iram addr
                    result.Instruction = InstructionType.DEC;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_DEC + 0x02: // @R0
                case CMD_DEC + 0x03: // @R1
                    result.Instruction = InstructionType.DEC;
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_DEC + 0x04: // R0
                case CMD_DEC + 0x05: // R1
                case CMD_DEC + 0x06: // R2
                case CMD_DEC + 0x07: // R3
                case CMD_DEC + 0x08: // R4
                case CMD_DEC + 0x09: // R5
                case CMD_DEC + 0x0a: // R6
                case CMD_DEC + 0x0b: // R7
                    result.Instruction = InstructionType.DEC;
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region SUBB // complete
                case CMD_SUBB: // #data
                    result.Instruction = InstructionType.SUBB;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_SUBB + 0x01: // iram addr
                    result.Instruction = InstructionType.SUBB;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_SUBB + 0x02: // @R0
                case CMD_SUBB + 0x03: // @R1
                    result.Instruction = InstructionType.SUBB;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_SUBB + 0x04: // R0
                case CMD_SUBB + 0x05: // R1
                case CMD_SUBB + 0x06: // R2
                case CMD_SUBB + 0x07: // R3
                case CMD_SUBB + 0x08: // R4
                case CMD_SUBB + 0x09: // R5
                case CMD_SUBB + 0x0a: // R6
                case CMD_SUBB + 0x0b: // R7
                    result.Instruction = InstructionType.SUBB;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region MOVX // complete
                case CMD_MOVX: // @DPTR
                    result.Instruction = InstructionType.MOVX;
                    result.Arguments.Add("A");
                    result.Arguments.Add("@DPTR");
                    break;

                case CMD_MOVX + 0x02: // @R0
                case CMD_MOVX + 0x03: // @R1
                    result.Instruction = InstructionType.MOVX;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_MOVX_A: // @DPTR
                    result.Instruction = InstructionType.MOVX;
                    result.Arguments.Add("@DPTR");
                    result.Arguments.Add("A");
                    break;

                case CMD_MOVX_A + 0x02: // @R0
                case CMD_MOVX_A + 0x03: // @R1
                    result.Instruction = InstructionType.MOVX;
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    result.Arguments.Add("A");
                    break;
                #endregion

                #region CJNE // complete
                case CMD_CJNE: // A, #data
                    result.Instruction = InstructionType.CJNE;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_CJNE + 0x01: // A, iram addr
                    result.Instruction = InstructionType.CJNE;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_CJNE + 0x02:  // @R0
                case CMD_CJNE + 0x03:  // @R1
                    result.Instruction = InstructionType.CJNE;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;

                case CMD_CJNE + 0x04: // R0
                case CMD_CJNE + 0x05: // R1
                case CMD_CJNE + 0x06: // R2
                case CMD_CJNE + 0x07: // R3
                case CMD_CJNE + 0x08: // R4
                case CMD_CJNE + 0x09: // R5
                case CMD_CJNE + 0x0a: // R6
                case CMD_CJNE + 0x0b: // R7
                    result.Instruction = InstructionType.CJNE;
                    result.Data.Add(br.ReadByte());
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    result.TargetAddress = result.Data[2];
                    break;
                #endregion

                #region ADD // complete
                case CMD_ADD: // A, #data
                    result.Instruction = InstructionType.ADD;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_ADD + 0x01: // A, iram addr
                    result.Instruction = InstructionType.ADD;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_ADD + 0x02:  // A, @R0
                case CMD_ADD + 0x03:  // A, @R1
                    result.Instruction = InstructionType.ADD;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_ADD + 0x04: // A, R0
                case CMD_ADD + 0x05: // A, R1
                case CMD_ADD + 0x06: // A, R2
                case CMD_ADD + 0x07: // A, R3
                case CMD_ADD + 0x08: // A, R4
                case CMD_ADD + 0x09: // A, R5
                case CMD_ADD + 0x0a: // A, R6
                case CMD_ADD + 0x0b: // A, R7
                    result.Instruction = InstructionType.ADD;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region ADDC // complete
                case CMD_ADDC: // A, #data
                    result.Instruction = InstructionType.ADDC;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("#{0:X2}h", result.Data[1]));
                    break;

                case CMD_ADDC + 0x01: // A, iram addr
                    result.Instruction = InstructionType.ADDC;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_ADDC + 0x02:  // A, @R0
                case CMD_ADDC + 0x03:  // A, @R1
                    result.Instruction = InstructionType.ADDC;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_ADDC + 0x04: // A, R0
                case CMD_ADDC + 0x05: // A, R1
                case CMD_ADDC + 0x06: // A, R2
                case CMD_ADDC + 0x07: // A, R3
                case CMD_ADDC + 0x08: // A, R4
                case CMD_ADDC + 0x09: // A, R5
                case CMD_ADDC + 0x0a: // A, R6
                case CMD_ADDC + 0x0b: // A, R7
                    result.Instruction = InstructionType.ADDC;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                #region XCH // complete
                case CMD_XCH: // iram addr
                    result.Instruction = InstructionType.XCH;
                    result.Data.Add(br.ReadByte());
                    result.Arguments.Add("A");
                    result.Arguments.Add(RamName(result.Data[1]));
                    break;

                case CMD_XCH + 0x01: // @R0
                case CMD_XCH + 0x02: // @R1
                    result.Instruction = InstructionType.XCH;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("@R{0}", instruction & 0x01));
                    break;

                case CMD_XCH + 0x03: // R0
                case CMD_XCH + 0x04: // R1
                case CMD_XCH + 0x05: // R2
                case CMD_XCH + 0x06: // R3
                case CMD_XCH + 0x07: // R4
                case CMD_XCH + 0x08: // R5
                case CMD_XCH + 0x09: // R6
                case CMD_XCH + 0x0a: // R7
                    result.Instruction = InstructionType.XCH;
                    result.Arguments.Add("A");
                    result.Arguments.Add(string.Format("R{0}", instruction & 0x07));
                    break;
                #endregion

                case 0xA5:
                    Debug.WriteLine("Unknown");
                    break;
                default:
                    throw new NotImplementedException(instruction.ToString("X2"));
            }

            return result;
        }

        private string RamName(byte address)
        {
            if (reverseSfrMap.ContainsKey(address))
            {
                return reverseSfrMap[address];
            }
            return string.Format("RAM_{0:X2}", address);
        }

        private string BitName(byte address)
        {
            if (reverseSfrBitMap.ContainsKey(address))
            {
                return reverseSfrBitMap[address];
            }

            byte ramAddr = (byte)(address & 0xF8);
            return string.Format("RAM_{0:X2}.{1}", ramAddr, address & 0x07);
        }

        public void CreateString(BinaryReader br, ushort startAddress, List<byte> entryData)
        {
            ListingEntry entry = new()
            {
                Address = startAddress,
                Instruction = InstructionType.DB,
                Data = entryData,
                Comment = entryData.ToAnsiString(),
                Arguments = new() { "'" + entryData.ToEscapedAnsiString() + "'" }
            };
            entry.UpdateStrings();

            RemoveOverlappingDbEntries(entry.Address, (ushort)(entry.Address + entry.Data.Count));

            if (!listing!.Contains(entry.Address))
            {
                listing.Add(entry);
            }

            RestoreDbEntries(br);
        }

        private void RestoreDbEntries(BinaryReader br)
        {
            // scan for data
            int dataAddress = 0;
            while (dataAddress < br.BaseStream.Length)
            {
                ushort address = (ushort)dataAddress;

                // step over next listing
                if (listing!.GetByAddress(address) is ListingEntry listingEntry)
                {
                    dataAddress += listingEntry.Data.Count;
                    continue;
                }


                int next = (int)br.BaseStream.Length;

                try
                {
                    next = listing.Where(l => l.Address > address).Select(l => l.Address).Min();
                }
                catch
                {
                }

                // read gap
                br.BaseStream.Position = address;
                byte[] data = br.ReadBytes(next - address);

                int dataPos = 0;
                while (dataPos < data.Length)
                {
                    int length = Math.Min(DB_DEFAULT_SIZE, data.Length - dataPos);

                    byte[] buffer = data[dataPos..(dataPos + length)];

                    ListingEntry entry = new()
                    {
                        Address = address,
                        Instruction = InstructionType.DB,
                        Data = buffer.ToList(),
                        Comment = buffer.ToAnsiString(),
                        Arguments = buffer.Select(c => c.ToString("x2") + 'h').ToList()
                    };
                    entry.UpdateStrings();
                    listing.Add(entry);

                    address += (ushort)length;
                    dataPos += length;
                }

                dataAddress = next;
            }
        }

        public void Undefine(BinaryReader reader, ushort currentAddress)
        {
            if (listing!.GetByAddress(currentAddress) is not ListingEntry listingEntry)
            {
                return;
            }
            int index = listing.IndexOf(listingEntry);

            if (index > 0)
            {
                ListingEntry prevListingEntry = listing[index - 1];
                if (prevListingEntry.Instruction == InstructionType.DB && 
                    !prevListingEntry.ArgumentString.Contains('\'') &&
                    prevListingEntry.Arguments.Count < DB_DEFAULT_SIZE)
                {
                    listing.Remove(prevListingEntry);
                }
            }
            listing.Remove(listingEntry);
            RestoreDbEntries(reader);
        }
    }
}

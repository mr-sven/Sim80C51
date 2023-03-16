using Sim80C51.Common;
using Sim80C51.Toolbox.Wpf;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sim80C51.Processors
{
    /// <summary>
    /// Debug and process logic
    /// </summary>
    public abstract partial class C80C51 : NotifyPropertyChanged
    {
        private static readonly string[] regNames = new string[] { nameof(R0), nameof(R1), nameof(R2), nameof(R3), nameof(R4), nameof(R5), nameof(R6), nameof(R7) };

        protected bool instructionInProgress = false;

        // TODO: Handle multiple IRQ Prio 
        private Action? pendingIrq = null;

        private void Push(byte data)
        {
            SetMem(++SP, data);
        }

        private byte Pop()
        {
            byte res = GetMem(SP--);
            CallStackEntry[] rmCallStack = CallStack.Where(cs => cs.StackPointer >= SP).ToArray();
            foreach (CallStackEntry cs in rmCallStack)
            {
                CallStack.Remove(cs);
            }

            return res;
        }

        /// <summary>
        /// Callback for MOVC calls to get code byte
        /// </summary>
        public Func<ushort, byte>? GetCodeByte { get; set; }

        /// <summary>
        /// Callback for MOVX to get byte from XRAM
        /// </summary>
        public Func<ushort, byte>? GetRamByte { get; set; }

        /// <summary>
        /// Callback for MOVX to write byte to XRAM
        /// </summary>
        public Action<ushort, byte>? SetRamByte { get; set; }

        /// <summary>
        /// Process listing entry.
        /// May throw exceptions in case of wrong listing entrys
        /// </summary>
        /// <param name="entry">the listing entry</param>
        public void Process(ListingEntry entry)
        {
            instructionInProgress = true;
            CpuCycle(); // first process cycle
            PC += (ushort)entry.Data.Count;
            byte tmpByte;
            ushort tmpUshort;
            int tmpInt;
            switch (entry.Instruction)
            {
                case InstructionType.NOP:
                    break;

                case InstructionType.LJMP:
                case InstructionType.AJMP:
                case InstructionType.SJMP:
                    CpuCycle();
                    PC = entry.TargetAddress;
                    break;

                case InstructionType.LCALL:
                case InstructionType.ACALL:
                    CpuCycle();
                    CallStack.Add(new(PC, SP));
                    Push((byte)(PC & 0xff));
                    Push((byte)(PC >> 8 & 0xff));
                    PC = entry.TargetAddress;
                    break;

                case InstructionType.RET:
                case InstructionType.RETI:
                    CpuCycle();
                    PC = (ushort)(Pop() << 8);
                    PC += Pop();
                    break;

                case InstructionType.MUL:
                    CpuCycle();
                    CpuCycle();
                    CpuCycle();
                    CY = false;
                    uint res = (uint)ACC * B;
                    OV = res > 0xff;
                    ACC = (byte)(res & 0xff);
                    B = (byte)(res >> 8 & 0xff);
                    break;

                case InstructionType.DIV:
                    CpuCycle();
                    CpuCycle();
                    CpuCycle();
                    CY = false;
                    if (B == 0)
                    {
                        OV = true;
                    }
                    else
                    {
                        OV = false;
                        byte quotient = (byte)(ACC / B);
                        B = (byte)(ACC % B);
                        ACC = quotient;
                    }
                    break;

                case InstructionType.RL:
                    ACC = (byte)(ACC << 1 | ACC >> 7);
                    break;

                case InstructionType.RLC:
                    tmpUshort = (ushort)(ACC << 1);
                    if (CY)
                    {
                        tmpUshort |= 0x01;
                    }
                    CY = (tmpUshort & 0x100) == 0x100;
                    ACC = (byte)(tmpUshort & 0xff);
                    break;

                case InstructionType.RR:
                    ACC = (byte)(ACC >> 1 | ACC << 7);
                    break;

                case InstructionType.RRC:
                    tmpUshort = ACC;
                    if (CY)
                    {
                        tmpUshort |= 0x100;
                    }
                    CY = (tmpUshort & 0x01) == 0x01;
                    ACC = (byte)(tmpUshort >> 1 & 0xff);
                    break;

                case InstructionType.SWAP:
                    ACC = (byte)(ACC >> 4 | ACC << 4);
                    break;

                case InstructionType.JC:
                    CpuCycle();
                    if (CY)
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JNC:
                    CpuCycle();
                    if (!CY)
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JNZ:
                    CpuCycle();
                    if (ACC != 0x00)
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JZ:
                    CpuCycle();
                    if (ACC == 0x00)
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JMP:
                    CpuCycle();
                    PC = (ushort)(ACC + DPTR);
                    break;

                case InstructionType.MOV:
                    if (entry.Arguments.First() != "A" &&
                        entry.Arguments.Last() != "A" &&
                        (!entry.Arguments.First().StartsWith('@') || !entry.Arguments.Last().StartsWith('#')) &&
                        (!regNames.Contains(entry.Arguments.First()) || !entry.Arguments.Last().StartsWith('#')))
                    {
                        CpuCycle();
                    }

                    if (entry.Arguments[0] == "C")
                    {
                        CY = GetBitFromInstuctionArgument(entry.Arguments[1]);
                    }
                    else if (entry.Arguments[1] == "C")
                    {
                        SetBitFromInstuctionArgument(entry.Arguments[0], CY);
                    }
                    else if (entry.Arguments[0] == "DPTR")
                    {
                        DPTR = ListingFactory.ParseIntermediateUShort(entry.Arguments[1]);
                    }
                    else
                    {
                        tmpByte = GetByteFromInstructionArgument(entry.Arguments[1]);
                        SetByteFromInstructionArgument(entry.Arguments[0], tmpByte);
                    }
                    break;

                case InstructionType.DJNZ:
                    CpuCycle();
                    tmpByte = GetByteFromInstructionArgument(entry.Arguments[0]);
                    tmpByte--;
                    SetDirectRam(entry.Arguments[0], tmpByte);
                    if (tmpByte != 0x00)
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.POP:
                    CpuCycle();
                    SetDirectRam(entry.Arguments[0], Pop());
                    break;

                case InstructionType.PUSH:
                    CpuCycle();
                    Push(GetDirectRam(entry.Arguments[0]));
                    break;

                case InstructionType.INC:
                    if (entry.Arguments[0] == "DPTR")
                    {
                        CpuCycle();
                        CpuCycle();
                        DPTR++;
                    }
                    else
                    {
                        tmpByte = GetByteFromInstructionArgument(entry.Arguments[0]);
                        SetByteFromInstructionArgument(entry.Arguments[0], (byte)(tmpByte + 1));
                    }
                    break;

                case InstructionType.DEC:
                    tmpByte = GetByteFromInstructionArgument(entry.Arguments[0]);
                    SetByteFromInstructionArgument(entry.Arguments[0], (byte)(tmpByte - 1));
                    break;

                case InstructionType.MOVC:
                    CpuCycle();
                    if (entry.Arguments[1] == "@A+DPTR")
                    {
                        ACC = GetCodeByte?.Invoke((ushort)(ACC + DPTR)) ?? 0;
                    }
                    else if (entry.Arguments[1] == "@A+PC")
                    {
                        ACC = GetCodeByte?.Invoke((ushort)(ACC + PC)) ?? 0;
                    }
                    break;

                case InstructionType.CLR:
                    if (entry.Arguments[0] == "A")
                    {
                        ACC = 0x00;
                    }
                    else if (entry.Arguments[0] == "C")
                    {
                        CY = false;
                    }
                    else
                    {
                        SetBitFromInstuctionArgument(entry.Arguments[0], false);
                    }
                    break;

                case InstructionType.SETB:
                    if (entry.Arguments[0] == "C")
                    {
                        CY = true;
                    }
                    else
                    {
                        SetBitFromInstuctionArgument(entry.Arguments[0], true);
                    }
                    break;

                case InstructionType.JB:
                    CpuCycle();
                    if (GetBitFromInstuctionArgument(entry.Arguments[0]))
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JNB:
                    CpuCycle();
                    if (!GetBitFromInstuctionArgument(entry.Arguments[0]))
                    {
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.JBC:
                    CpuCycle();
                    if (GetBitFromInstuctionArgument(entry.Arguments[0]))
                    {
                        SetBitFromInstuctionArgument(entry.Arguments[0], false);
                        PC = entry.TargetAddress;
                    }
                    break;

                case InstructionType.CPL:
                    if (entry.Arguments[0] == "A")
                    {
                        ACC = (byte)~ACC;
                    }
                    else if (entry.Arguments[0] == "C")
                    {
                        CY = !CY;
                    }
                    else
                    {
                        bool currentBit = GetBitFromInstuctionArgument(entry.Arguments[0]);
                        SetBitFromInstuctionArgument(entry.Arguments[0], !currentBit);
                    }
                    break;

                case InstructionType.ANL:
                    if (entry.Arguments[0] == "C")
                    {
                        CpuCycle();
                        string bitName = entry.Arguments[1];
                        if (bitName.StartsWith('/'))
                        {
                            CY &= !GetBitFromInstuctionArgument(bitName[1..]);
                        }
                        else
                        {
                            CY &= GetBitFromInstuctionArgument(bitName);
                        }
                    }
                    else
                    {
                        if (entry.Arguments[1].StartsWith('#'))
                        {
                            CpuCycle();
                        }
                        tmpByte = GetByteFromInstructionArgument(entry.Arguments[0]);
                        tmpByte &= GetByteFromInstructionArgument(entry.Arguments[1]);
                        SetDirectRam(entry.Arguments[0], tmpByte);
                    }
                    break;

                case InstructionType.ORL:
                    if (entry.Arguments[0] == "C")
                    {
                        CpuCycle();
                        string bitName = entry.Arguments[1];
                        if (bitName.StartsWith('/'))
                        {
                            CY |= !GetBitFromInstuctionArgument(bitName[1..]);
                        }
                        else
                        {
                            CY |= GetBitFromInstuctionArgument(bitName);
                        }
                    }
                    else
                    {
                        if (entry.Arguments[1].StartsWith('#'))
                        {
                            CpuCycle();
                        }
                        tmpByte = GetByteFromInstructionArgument(entry.Arguments[0]);
                        tmpByte |= GetByteFromInstructionArgument(entry.Arguments[1]);
                        SetDirectRam(entry.Arguments[0], tmpByte);
                    }
                    break;

                case InstructionType.MOVX:
                    CpuCycle();
                    if (entry.Arguments[0] == "A")
                    {
                        switch (entry.Arguments[1])
                        {
                            case "@R0":
                                ACC = GetRamByte?.Invoke(R0) ?? 0;
                                break;
                            case "@R1":
                                ACC = GetRamByte?.Invoke(R1) ?? 0;
                                break;
                            case "@DPTR":
                                ACC = GetRamByte?.Invoke(DPTR) ?? 0;
                                break;
                        }
                    }
                    else
                    {
                        switch (entry.Arguments[0])
                        {
                            case "@R0":
                                SetRamByte?.Invoke(R0, ACC);
                                break;
                            case "@R1":
                                SetRamByte?.Invoke(R1, ACC);
                                break;
                            case "@DPTR":
                                SetRamByte?.Invoke(DPTR, ACC);
                                break;
                        }
                    }
                    break;

                case InstructionType.CJNE:
                    CpuCycle();
                    byte compFirst = entry.Arguments[0] switch
                    {
                        "A" => ACC,
                        "R0" => R0,
                        "R1" => R1,
                        "R2" => R2,
                        "R3" => R3,
                        "R4" => R4,
                        "R5" => R5,
                        "R6" => R6,
                        "R7" => R7,
                        "@R0" => GetIndirectRam(R0),
                        "@R1" => GetIndirectRam(R1),
                        _ => throw new Exception("Instruction Argument unknown: " + entry.Arguments[0])
                    };

                    byte compSecond = entry.Arguments[1].StartsWith('#') ? ListingFactory.ParseIntermediateByte(entry.Arguments[1]) : GetDirectRam(entry.Arguments[1]);
                    if (compFirst != compSecond)
                    {
                        PC = entry.TargetAddress;
                    }

                    CY = compFirst < compSecond;
                    break;

                case InstructionType.XCH:
                    switch (entry.Arguments[1])
                    {
                        case "@R0":
                            tmpByte = GetIndirectRam(R0);
                            SetIndirectRam(R0, ACC);
                            break;
                        case "@R1":
                            tmpByte = GetIndirectRam(R1);
                            SetIndirectRam(R1, ACC);
                            break;
                        default:
                            tmpByte = GetDirectRam(entry.Arguments[1]);
                            SetDirectRam(entry.Arguments[1], ACC);
                            break;
                    }
                    ACC = tmpByte;
                    break;

                case InstructionType.XRL:
                    if (entry.Arguments[1].StartsWith('#'))
                    {
                        CpuCycle();
                    }
                    if (entry.Arguments[0] == "A")
                    {
                        ACC ^= GetByteFromInstructionArgument(entry.Arguments[1]);
                    }
                    else
                    {
                        tmpByte = GetDirectRam(entry.Arguments[0]);
                        if (entry.Arguments[1] == "A")
                        {
                            tmpByte ^= ACC;
                        }
                        else
                        {
                            tmpByte ^= ListingFactory.ParseIntermediateByte(entry.Arguments[1]);
                        }
                        SetDirectRam(entry.Arguments[0], tmpByte);
                    }
                    break;

                case InstructionType.XCHD:
                    if (entry.Arguments[1] == "@R0")
                    {
                        tmpByte = GetIndirectRam(R0);
                        SetIndirectRam(R0, (byte)((tmpByte & 0xf0) | (ACC & 0x0f)));
                    }
                    else
                    {
                        tmpByte = GetIndirectRam(R1);
                        SetIndirectRam(R1, (byte)((tmpByte & 0xf0) | (ACC & 0x0f)));
                    }
                    ACC = (byte)((ACC & 0xf0) | (tmpByte & 0x0f));
                    break;

                case InstructionType.DA:
                    if ((ACC & 0x0f) > 9 | AC)
                    {
                        tmpByte = (byte)((ACC & 0x0f) + 6);
                        ACC = (byte)((ACC & 0xf0) | (tmpByte & 0x0f));
                    }
                    if ((ACC & 0xf0 >> 4) > 9 | CY)
                    {
                        tmpByte = (byte)((ACC & 0xf0 >> 4) + 6);
                        ACC = (byte)((ACC & 0x0f) | (tmpByte & 0x0f << 4));
                    }
                    break;

                case InstructionType.ADD:
                    tmpByte = GetByteFromInstructionArgument(entry.Arguments[1]);

                    tmpUshort = (ushort)(ACC + tmpByte);
                    CY = tmpUshort > 0xff;

                    tmpInt = unchecked((sbyte)ACC) + unchecked((sbyte)tmpByte);
                    OV = tmpInt > 127 || tmpInt < -127;

                    AC = (ACC & 0x0f) + (tmpByte & 0x0f) > 0x0f;

                    ACC = (byte)(tmpUshort & 0xff);
                    break;

                case InstructionType.ADDC:
                    tmpByte = GetByteFromInstructionArgument(entry.Arguments[1]);

                    tmpUshort = (ushort)(ACC + tmpByte + (CY ? 1 : 0));
                    CY = tmpUshort > 0xff;

                    tmpInt = unchecked((sbyte)ACC) + unchecked((sbyte)tmpByte + (CY ? 1 : 0));
                    OV = tmpInt > 127 || tmpInt < -127;

                    AC = (ACC & 0x0f) + (tmpByte & 0x0f) > 0x0f;

                    ACC = (byte)(tmpUshort & 0xff);
                    break;

                case InstructionType.SUBB:
                    tmpByte = GetByteFromInstructionArgument(entry.Arguments[1]);
                    tmpInt = ACC - tmpByte - (CY ? 1 : 0);
                    tmpUshort = (ushort)(tmpInt & 0xff);
                    if (tmpInt < 0)
                    {
                        CY = true;
                    }
                    else
                    {
                        CY = false;
                    }

                    tmpInt = unchecked((sbyte)ACC) - unchecked((sbyte)tmpByte - (CY ? 1 : 0));
                    OV = tmpInt > 127 || tmpInt < -127;

                    AC = ((tmpByte + (CY ? 1 : 0)) & 0x0f) > (ACC & 0x0f);
                    ACC = (byte)(tmpUshort & 0xff);
                    break;

                // Compiler instructions
                case InstructionType.DB:
                    throw new NotImplementedException(entry.Instruction + " " + entry.ArgumentString);
                case InstructionType.DW:
                    throw new NotImplementedException(entry.Instruction + " " + entry.ArgumentString);
                case InstructionType.DBIT:
                    throw new NotImplementedException(entry.Instruction + " " + entry.ArgumentString);
                case InstructionType.ORG:
                    throw new NotImplementedException(entry.Instruction + " " + entry.ArgumentString);
            }
            instructionInProgress = false;
            // TODO: Handle multiple IRQ Prio 
            pendingIrq?.Invoke();

            CheckInterruptFlags();
        }

        private void CheckInterruptFlags()
        {
            if (!EA) { return; }
            //if (EX0 && IE0) { Interrupt_X0(); }
        }

        private void ExecuteIrq(ushort address)
        {
            pendingIrq = null;

            // Execute a virtual LCALL (2 cyles)
            CpuCycle();
            CpuCycle();

            // save current PC to stack
            CallStack.Add(new(PC, SP));
            Push((byte)(PC & 0xff));
            Push((byte)(PC >> 8 & 0xff));

            // set PC to IV Address
            PC = address;
        }

        protected void Interrupt([CallerMemberName] string irqName = "")
        {
            if (GetType().GetMethod(irqName) is not MethodInfo mInfo)
            {
                throw new ArgumentException("Invalid IV Call", irqName);
            }

            if (mInfo.GetCustomAttribute<IVAttribute>() is not IVAttribute ivAttr)
            {
                throw new ArgumentException("IV Attribute missing", irqName);
            }

            // TODO: Handle multiple IRQ Prio 
            if (instructionInProgress)
            {
                pendingIrq = () => { ExecuteIrq(ivAttr.Address); };
            }
            else
            {
                ExecuteIrq(ivAttr.Address);
            }
        }

        protected virtual void CpuCycle()
        {
            Cycles++;
            StepTimer0(true);
            StepTimer1(true);
        }

        private void StepTimer0(bool fromCpu)
        {
            if ((!TR0) || (GATE_0 && (!INT0)))
            {
                return;
            }

            bool oldTF0 = TF0;
            bool oldTF1 = TF1;

            if (M0_0 && M0_1) // mode 3
            {
                TH0++;
                if (TH0 == 0)
                {
                    TF1 = true;
                }
            }

            if (!C_T_0 || !fromCpu)
            {
                TL0++;
                if (M0_0 && M0_1) // mode 3
                {
                    if (TL0 == 0)
                    {
                        TF0 = true;
                    }
                }
                else if (!M0_0 && !M0_1) // mode 0
                {
                    if (TL0 == 32)
                    {
                        TL0 = 0;
                        TH0++;
                        if (TH0 == 0)
                        {
                            TF0 = true;
                        }
                    }
                }
                else if (M0_0 && !M0_1) // mode 1
                {
                    if (TL0 == 0)
                    {
                        TH0++;
                        if (TH0 == 0)
                        {
                            TF0 = true;
                        }
                    }
                }
                else if (!M0_0 && M0_1) // mode 2
                {
                    if (TL0 == 0)
                    {
                        TL0 = TH0;
                        TF0 = true;
                    }
                }
            }
            if (EA && ET1 && TF1 && !oldTF1)
            {
                TF1 = false;
                Interrupt_T1();
            }
            else if (EA && ET0 && TF0 && !oldTF0)
            {
                TF0 = false;
                Interrupt_T0();
            }
        }

        private void StepTimer1(bool fromCpu)
        {
            if ((!TR1) || (GATE_1 && (!INT1)))
            {
                return;
            }

            if (M1_0 && M1_1) // mode 3
            {
                return;
            }

            bool oldTF1 = TF1;

            if (!C_T_1 || !fromCpu)
            {
                TL1++;
                if (!M1_0 && !M1_1) // mode 0
                {
                    if (TL1 == 32)
                    {
                        TL1 = 0;
                        TH1++;
                        if (TH1 == 0)
                        {
                            TF1 = true;
                        }
                    }
                }
                else if (M1_0 && !M1_1) // mode 1
                {
                    if (TL1 == 0)
                    {
                        TH1++;
                        if (TH1 == 0)
                        {
                            TF1 = true;
                        }
                    }
                }
                else if (!M1_0 && M1_1) // mode 2
                {
                    if (TL1 == 0)
                    {
                        TL1 = TH1;
                        TF1 = true;
                    }
                }
            }
            if (EA && ET1 && TF1 && !oldTF1)
            {
                TF1 = false;
                Interrupt_T1();
            }
        }

        private void SetBitFromInstuctionArgument(string arg, bool data)
        {
            if (arg.Contains('.'))
            {
                string[] argParts = arg.Split('.');
                byte regData = GetDirectRam(argParts[0]);
                int bit = int.Parse(argParts[1]);
                if (data)
                {
                    regData |= (byte)(1 << bit);
                }
                else
                {
                    regData &= (byte)(~(1 << bit));
                }
                SetDirectRam(argParts[0], regData);
                return;
            }

            // direct Register access (R0 to R7 aligned with register bank selector)
            if (GetType().GetProperty(arg, typeof(bool)) is PropertyInfo pInfo)
            {
                pInfo.SetValue(this, data);
                return;
            }

            throw new Exception("Instruction Argument unknown: " + arg);
        }

        private void SetByteFromInstructionArgument(string arg, byte data)
        {
            // Indirect RAM access
            switch (arg)
            {
                case "@R0":
                    SetIndirectRam(R0, data);
                    return;
                case "@R1":
                    SetIndirectRam(R1, data);
                    return;
            }

            SetDirectRam(arg, data);
        }

        private void SetDirectRam(string arg, byte data)
        {
            // direct RAM access
            if (arg.StartsWith("RAM_"))
            {
                SetMem(Convert.ToByte(arg[4..], 16), data);
                return;
            }

            SetDirectRegisterByName(arg, data);
        }

        private void SetDirectRegisterByName(string arg, byte data)
        {
            // A Shortname
            if (arg == "A")
            {
                ACC = data;
                return;
            }

            // direct Register access (R0 to R7 aligned with register bank selector)
            if (GetType().GetProperty(arg, typeof(byte)) is PropertyInfo pInfo)
            {
                pInfo.SetValue(this, data);
                return;
            }

            throw new Exception("Instruction Argument unknown: " + arg);
        }

        private void SetIndirectRam(byte address, byte data)
        {
            if (address < 0x80)
            {
                SetMem(address, data);
            }
            else
            {
                SetMem((ushort)(address + 0x80), data);
            }
        }
        private bool GetBitFromInstuctionArgument(string arg)
        {
            if (arg.Contains('.'))
            {
                string[] argParts = arg.Split('.');
                byte regData = GetDirectRam(argParts[0]);
                int bit = int.Parse(argParts[1]);
                byte mask = (byte)(1 << bit);
                return (regData & mask) == mask;
            }

            // direct Register access (R0 to R7 aligned with register bank selector)
            if (GetType().GetProperty(arg, typeof(bool)) is PropertyInfo pInfo)
            {
                if (pInfo.GetValue(this) is bool res)
                {
                    return res;
                }
            }

            throw new Exception("Instruction Argument unknown: " + arg);
        }

        private byte GetByteFromInstructionArgument(string arg)
        {
            // Indirect RAM access
            if (arg == "@R0")
            {
                return GetIndirectRam(R0);
            }

            // Indirect RAM access
            if (arg == "@R1")
            {
                return GetIndirectRam(R1);
            }

            // 8-bit constant
            if (arg.StartsWith('#'))
            {
                return ListingFactory.ParseIntermediateByte(arg);
            }

            return GetDirectRam(arg);
        }

        private byte GetDirectRam(string arg)
        {
            // direct RAM access
            if (arg.StartsWith("RAM_"))
            {
                return GetMem(Convert.ToByte(arg[4..], 16));
            }

            return GetDirectRegisterByName(arg);
        }

        private byte GetDirectRegisterByName(string arg)
        {
            // A Shortname
            if (arg == "A")
            {
                return ACC;
            }

            // direct Register access (R0 to R7 aligned with register bank selector)
            if (GetType().GetProperty(arg, typeof(byte)) is PropertyInfo pInfo)
            {
                if (pInfo.GetValue(this) is byte res)
                {
                    return res;
                }
            }

            throw new Exception("Instruction Argument unknown: " + arg);
        }

        private byte GetIndirectRam(byte address)
        {
            if (address < 0x80)
            {
                return GetMem(address);
            }
            else
            {
                return GetMem((ushort)(address + 0x80));
            }
        }
    }
}

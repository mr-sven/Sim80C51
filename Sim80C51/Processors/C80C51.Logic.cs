using Sim80C51.Toolbox.Wpf;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sim80C51.Processors
{
    /// <summary>
    /// Register setter and getter logic from properties
    /// </summary>
    public abstract partial class C80C51 : NotifyPropertyChanged, I80C51Core
    {
        /// <summary>
        /// map for sfr addresses
        /// </summary>
        private readonly Dictionary<string, ushort> sfrMap = new();

        /// <summary>
        /// map for bits to sfr
        /// </summary>
        private readonly Dictionary<string, SFRBitAttribute> sfrBitMap = new();

        /// <summary>
        /// map for 16bit values
        /// </summary>
        private readonly Dictionary<string, SFR16Attribute> sfr16Map = new();

        protected readonly SortedList<byte, IV> ivList = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="size">Size of the core memory</param>
        protected C80C51(ushort size)
        {
            CoreMemory = new(ByteRow.InitRows(size));

            // build map tables
            foreach (PropertyInfo pInfo in GetType().GetProperties())
            {
                if (pInfo.GetCustomAttribute<SFRAttribute>() is SFRAttribute sfrAttr)
                {
                    sfrMap.Add(pInfo.Name, sfrAttr.Address);
                }

                if (pInfo.GetCustomAttribute<SFRBitAttribute>() is SFRBitAttribute sfrBitAttr)
                {
                    sfrBitMap.Add(pInfo.Name, sfrBitAttr);
                }

                if (pInfo.GetCustomAttribute<SFR16Attribute>() is SFR16Attribute sfr16BitAttr)
                {
                    sfr16Map.Add(pInfo.Name, sfr16BitAttr);
                }
            }

            AddIv("X0", () => PX0, () => EX0 && IE0);
            AddIv("T0", () => PT0, () => ET0 && TF0, () => TF0 = false);
            AddIv("X1", () => PX1, () => EX1 && IE1);
            AddIv("T1", () => PT1, () => ET1 && TF1, () => TF1 = false);
            AddIv("S0", () => PS0, () => ES0 && (RI || TI));

            Reset();
        }

        protected void AddIv(string name, Func<bool> priorityBit, Func<bool> check, Action? clear = null)
        {
            if (GetType().GetCustomAttributes<IVAttribute>().FirstOrDefault((iva) => iva.Name == name) is not IVAttribute ivAttr)
            {
                return;
            }

            ivList.Add(ivAttr.Priority, new(ivAttr.Address, priorityBit, check, clear));
        }

        /// <summary>
        /// Reset CPU
        /// </summary>
        public virtual void Reset()
        {
            CallStack.Clear();
            Cycles = 0;
            PC = 0x0000;
            ACC = 0x00;
            B = 0x00;
            DPTR = 0x00;
            IEN0 = 0x00;
            IP0 = 0x00;
            P3 = 0xff;
            P2 = 0xff;
            P1 = 0xff;
            P0 = 0xff;
            PCON = 0x00;
            PSW = 0x00;
            SP = 0x07;
            S0BUF = 0x00;
            S0CON = 0x00;
            TH1 = 0x00;
            TH0 = 0x00;
            TL1 = 0x00;
            TL0 = 0x00;
            TMOD = 0x00;
            TCON = 0x00;
        }

        public void RefreshUIProperies()
        {
            DoPropertyChanged("");
        }

        #region Property setter and getter with PropertyChanged notification
        /// <summary>
        /// 16-bit setter from property, calls PropertyChanged on registers and bits if changed
        /// </summary>
        /// <param name="value">new value</param>
        /// <param name="sfr16Name">register name via reflection</param>
        /// <exception cref="ArgumentException">if register not found</exception>
        protected void SetMem16FromProp(ushort value, [CallerMemberName] string sfr16Name = "")
        {
            if (!sfr16Map.ContainsKey(sfr16Name))
            {
                throw new ArgumentException(sfr16Name);
            }

            string sfrhName = sfr16Map[sfr16Name].SFRHName;
            string sfrlName = sfr16Map[sfr16Name].SFRLName;
            if (!sfrMap.ContainsKey(sfrhName))
            {
                throw new ArgumentException(sfrhName);
            }

            if (!sfrMap.ContainsKey(sfrlName))
            {
                throw new ArgumentException(sfrlName);
            }

            byte lValue = (byte)(value & 0xff);
            byte hValue = (byte)(value >> 8 & 0xff);

            SetMemFromProp(hValue, sfrhName);
            SetMemFromProp(lValue, sfrlName);
        }

        /// <summary>
        /// Returns 16 bit value
        /// </summary>
        /// <param name="sfr16Name">register name via reflection</param>
        /// <returns>the value</returns>
        /// <exception cref="ArgumentException">if register not found</exception>
        protected ushort GetMem16FromProp([CallerMemberName] string sfr16Name = "")
        {
            if (!sfr16Map.ContainsKey(sfr16Name))
            {
                throw new ArgumentException(sfr16Name);
            }

            string sfrhName = sfr16Map[sfr16Name].SFRHName;
            string sfrlName = sfr16Map[sfr16Name].SFRLName;
            if (!sfrMap.ContainsKey(sfrhName))
            {
                throw new ArgumentException(sfrhName);
            }

            if (!sfrMap.ContainsKey(sfrlName))
            {
                throw new ArgumentException(sfrlName);
            }

            return (ushort)(GetMem(sfrMap[sfrhName]) << 8 | GetMem(sfrMap[sfrlName]));
        }

        /// <summary>
        /// Sets bit from property, calls PropertyChanged on register and bit if changed
        /// </summary>
        /// <param name="value">new bit value</param>
        /// <param name="sfrBitName">bit name via reflection</param>
        /// <returns>true if value changed</returns>
        /// <exception cref="ArgumentException">if bit or register not found</exception>
        protected bool SetBitFromProp(bool value, [CallerMemberName] string sfrBitName = "")
        {
            if (!sfrBitMap.ContainsKey(sfrBitName))
            {
                throw new ArgumentException(sfrBitName);
            }

            string sfrName = sfrBitMap[sfrBitName].SFRName;
            if (!sfrMap.ContainsKey(sfrName))
            {
                throw new ArgumentException(sfrName);
            }

            if (GetBit(sfrMap[sfrName], sfrBitMap[sfrBitName].Bit) == value)
            {
                return false;
            }

            SetBit(sfrMap[sfrName], sfrBitMap[sfrBitName].Bit, value);
            DoPropertyChanged(sfrName);
            DoPropertyChanged(sfrBitName);
            foreach (string sfr16name in sfr16Map.Where(sf => sf.Value.SFRHName == sfrName || sf.Value.SFRLName == sfrName).Select(sf => sf.Key))
            {
                DoPropertyChanged(sfr16name);
            }
            return true;
        }

        /// <summary>
        /// Returns bit value
        /// </summary>
        /// <param name="sfrBitName">bit name via reflection</param>
        /// <returns>value of bit</returns>
        /// <exception cref="ArgumentException">if bit or register not found</exception>
        protected bool GetBitFromProp([CallerMemberName] string sfrBitName = "")
        {
            if (!sfrBitMap.ContainsKey(sfrBitName))
            {
                throw new ArgumentException(sfrBitName);
            }

            string sfrName = sfrBitMap[sfrBitName].SFRName;
            if (!sfrMap.ContainsKey(sfrName))
            {
                throw new ArgumentException(sfrName);
            }

            return GetBit(sfrMap[sfrName], sfrBitMap[sfrBitName].Bit);
        }

        /// <summary>
        /// Sets memory from register property, calls PropertyChanged on register and bit if changed
        /// </summary>
        /// <param name="value">new value for register</param>
        /// <param name="sfrName">register name via reflection</param>
        /// <returns>true if value changed</returns>
        /// <exception cref="ArgumentException">if register not found</exception>
        protected bool SetMemFromProp(byte value, [CallerMemberName] string sfrName = "")
        {
            if (!sfrMap.ContainsKey(sfrName))
            {
                throw new ArgumentException(sfrName);
            }

            if (GetMem(sfrMap[sfrName]) == value)
            {
                return false;
            }

            byte bitMask = SetMem(sfrMap[sfrName], value);
            DoPropertyChanged(sfrName);
            foreach (KeyValuePair<string, SFRBitAttribute> sfrBit in sfrBitMap.Where(sb => sb.Value.SFRName == sfrName))
            {
                if (((1 << sfrBit.Value.Bit) & bitMask) != 0)
                {
                    DoPropertyChanged(sfrBit.Key);
                }
            }

            foreach (string sfr16name in sfr16Map.Where(sf => sf.Value.SFRHName == sfrName || sf.Value.SFRLName == sfrName).Select(sf => sf.Key))
            {
                DoPropertyChanged(sfr16name);
            }
            return true;
        }

        /// <summary>
        /// Gets memory to property
        /// </summary>
        /// <param name="sfrName">register name via reflection</param>
        /// <returns>value of register</returns>
        /// <exception cref="ArgumentException">if register not found</exception>
        protected byte GetMemFromProp([CallerMemberName] string sfrName = "")
        {
            if (!sfrMap.ContainsKey(sfrName))
            {
                throw new ArgumentException(sfrName);
            }

            return GetMem(sfrMap[sfrName]);
        }

        /// <summary>
        /// Sets Register Value (R0-R7), calls PropertyChanged on register if changed
        /// </summary>
        /// <param name="value">new value</param>
        /// <param name="regName">register name via reflection</param>
        private void SetRegister(byte value, [CallerMemberName] string regName = "")
        {
            byte regNo = byte.Parse(regName[1].ToString());

            if (GetMem((ushort)((PSW & 0x18) | regNo)) == value)
            {
                return;
            }

            // shift by bank selector
            SetMem((ushort)((PSW & 0x18) | regNo), value);
            DoPropertyChanged(regName);
        }

        /// <summary>
        /// Gets Register Value (R0-R7)
        /// </summary>
        /// <param name="regName">register name via reflection</param>
        /// <returns>value of register</returns>
        private byte GetRegister([CallerMemberName] string regName = "")
        {
            byte regNo = byte.Parse(regName[1].ToString());

            // shift by bank selector
            return GetMem((ushort)((PSW & 0x18) | regNo));
        }
        #endregion

        #region direct memory setter and getter
        protected bool SetBit(ushort address, byte bit, bool value)
        {
            byte mask = (byte)(1 << bit);
            byte data = GetMem(address);
            if (value)
            {
                data |= mask;
            }
            else
            {
                data &= (byte)(~mask);
            }
            return SetMem(address, data) != 0;
        }

        protected bool GetBit(ushort address, byte bit)
        {
            byte mask = (byte)(1 << bit);
            return (GetMem(address) & mask) == mask;
        }

        /// <summary>
        /// Get the memory byte
        /// </summary>
        /// <param name="address">address of memory</param>
        /// <returns>data</returns>
        protected byte GetMem(ushort address)
        {
            int row = address / ByteRow.ROW_WIDTH;
            int col = address % ByteRow.ROW_WIDTH;
            return CoreMemory[row].Row[col];
        }

        /// <summary>
        /// Setting memory by sfr name
        /// </summary>
        /// <param name="sfrName">sfr to set</param>
        /// <param name="value">value to set</param>
        /// <returns>Bitmask of bits updated</returns>
        protected byte SetMem(string sfrName, byte value)
        {
            if (!sfrMap.ContainsKey(sfrName))
            {
                throw new ArgumentException(sfrName);
            }

            if (GetMem(sfrMap[sfrName]) == value)
            {
                return 0;
            }

            return SetMem(sfrMap[sfrName], value);
        }

        /// <summary>
        /// Setting memory by address
        /// </summary>
        /// <param name="address">address to set</param>
        /// <param name="value">value to set</param>
        /// <returns>Bitmask of bits updated</returns>
        protected byte SetMem(ushort address, byte value)
        {
            byte oldValue = GetMem(address);
            int row = address / ByteRow.ROW_WIDTH;
            int col = address % ByteRow.ROW_WIDTH;
            CoreMemory[row].Row[col] = value;
            return (byte)(oldValue ^ value);
        }
        #endregion
    }
}

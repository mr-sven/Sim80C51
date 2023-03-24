using Sim80C51.Interfaces;
using Sim80C51.Processors.Attributes;
using System.Collections.ObjectModel;

namespace Sim80C51.Processors
{
    /// <summary>
    /// Core register class, contains base 80C51 Regsiters
    /// </summary>
    [IV(0x0003, 1, "X0")]
    [IV(0x000B, 4, "T0")]
    [IV(0x0013, 7, "X1")]
    [IV(0x001B, 10, "T1")]
    [IV(0x0023, 13, "S0")]
    public abstract partial class C80C51 : I80C51
    {
        /// <summary>
        /// Program Counter
        /// </summary>
        public ushort PC { get => pc; set { pc = value; DoPropertyChanged(); } }
        private ushort pc;

        /// <summary>
        /// Cycle Counter
        /// </summary>
        public ulong Cycles { get => cycles; set { cycles = value; DoPropertyChanged(); } }
        private ulong cycles;

        /// <summary>
        /// Internal RAM Address space
        /// </summary>
        public ObservableCollection<IByteRow> CoreMemory { get; }

        /// <summary>
        /// Call Stack
        /// </summary>
        public ObservableCollection<ICallStackEntry> CallStack { get; } = new();

        public byte R0 { get => GetRegister(); set { SetRegister(value); } }
        public byte R1 { get => GetRegister(); set { SetRegister(value); } }
        public byte R2 { get => GetRegister(); set { SetRegister(value); } }
        public byte R3 { get => GetRegister(); set { SetRegister(value); } }
        public byte R4 { get => GetRegister(); set { SetRegister(value); } }
        public byte R5 { get => GetRegister(); set { SetRegister(value); } }
        public byte R6 { get => GetRegister(); set { SetRegister(value); } }
        public byte R7 { get => GetRegister(); set { SetRegister(value); } }

        #region ACC
        [SFR(0xE0)]
        public byte ACC { get => GetMemFromProp(); set { if (SetMemFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 0, true)]
        public bool ACC0 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 1, true)]
        public bool ACC1 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 2, true)]
        public bool ACC2 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 3, true)]
        public bool ACC3 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 4, true)]
        public bool ACC4 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 5, true)]
        public bool ACC5 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 6, true)]
        public bool ACC6 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }
        [SFRBit(nameof(ACC), 7, true)]
        public bool ACC7 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) SetParity(); } }

        /// <summary>
        /// Updates parity flag automatic if ACC is changed
        /// </summary>
        private void SetParity()
        {
            byte tmp = ACC;
            bool parity = false;
            while (tmp != 0)
            {
                parity = !parity;
                tmp = (byte)(tmp & (tmp - 1));
            }
            P = parity;
        }
        #endregion

        #region B
        [SFR(0xF0)]
        public byte B { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(B), 0, true)]
        public bool B0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 1, true)]
        public bool B1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 2, true)]
        public bool B2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 3, true)]
        public bool B3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 4, true)]
        public bool B4 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 5, true)]
        public bool B5 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 6, true)]
        public bool B6 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(B), 7, true)]
        public bool B7 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion B

        #region PSW
        [SFR(0xD0)]
        public byte PSW { get => GetMemFromProp(); set { if (SetMemFromProp(value)) DoPropertyChanged("R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7"); } }
        [SFRBit(nameof(PSW), 0, true)]
        public bool P { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PSW), 1, true)]
        public bool F1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PSW), 2, true)]
        public bool OV { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PSW), 3, true)]
        public bool RS0 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) DoPropertyChanged("R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7"); } }
        [SFRBit(nameof(PSW), 4, true)]
        public bool RS1 { get => GetBitFromProp(); set { if (SetBitFromProp(value)) DoPropertyChanged("R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7"); } }
        [SFRBit(nameof(PSW), 5, true)]
        public bool F0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PSW), 6, true)]
        public bool AC { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PSW), 7, true)]
        public bool CY { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion PSW

        #region SP
        [SFR(0x81)]
        public byte SP { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion SP

        #region DPTR
        [SFR16(nameof(DPH), nameof(DPL))]
        public ushort DPTR { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }

        [SFR(0x83)]
        public byte DPH { get => GetMemFromProp(); set { SetMemFromProp(value); } }

        [SFR(0x82)]
        public byte DPL { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion DPTR

        #region P0
        [SFR(0x80)]
        public byte P0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion P0

        #region P1
        [SFR(0x90)]
        public byte P1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion P1

        #region P2
        [SFR(0xA0)]
        public byte P2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion P2

        #region P3
        [SFR(0xB0)]
        public byte P3 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(P3), 0)]
        public bool RXD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 1)]
        public bool TXD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 2)]
        public bool INT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 3)]
        public bool INT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 4)]
        public bool T0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 5)]
        public bool T1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 6)]
        public bool WR { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P3), 7)]
        public bool RD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P3

        #region IEN0
        [SFR(0xA8)]
        public byte IEN0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(IEN0), 0, true)]
        public bool EX0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 1, true)]
        public bool ET0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 2, true)]
        public bool EX1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 3, true)]
        public bool ET1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 4, true)]
        public bool ES0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 7, true)]
        public bool EA { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IEN0

        #region IP0
        [SFR(0xB8)]
        public byte IP0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(IP0), 0, true)]
        public bool PX0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP0), 1, true)]
        public bool PT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP0), 2, true)]
        public bool PX1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP0), 3, true)]
        public bool PT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP0), 4, true)]
        public bool PS0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IP0

        #region TMOD
        [SFR(0x89)]
        public byte TMOD { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(TMOD), 0)]
        public bool M0_0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 1)]
        public bool M1_0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 2)]
        public bool C_T_0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 3)]
        public bool GATE_0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 4)]
        public bool M0_1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 5)]
        public bool M1_1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 6)]
        public bool C_T_1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TMOD), 7)]
        public bool GATE_1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion TMOD

        #region TCON
        [SFR(0x88)]
        public byte TCON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(TCON), 0, true)]
        public bool IT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 1, true)]
        public bool IE0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 2, true)]
        public bool IT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 3, true)]
        public bool IE1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 4, true)]
        public bool TR0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 5, true)]
        public bool TF0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 6, true)]
        public bool TR1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TCON), 7, true)]
        public bool TF1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion TCON

        #region TM0
        [SFR16(nameof(TH0), nameof(TL0))]
        public ushort TM0 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0x8c)]
        public byte TH0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0x8a)]
        public byte TL0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion TM0

        #region TM1
        [SFR16(nameof(TH1), nameof(TL1))]
        public ushort TM1 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0x8d)]
        public byte TH1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0x8b)]
        public byte TL1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion TM1

        #region S0CON
        [SFR(0x98)]
        public byte S0CON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(S0CON), 0, true)]
        public bool RI { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 1, true)]
        public bool TI { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 2, true)]
        public bool RB8 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 3, true)]
        public bool TB8 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 4, true)]
        public bool REN { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 5, true)]
        public bool SM2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 6, true)]
        public bool SM1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S0CON), 7, true)]
        public bool SM0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion S0CON

        #region S0BUF
        [SFR(0x99)]
        public byte S0BUF { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion S0BUF

        #region PCON
        [SFR(0x87)]
        public byte PCON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(PCON), 0)]
        public bool IDLE { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PCON), 1)]
        public bool PDE { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PCON), 2)]
        public bool GF0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PCON), 3)]
        public bool GF1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(PCON), 7)]
        public bool SMOD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion PCON

        public virtual void SaveAdditionalSettings(IDictionary<string, object> additionalSettings) { }
        public virtual void LoadAdditionalSettings(IDictionary<string, object> additionalSettings) { }
    }
}

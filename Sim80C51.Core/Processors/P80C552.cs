﻿using Sim80C51.Interfaces;
using Sim80C51.Processors.Attributes;
using Sim80C51.Toolbox;
using System.ComponentModel;

namespace Sim80C51.Processors
{
    [DisplayName("Philips 80C552")]
    [IV(0x002B, 2, "S1")]
    [IV(0x0033, 5, "CT0")]
    [IV(0x003B, 8, "CT1")]
    [IV(0x0043, 11, "CT2")]
    [IV(0x004B, 14, "CT3")]
    [IV(0x0053, 3, "AD")]
    [IV(0x005B, 6, "CM0")]
    [IV(0x0063, 9, "CM1")]
    [IV(0x006B, 12, "CM2")]
    [IV(0x0073, 15, "T2")]
    public class P80C552 : C80C51, IP80C552
    {
        public const int RAM_SIZE = 256;
        public const int SFR_SIZE = 128;
        private const byte I2C_RESET = 0xf8;
        private const byte I2C_START = 0x08;
        private const byte I2C_REPEAT_START = 0x10;
        private const byte I2C_SLA_W_ACK = 0x18;
        private const byte I2C_SLA_W_NACK = 0x20;
        private const byte I2C_S1DAT_W_ACK = 0x28;
        private const byte I2C_S1DAT_W_NACK = 0x30;
        private const byte I2C_SLA_R_ACK = 0x40;
        private const byte I2C_SLA_R_NACK = 0x48;
        private const byte I2C_S1DAT_R_ACK = 0x50;
        private const byte I2C_S1DAT_R_NACK = 0x58;
        private const byte I2C_DATA_LEN = 9; // 8 Bit + ACK

        #region ADCH
        [SFR(0xC6, 0x00)]
        public byte ADCH { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion ADCH

        #region ADCON
        [SFR(0xC5, 0x00)]
        public byte ADCON
        {
            get => GetMemFromProp(); 
            set
            {
                bool oldAdcs = ADCS;
                SetMemFromProp(value);
                if (!oldAdcs && ADCS)
                {
                    StartAdcConversion();
                }
            }
        }
        [SFRBit(nameof(ADCON), 0)]
        public bool AADR0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 1)]
        public bool AADR1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 2)]
        public bool AADR2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 3)]
        public bool ADCS { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 4)]
        public bool ADCI { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 5)]
        public bool ADEX { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 6)]
        public bool ADC_0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(ADCON), 7)]
        public bool ADC_1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion ADCON

        #region CTCON
        [SFR(0xEB, 0x00)]
        public byte CTCON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(CTCON), 0)]
        public bool CTP0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 1)]
        public bool CTN0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 2)]
        public bool CTP1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 3)]
        public bool CTN1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 4)]
        public bool CTP2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 5)]
        public bool CTN2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 6)]
        public bool CTP3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(CTCON), 7)]
        public bool CTN3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion CTCON

        #region CT3
        [SFR16(nameof(CTH3), nameof(CTL3))]
        public ushort CT3 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCF, 0x00)]
        public byte CTH3 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAF, 0x00)]
        public byte CTL3 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CT3

        #region CT2
        [SFR16(nameof(CTH2), nameof(CTL2))]
        public ushort CT2 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCE, 0x00)]
        public byte CTH2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAE, 0x00)]
        public byte CTL2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CT2

        #region CT1
        [SFR16(nameof(CTH1), nameof(CTL1))]
        public ushort CT1 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCD, 0x00)]
        public byte CTH1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAD, 0x00)]
        public byte CTL1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CT1

        #region CT0
        [SFR16(nameof(CTH0), nameof(CTL0))]
        public ushort CT0 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCC, 0x00)]
        public byte CTH0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAC, 0x00)]
        public byte CTL0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CT0

        #region CM2
        [SFR16(nameof(CMH2), nameof(CML2))]
        public ushort CM2 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCB, 0x00)]
        public byte CMH2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAB, 0x00)]
        public byte CML2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CM2

        #region CM1
        [SFR16(nameof(CMH1), nameof(CML1))]
        public ushort CM1 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xCA, 0x00)]
        public byte CMH1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xAA, 0x00)]
        public byte CML1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CM1

        #region CM0
        [SFR16(nameof(CMH0), nameof(CML0))]
        public ushort CM0 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xC9, 0x00)]
        public byte CMH0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xA9, 0x00)]
        public byte CML0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion CM0

        #region IEN0
        [SFRBit(nameof(IEN0), 5, true)]
        public bool ES1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN0), 6, true)]
        public bool EAD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IEN0

        #region IEN1
        [SFR(0xE8, 0x00)]
        public byte IEN1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(IEN1), 0, true)]
        public bool ECT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 1, true)]
        public bool ECT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 2, true)]
        public bool ECT2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 3, true)]
        public bool ECT3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 4, true)]
        public bool ECM0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 5, true)]
        public bool ECM1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 6, true)]
        public bool ECM2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IEN1), 7, true)]
        public bool ET2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IEN1

        #region IP0
        [SFRBit(nameof(IP0), 5, true)]
        public bool PS1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP0), 6, true)]
        public bool PAD { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IP0

        #region IP1
        [SFR(0xF8, 0x00)]
        public byte IP1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(IP1), 0, true)]
        public bool PCT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 1, true)]
        public bool PCT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 2, true)]
        public bool PCT2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 3, true)]
        public bool PCT3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 4, true)]
        public bool PCM0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 5, true)]
        public bool PCM1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 6, true)]
        public bool PCM2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(IP1), 7, true)]
        public bool PT2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion IP1

        #region P5
        [SFR(0xC4, 0x00)]
        public byte P5 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(P5), 0)]
        public bool ADC0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 1)]
        public bool ADC1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 2)]
        public bool ADC2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 3)]
        public bool ADC3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 4)]
        public bool ADC4 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 5)]
        public bool ADC5 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 6)]
        public bool ADC6 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P5), 7)]
        public bool ADC7 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P5

        #region P4
        [SFR(0xC0, 0xff)]
        public byte P4 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(P4), 0)]
        public bool CMSR0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 1)]
        public bool CMSR1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 2)]
        public bool CMSR2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 3)]
        public bool CMSR3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 4)]
        public bool CMSR4 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 5)]
        public bool CMSR5 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 6)]
        public bool CMT0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P4), 7)]
        public bool CMT1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P4

        #region P2
        [SFRBit(nameof(P2), 0)]
        public bool A8 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 1)]
        public bool A9 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 2)]
        public bool A10 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 3)]
        public bool A11 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 4)]
        public bool A12 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 5)]
        public bool A13 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 6)]
        public bool A14 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P2), 7)]
        public bool A15 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P2

        #region P1
        [SFRBit(nameof(P1), 0)]
        public bool CT0I { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 1)]
        public bool CT1I { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 2)]
        public bool CT2I { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 3)]
        public bool CT3I { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 4)]
        public bool T2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 5)]
        public bool RT2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 6)]
        public bool SCL { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P1), 7)]
        public bool SDA { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P1

        #region P0
        [SFRBit(nameof(P0), 0)]
        public bool AD0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 1)]
        public bool AD1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 2)]
        public bool AD2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 3)]
        public bool AD3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 4)]
        public bool AD4 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 5)]
        public bool AD5 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 6)]
        public bool AD6 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(P0), 7)]
        public bool AD7 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion P0

        #region PCON
        [SFRBit(nameof(PCON), 4)]
        public bool WLE { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion PCON

        #region PWMP
        [SFR(0xFE, 0x00)]
        public byte PWMP { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion PWMP

        #region PWM1
        [SFR(0xFD, 0x00)]
        public byte PWM1 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion PWM1

        #region PWM0
        [SFR(0xFC, 0x00)]
        public byte PWM0 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion PWM0

        #region RTE
        [SFR(0xEF, 0x00)]
        public byte RTE { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(RTE), 0)]
        public bool RP40 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 1)]
        public bool RP41 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 2)]
        public bool RP42 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 3)]
        public bool RP43 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 4)]
        public bool RP44 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 5)]
        public bool RP45 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 6)]
        public bool TP46 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(RTE), 7)]
        public bool TP47 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion RTE

        #region S1ADR
        [SFR(0xDB, 0x00)]
        public byte S1ADR { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(S1ADR), 0)]
        public bool GC { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion S1ADR

        #region S1DAT
        [SFR(0xDA, 0x00)]
        public byte S1DAT { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion S1DAT

        #region S1STA
        [SFR(0xD9, 0xf8)]
        public byte S1STA { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(S1STA), 3)]
        public bool SC0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1STA), 4)]
        public bool SC1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1STA), 5)]
        public bool SC2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1STA), 6)]
        public bool SC3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1STA), 7)]
        public bool SC4 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion S1STA

        #region S1CON
        [SFR(0xD8, 0x00)]
        public byte S1CON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(S1CON), 0, true)]
        public bool CR0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 1, true)]
        public bool CR1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 2, true)]
        public bool AA { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 3, true)]
        public bool SI { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 4, true)]
        public bool STO { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 5, true)]
        public bool STA { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 6, true)]
        public bool ENS1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(S1CON), 7, true)]
        public bool CR2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion S1CON

        #region STE
        [SFR(0xEE,0xc0)]
        public byte STE { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(STE), 0)]
        public bool SP40 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 1)]
        public bool SP41 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 2)]
        public bool SP42 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 3)]
        public bool SP43 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 4)]
        public bool SP44 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 5)]
        public bool SP45 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 6)]
        public bool TG46 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(STE), 7)]
        public bool TG47 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion STE

        #region TM2
        [SFR16(nameof(TMH2), nameof(TML2))]
        public ushort TM2 { get => GetMem16FromProp(); set { SetMem16FromProp(value); } }
        [SFR(0xED, 0x00)]
        public byte TMH2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFR(0xEC, 0x00)]
        public byte TML2 { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        #endregion TM2

        #region TM2CON
        [SFR(0xEA, 0x00)]
        public byte TM2CON { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(TM2CON), 0)]
        public bool T2MS0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 1)]
        public bool T2MS1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 2)]
        public bool T2P0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 3)]
        public bool T2P1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 4)]
        public bool T2B0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 5)]
        public bool T2ER { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 6)]
        public bool T2IS0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2CON), 7)]
        public bool T2IS1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion TM2CON

        #region TM2IR
        [SFR(0xC8, 0x00)]
        public byte TM2IR { get => GetMemFromProp(); set { SetMemFromProp(value); } }
        [SFRBit(nameof(TM2IR), 0, true)]
        public bool CTI0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 1, true)]
        public bool CTI1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 2, true)]
        public bool CTI2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 3, true)]
        public bool CTI3 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 4, true)]
        public bool CMI0 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 5, true)]
        public bool CMI1 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 6, true)]
        public bool CMI2 { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        [SFRBit(nameof(TM2IR), 7, true)]
        public bool T20V { get => GetBitFromProp(); set { SetBitFromProp(value); } }
        #endregion TM2IR

        #region T3 / Watchdog

        /// <summary>
        /// ! Enable Watchdog Timer
        /// </summary>
        public bool EW { get => ew; set { ew = value; DoPropertyChanged(); } }
        private bool ew = true;

        [SFR(0xFF, 0x00)]
        public byte T3
        {
            get => GetMemFromProp();
            set
            {
                if(!EW && WLE)
                {
                    t3Prescaler = 0;
                    SetMemFromProp(value);
                    WLE = false;
                }
            }
        }

        private ushort t3Prescaler = 0;

        private void StepWatchdog()
        {
            if (EW)
            {
                return;
            }

            t3Prescaler++;
            if (t3Prescaler >= 0x800) // 11-bit prescaler
            {
                byte oldValue = T3;
                oldValue++;
                if (oldValue == 0)
                {
                    Reset();
                    return;
                }

                SetMem(nameof(T3), oldValue);
                DoPropertyChanged(nameof(T3));
                t3Prescaler = 0;
            }
        }


        #endregion T3  / Watchdog

        public P80C552() : base(SFR_SIZE + RAM_SIZE)
        {
            AddIv("S1", () => PS1, () => ENS1 && SI);
            AddIv("CT0", () => PCT0, () => ECT0 && CTI0);
            AddIv("CT1", () => PCT1, () => ECT1 && CTI1);
            AddIv("CT2", () => PCT2, () => ECT2 && CTI2);
            AddIv("CT3", () => PCT3, () => ECT3 && CTI3);
            AddIv("AD", () => PAD, () => EAD && ADCI);
            AddIv("CM0", () => PCM0, () => ECM0 && CMI0);
            AddIv("CM1", () => PCM1, () => ECM1 && CMI1);
            AddIv("CM2", () => PCM2, () => ECM2 && CMI2);
            AddIv("T2", () => PT2, () => ET2 && ((T2IS1 && T20V) || (T2IS0 && T2B0)));
        }

        public override void Reset()
        {
            base.Reset();

            // Watchdog via internal
            SetMem(nameof(T3), 0);
            DoPropertyChanged(nameof(T3));
            t3Prescaler = 0;
            s1Prescaler = 0;
            s1DataCounter = 0;
            adcCycle = 0;
        }

        public override void SaveAdditionalSettings(IDictionary<string, object> additionalSettings)
        {
            additionalSettings.Add("ADC0", ADC0Value);
            additionalSettings.Add("ADC1", ADC1Value);
            additionalSettings.Add("ADC2", ADC2Value);
            additionalSettings.Add("ADC3", ADC3Value);
            additionalSettings.Add("ADC4", ADC4Value);
            additionalSettings.Add("ADC5", ADC5Value);
            additionalSettings.Add("ADC6", ADC6Value);
            additionalSettings.Add("ADC7", ADC7Value);
        }

        public override void LoadAdditionalSettings(IDictionary<string, object> additionalSettings)
        {
            ADC0Value = additionalSettings.TryGet("ADC0", 0);
            ADC1Value = additionalSettings.TryGet("ADC1", 0);
            ADC2Value = additionalSettings.TryGet("ADC2", 0);
            ADC3Value = additionalSettings.TryGet("ADC3", 0);
            ADC4Value = additionalSettings.TryGet("ADC4", 0);
            ADC5Value = additionalSettings.TryGet("ADC5", 0);
            ADC6Value = additionalSettings.TryGet("ADC6", 0);
            ADC7Value = additionalSettings.TryGet("ADC7", 0);
        }

        protected override void CpuCycle()
        {
            base.CpuCycle();
            StepWatchdog();
            StepAdc();
            StepS1();
        }

        #region S1 I2C
        public Predicate<string>? I2CCommandProcessor { get; set; }

        private int s1Prescaler = 0;
        private int s1DataCounter = 0;

        private void StepS1()
        {
            // no processing if IRQ set or not enabled
            if (SI || !ENS1)
            {
                return;
            }

            s1Prescaler++;
            if (s1Prescaler >= S1PrescalerSize)
            {
                s1Prescaler = 0;
                S1Process();
            }
        }

        private void S1Process()
        {
            if (STO)
            {
                STO = false;
                S1STA = I2C_RESET;
                s1DataCounter = 0;
                I2CCommandProcessor?.Invoke("STO");
                return;
            }

            if (STA)
            {
                if (S1STA == I2C_SLA_W_NACK ||
                    S1STA == I2C_S1DAT_W_ACK ||
                    S1STA == I2C_SLA_R_NACK ||
                    S1STA == I2C_S1DAT_R_NACK)
                {
                    S1STA = I2C_REPEAT_START;
                }
                else
                {
                    S1STA = I2C_START;
                }
                SI = true;
                I2CCommandProcessor?.Invoke("STA");
                // Start SLA + RW Transmit
                s1DataCounter = sizeof(byte) + 1; // size + ACK
                return;
            }

            if (s1DataCounter <= 0)
            {
                switch (S1STA)
                {
                    case (I2C_START):
                    case (I2C_REPEAT_START):
                        bool rw = (S1DAT & 0x01) != 0;
                        SI = true;
                        AA = I2CCommandProcessor?.Invoke("SLA") == true;
                        S1STA = rw ? (AA ? I2C_SLA_R_ACK : I2C_SLA_R_NACK) : (AA ? I2C_SLA_W_ACK : I2C_SLA_W_NACK);
                        // if write or ACK rceived. If read and no ACK received, do not transfer data.
                        if (!rw || AA)
                        {
                            // Start data transfer
                            s1DataCounter = I2C_DATA_LEN;
                        }
                        break;

                    case (I2C_SLA_W_NACK):
                    case (I2C_SLA_W_ACK):
                    case (I2C_S1DAT_W_NACK):
                    case (I2C_S1DAT_W_ACK):
                        SI = true;
                        AA = I2CCommandProcessor?.Invoke("DAT") == true;
                        S1STA = AA ? I2C_S1DAT_W_ACK : I2C_S1DAT_W_NACK;
                        // Start data transfer
                        s1DataCounter = I2C_DATA_LEN;
                        break;

                    case (I2C_SLA_R_ACK):
                    case (I2C_S1DAT_R_ACK):
                        SI = true;
                        I2CCommandProcessor?.Invoke("DAT");
                        S1STA = AA ? I2C_S1DAT_R_ACK : I2C_S1DAT_R_NACK;
                        // Start data transfer
                        s1DataCounter = I2C_DATA_LEN;
                        break;
                }
            }
            else
            {
                s1DataCounter--;
            }
        }

        private int S1PrescalerSize => (S1CON & 0x83) switch
        {
            // fOSC / 256
            0x00 => 43,
            // fOSC / 224
            0x01 => 37,
            // fOSC / 192
            0x02 => 32,
            // fOSC / 160
            0x03 => 27,
            // fOSC / 960
            0x80 => 160,
            // fOSC / 120
            0x81 => 20,
            // fOSC / 60
            0x82 => 10,
            _ => 0,
        };
        #endregion

        #region ADC Values
        public ushort ADC0Value { get => adc0Value; set { adc0Value = value; DoPropertyChanged(); } }
        private ushort adc0Value = 0;

        public ushort ADC1Value { get => adc1Value; set { adc1Value = value; DoPropertyChanged(); } }
        private ushort adc1Value = 0;

        public ushort ADC2Value { get => adc2Value; set { adc2Value = value; DoPropertyChanged(); } }
        private ushort adc2Value = 0;

        public ushort ADC3Value { get => adc3Value; set { adc3Value = value; DoPropertyChanged(); } }
        private ushort adc3Value = 0;

        public ushort ADC4Value { get => adc4Value; set { adc4Value = value; DoPropertyChanged(); } }
        private ushort adc4Value = 0;

        public ushort ADC5Value { get => adc5Value; set { adc5Value = value; DoPropertyChanged(); } }
        private ushort adc5Value = 0;

        public ushort ADC6Value { get => adc6Value; set { adc6Value = value; DoPropertyChanged(); } }
        private ushort adc6Value = 0;

        public ushort ADC7Value { get => adc7Value; set { adc7Value = value; DoPropertyChanged(); } }
        private ushort adc7Value = 0;

        private int adcCycle = 0;

        private void StepAdc()
        {
            if (adcCycle <= 0)
            {
                return;
            }
            adcCycle--;
            if (adcCycle == 0)
            {
                EndAdcConversation();
            }
        }

        private void StartAdcConversion()
        {
            // 8XC552 requires 50 cycles
            adcCycle = 50;
        }

        private void EndAdcConversation()
        {
            ushort adcValue = (ADCON & 0x07) switch
            {
                0 => ADC0Value,
                1 => ADC1Value,
                2 => ADC2Value,
                3 => ADC3Value,
                4 => ADC4Value,
                5 => ADC5Value,
                6 => ADC6Value,
                7 => ADC7Value,
                _ => 0,
            };

            // save upper 8 bit
            ADCH = (byte)(adcValue >> 2);
            // save lower 2 bit to ADCON[6,7]
            ADCON |= (byte)(adcValue << 6);
            ADCI = true;
            ADCS = false;
        }
        #endregion
    }
}

using Sim80C51.Interfaces;
using Sim80C51.Toolbox.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Markup;

namespace Sim80C51.TanningBed
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private MainWindow? owner;

        public IP80C552? CPU { get; set; }

        /// <summary>
        /// Left HC273 (Outputs) P1.2 -> CLK
        /// </summary>
        public byte HC273_0 { get => hc273_0; set { hc273_0 = value; DoPropertyChanged(); } }
        private byte hc273_0 = 0;

        /// <summary>
        /// Right HC273 (Outputs) P1.3 -> CLK
        /// </summary>
        public byte HC273_1 { get => hc273_1; set { hc273_1 = value; DoPropertyChanged(); } }
        private byte hc273_1 = 0;

        /// <summary>
        /// Right HC640 (Inputs) P1.4 -> !Enable Bus
        /// </summary>
        public byte HC640_0 { get => hc640_0; set { hc640_0 = value; DoPropertyChanged(); } }
        private byte hc640_0 = 0;

        /// <summary>
        /// Left HC640 (Inputs) P1.5 -> !Enable Bus
        /// </summary>
        public byte HC640_1 { get => hc640_1; set { hc640_1 = value; DoPropertyChanged(); } }
        private byte hc640_1 = 0;

        /// <summary>
        /// Control Unit bottom right PCF Address 0x20, WR = 0x40, RD = 0x41.
        /// Buttons inverted, 0 when pushed, 1 when not pushed
        /// </summary>
        public byte PCF8574_20 { get => pcf8574_20; set { pcf8574_20 = value; DoPropertyChanged(); } }
        private byte pcf8574_20 = 0;

        /// <summary>
        /// Control Unit bottom left PCF Address 0x21, WR = 0x42, RD = 0x43.
        /// Buttons inverted, 0 when pushed, 1 when not pushed
        /// </summary>
        public byte PCF8574_21 { get => pcf8574_21; set { pcf8574_21 = value; DoPropertyChanged(); } }
        private byte pcf8574_21 = 0;

        /// <summary>
        /// Control Unit left PCF Address 0x22, WR = 0x44, RD = 0x45.
        /// </summary>
        public byte PCF8574_22 { get => pcf8574_22; set { pcf8574_22 = value; DoPropertyChanged(); } }
        private byte pcf8574_22 = 0;

        public ICommand TH0Command => new RelayCommand((o) => { CPU!.TH0 = 0xff; });

        public void Loaded(MainWindow mainWindow)
        {
            if (owner != null)
            {
                return;
            }
            owner = mainWindow;
            CPU!.RegisterSfrChangeCallback(nameof(I80C51.P1), P1Update);
            CPU!.I2CCommandProcessor = I2cCommandProcessor;
            HC640_0 = 0b00011101;
            PCF8574_20 = 0xff;
            PCF8574_21 = 0x83;
        }

        private void P1Update()
        {
            byte ioBusSelector = (byte)(CPU!.P1 & 0b00111100);
            if (ioBusSelector != 0)
            {
                CheckIOBus(ioBusSelector);
            }
        }

        private string i2cLastState = string.Empty;
        private bool i2cRw = false;
        private byte i2cSla = 0;

        private bool I2cCommandProcessor(string command)
        {
            switch(command)
            {
                case "STO":
                case "STA":
                    i2cLastState = command;
                    break;
                case "SLA":
                    i2cLastState = "SLA";
                    return ProcessI2CSla();
                case "DAT":
                    i2cLastState = "DAT";
                    return ProcessI2CData();
            }

            return false;
        }

        private bool ProcessI2CSla()
        {
            i2cRw = (CPU!.S1DAT & 0x01) != 0;
            i2cSla = (byte)(CPU!.S1DAT >> 1);
            switch (i2cSla)
            {
                // bottom right PCF8574
                case 0x20:
                // bottom left PCF8574
                case 0x21:
                // left PCF8574
                case 0x22:
                // top right SAA1064
                case 0x38:
                // top left SAA1064
                case 0x3B:
                    return true;
            }
            return false;
        }

        private bool ProcessI2CData()
        {
            switch (i2cSla)
            {
                // bottom right PCF8574
                case 0x20:
                    if (i2cRw)
                    {
                        CPU!.S1DAT = PCF8574_20;
                    }
                    break;
                // bottom left PCF8574
                case 0x21:
                    if (i2cRw)
                    {
                        CPU!.S1DAT = PCF8574_21;
                    }
                    break;
                // left PCF8574
                case 0x22:
                    break;
                // top right SAA1064
                case 0x38:
                    if (i2cRw)
                    {
                        CPU!.S1DAT = 0x00;
                    }
                    break;
                // top left SAA1064
                case 0x3B:
                    if (i2cRw)
                    {
                        CPU!.S1DAT = 0x00;
                    }
                    break;
            }
            return false;
        }

        bool inUpdate = false;

        private void CheckIOBus(byte busSelector)
        {
            if (inUpdate)
            {
                return; 
            }
            inUpdate = true;
            switch (busSelector)
            {
                // P1.2 On -> CLK Bus on Left HC273 (Outputs)
                case 0b00110100:
                    HC273_0 = GetIOBus();
                    break;
                // P1.3 On -> CLK Bus on Right HC273 (Outputs)
                case 0b00111000:
                    HC273_1 = GetIOBus();
                    break;
                // P1.4 Off -> !Enable Bus on Right HC640 (Inputs)
                case 0b00100000:
                    SetIOBus(HC640_0);
                    break;
                // P1.5 Off -> !Enable Bus on Left HC640 (Inputs)
                case 0b00010000:
                    SetIOBus(HC640_1);
                    break;
            }
            inUpdate = false;
        }

        private void SetIOBus(byte data)
        {
            CPU!.P1 = (byte)((CPU!.P1 & 0xFC) | (data & 0x03));
            CPU!.P4 = (byte)((CPU!.P4 & 0x03) | (data & 0xFC));
        }

        private byte GetIOBus()
        {
            return (byte)((CPU!.P1 & 0x03) | (CPU!.P4 & 0xFC));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void DoPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

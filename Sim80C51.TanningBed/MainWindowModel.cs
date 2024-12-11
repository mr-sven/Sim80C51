using Sim80C51.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sim80C51.Devices;

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
        /// Inverted inputs
        /// </summary>
        public byte HC640_0 { get => hc640_0; set { hc640_0 = value; DoPropertyChanged(); } }
        private byte hc640_0 = 0;

        /// <summary>
        /// Left HC640 (Inputs) P1.5 -> !Enable Bus
        /// Inverted inputs
        /// </summary>
        public byte HC640_1 { get => hc640_1; set { hc640_1 = value; DoPropertyChanged(); } }
        private byte hc640_1 = 0;

        /// <summary>
        /// Control Unit bottom right PCF Address 0x20, WR = 0x40, RD = 0x41.
        /// Buttons inverted, 0 when pushed, 1 when not pushed
        /// </summary>
        public PCF8574 PCF8574_20 { get; } = new PCF8574(false, false, false, 0);
        /// <summary>
        /// Control Unit bottom left PCF Address 0x21, WR = 0x42, RD = 0x43.
        /// Buttons inverted, 0 when pushed, 1 when not pushed
        /// </summary>
        public PCF8574 PCF8574_21 { get; } = new PCF8574(true, false, false, 0);

        /// <summary>
        /// Control Unit left PCF Address 0x22, WR = 0x44, RD = 0x45.
        /// </summary>
        public PCF8574 PCF8574_22 { get; } = new PCF8574(false, true, false, 0xff);

        public SAA1064 SAA1064_38 { get; } = new(false, false);

        public SAA1064 SAA1064_3B { get; } = new(true, true);

        private readonly II2CDevice[] i2CDevices;

        public MainWindowModel()
        {
            i2CDevices =
            [
                PCF8574_20,
                PCF8574_21,
                PCF8574_22,
                SAA1064_38,
                SAA1064_3B
            ];
        }

        public void Loaded(MainWindow mainWindow)
        {
            if (owner != null)
            {
                return;
            }
            owner = mainWindow;
            CPU!.RegisterSfrChangeCallback(nameof(I80C51.P1), P1Update);
            CPU!.I2CCommandProcessor = I2cCommandProcessor;

            HC640_0 = 0b10011101;
            HC640_1 = 0b00000001;
            PCF8574_21.PIn = 0x83;
        }

        private void P1Update()
        {
            byte ioBusSelector = (byte)(CPU!.P1 & 0b00111100);
            if (ioBusSelector != 0)
            {
                CheckIOBus(ioBusSelector);
            }
        }

        private bool I2cCommandProcessor(string command)
        {
            switch(command)
            {
                case "STA":
                    break;
                case "SLA":
                    return ProcessI2CSla();
                case "DAT":
                    return ProcessI2CData();
                case "STO":
                    foreach (II2CDevice device in i2CDevices)
                    {
                        device.Stop();
                    }
                    break;
            }

            return false;
        }

        private bool ProcessI2CSla()
        {
            bool result = false;
            foreach (II2CDevice device in i2CDevices)
            {
                result |= device.Sla(CPU!.S1DAT);
            }
            return result;
        }

        private bool ProcessI2CData()
        {
            byte data = CPU!.S1DAT;
            bool result = false;
            foreach (II2CDevice device in i2CDevices)
            {
                result |= device.Data(ref data);
            }
            CPU!.S1DAT = data;
            return result;
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

using Sim80C51.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private byte hc640_0 = 0b00011101;

        /// <summary>
        /// Left HC640 (Inputs) P1.5 -> !Enable Bus
        /// </summary>
        public byte HC640_1 { get => hc640_1; set { hc640_1 = value; DoPropertyChanged(); } }
        private byte hc640_1 = 0;

        public void Loaded(MainWindow mainWindow)
        {
            if (owner != null)
            {
                return;
            }
            owner = mainWindow;
            CPU!.PropertyChanged += CPU_PropertyChanged;
        }

        private void CPU_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "P1":
                    byte ioBusSelector = (byte)(CPU!.P1 & 0b00111100);
                    if (ioBusSelector != 0)
                    {
                        CheckIOBus(ioBusSelector);
                    }
                    break;
            }
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

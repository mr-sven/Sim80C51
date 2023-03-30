using Sim80C51.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sim80C51.Devices
{
    public class SAA1064 : INotifyPropertyChanged, II2CDevice
    {
        public byte Digit1 { get => digit1; set { digit1 = value; DoPropertyChanged(); } }
        private byte digit1;

        public byte Digit2 { get => digit2; set { digit2 = value; DoPropertyChanged(); } }
        private byte digit2;

        public byte Digit3 { get => digit3; set { digit3 = value; DoPropertyChanged(); } }
        private byte digit3;

        public byte Digit4 { get => digit4; set { digit4 = value; DoPropertyChanged(); } }
        private byte digit4;

        private bool rw = false;
        private bool recv = false;
        private int subAddress = 0;
        private readonly byte slaveAddress = 0x38;

        public SAA1064(bool a0, bool a1)
        {
            slaveAddress |= (byte)(a0 ? 1 : 0);
            slaveAddress |= (byte)(a1 ? 2 : 0);            
        }

        public bool Sla(byte data)
        {
            byte i2cSla = (byte)(data >> 1);
            if (i2cSla != slaveAddress)
            {
                recv = false;
                return false;
            }
            recv = true;
            rw = (data & 0x01) != 0;
            subAddress = 0;
            return true;
        }

        public bool Data(ref byte data)
        {
            if (!recv)
            {
                return false;
            }

            if (rw)
            {
                data = 0x00;
                return true;
            }

            switch (subAddress)
            {
                case 0:
                    subAddress = (data & 0x07);
                    break;
                case 1:
                    // TODO: check control bits
                    break;
                case 2:
                    Digit1 = data;
                    break;
                case 3:
                    Digit2 = data;
                    break;
                case 4:
                    Digit3 = data;
                    break;
                case 5:
                    Digit4 = data;
                    break;
            }
            subAddress++;
            return true;
        }

        public void Stop()
        {
            recv = false;
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

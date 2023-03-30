using Sim80C51.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sim80C51.Devices
{
    public class PCF8574 : INotifyPropertyChanged, II2CDevice
    {
        public byte POut { get => pOut; set { pOut = value; DoPropertyChanged(); } }
        private byte pOut = 0xff;

        public byte PIn { get => pIn; set { pIn = value; DoPropertyChanged(); } }
        private byte pIn = 0xff;

        private bool rw = false;
        private bool recv = false;
        private readonly byte slaveAddress = 0x20;
        private readonly byte dir;

        public PCF8574(bool a0, bool a1, bool a2, byte dir)
        {
            slaveAddress |= (byte)(a0 ? 1 : 0);
            slaveAddress |= (byte)(a1 ? 2 : 0);
            slaveAddress |= (byte)(a2 ? 4 : 0);
            this.dir = dir;
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
                data = (byte)(POut & dir);
                data |= (byte)(PIn & ~dir);
                return true;
            }
            POut = data;
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

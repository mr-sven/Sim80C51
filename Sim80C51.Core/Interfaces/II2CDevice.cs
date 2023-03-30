
namespace Sim80C51.Interfaces
{
    public interface II2CDevice
    {
        bool Sla(byte data);
        bool Data(ref byte data);
        void Stop();
    }
}

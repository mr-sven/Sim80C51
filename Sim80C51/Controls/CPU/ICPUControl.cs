using Sim80C51.Interfaces;

namespace Sim80C51.Controls.CPU
{
    public interface ICPUControl
    {
        I80C51? CPUContext { get; }
    }
}

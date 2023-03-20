using Sim80C51.Processors;

namespace Sim80C51.Controls.CPU
{
    public interface ICPUControl
    {
        I80C51Core? CPUContext { get; }
    }
}

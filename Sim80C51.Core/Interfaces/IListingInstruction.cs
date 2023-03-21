using Sim80C51.Processors;

namespace Sim80C51.Interfaces
{
    public interface IListingInstruction
    {
        InstructionType Instruction { get; }
        List<string> Arguments { get; }
        ushort TargetAddress { get; }
        string ArgumentString { get; }
        ushort Length { get; }
    }
}
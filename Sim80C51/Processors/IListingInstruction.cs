namespace Sim80C51.Processors
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
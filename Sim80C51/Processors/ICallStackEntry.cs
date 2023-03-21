namespace Sim80C51.Processors
{
    public interface ICallStackEntry
    {
        ushort Address { get; }
        byte StackPointer { get; }
    }
}
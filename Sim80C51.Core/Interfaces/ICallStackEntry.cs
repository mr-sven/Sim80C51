namespace Sim80C51.Interfaces
{
    public interface ICallStackEntry
    {
        ushort Address { get; }
        byte StackPointer { get; }
    }
}
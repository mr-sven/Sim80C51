namespace Sim80C51.Processors
{
    public class CallStackEntry
    {
        public ushort Address { get; set; }
        public byte StackPointer { get; set; }

        public CallStackEntry() { }
        public CallStackEntry(ushort address, byte stackPointer) : base()
        {
            Address = address;
            StackPointer = stackPointer;
        }
    }
}

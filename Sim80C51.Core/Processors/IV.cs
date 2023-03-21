namespace Sim80C51.Processors
{
    public class IV
    {
        public IV(ushort vectorAddress, Func<bool> priorityBit, Func<bool> check, Action? clear = null)
        {
            VectorAddress = vectorAddress;
            PriorityBit = priorityBit;
            Check = check;
            Clear = clear;
        }

        public ushort VectorAddress { get; }
        public Func<bool> PriorityBit { get; }
        public Func<bool> Check { get; }
        public Action? Clear { get; }
    }
}

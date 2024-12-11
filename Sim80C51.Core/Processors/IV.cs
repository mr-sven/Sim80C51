namespace Sim80C51.Processors
{
    public class IV(ushort vectorAddress, Func<bool> priorityBit, Func<bool> check, Action? clear = null)
    {
        public ushort VectorAddress => vectorAddress;
        public Func<bool> PriorityBit => priorityBit;
        public Func<bool> Check => check;
        public Action? Clear => clear;
    }
}

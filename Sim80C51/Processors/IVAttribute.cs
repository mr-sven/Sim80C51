namespace Sim80C51.Processors
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class IVAttribute : Attribute
    {
        public ushort Address { get; }
        public byte Priority { get; }

        public IVAttribute(ushort Address, byte Priority)
        {
            this.Address = Address;
            this.Priority = Priority;
        }
    }
}

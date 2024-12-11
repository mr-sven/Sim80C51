namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IVAttribute(ushort address, byte priority, string name) : Attribute
    {
        public ushort Address => address;
        public byte Priority => priority;
        public string Name => name;
    }
}

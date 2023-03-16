namespace Sim80C51.Processors
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IVAttribute : Attribute
    {
        public ushort Address { get; }
        public byte Priority { get; }
        public string Name { get; }

        public IVAttribute(ushort address, byte priority, string name)
        {
            Address = address;
            Priority = priority;
            Name = name;
        }
    }
}

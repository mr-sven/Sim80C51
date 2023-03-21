namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRAttribute : Attribute
    {
        public ushort Address { get; }

        public bool ForceUpdate { get; }

        public SFRAttribute(ushort address, bool forceUpdate = false)
        {
            Address = address;
            ForceUpdate = forceUpdate;
        }
    }
}

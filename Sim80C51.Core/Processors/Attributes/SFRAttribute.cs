namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRAttribute : Attribute
    {
        public ushort Address { get; }

        public SFRAttribute(ushort Address)
        {
            this.Address = Address;
        }
    }
}

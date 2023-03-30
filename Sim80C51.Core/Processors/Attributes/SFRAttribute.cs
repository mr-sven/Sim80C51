namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRAttribute : Attribute
    {
        public ushort Address { get; }

        public byte ResetValue { get; }

        public SFRAttribute(ushort address, byte resetValue)
        {
            Address = address;
            ResetValue = resetValue;
        }
    }
}

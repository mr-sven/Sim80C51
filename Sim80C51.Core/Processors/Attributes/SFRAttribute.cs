namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRAttribute(ushort address, byte resetValue) : Attribute
    {
        public ushort Address => address;

        public byte ResetValue => resetValue;
    }
}

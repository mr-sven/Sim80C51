namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRBitAttribute : Attribute
    {
        public string SFRName { get; }
        public byte Bit { get; }
        public bool Addressable { get; }

        public SFRBitAttribute(string SFRName, byte Bit, bool Addressable = false)
        {
            this.SFRName = SFRName;
            this.Bit = Bit;
            this.Addressable = Addressable;
        }
    }
}

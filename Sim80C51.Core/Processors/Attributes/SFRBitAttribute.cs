namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFRBitAttribute(string sfrName, byte bit, bool addressable = false) : Attribute
    {
        public string SFRName => sfrName;
        public byte Bit => bit;
        public bool Addressable => addressable;
    }
}

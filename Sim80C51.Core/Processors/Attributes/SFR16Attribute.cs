namespace Sim80C51.Processors.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFR16Attribute(string sfrHName, string sfrLName) : Attribute
    {
        public string SFRHName => sfrHName;
        public string SFRLName => sfrLName;
    }
}

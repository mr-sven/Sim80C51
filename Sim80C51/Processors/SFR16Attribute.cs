namespace Sim80C51.Processors
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SFR16Attribute : Attribute
    {
        public string SFRHName { get; }
        public string SFRLName { get; }

        public SFR16Attribute(string SFRHName, string SFRLName)
        {
            this.SFRHName = SFRHName;
            this.SFRLName = SFRLName;
        }
    }
}

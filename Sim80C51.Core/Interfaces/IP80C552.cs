namespace Sim80C51.Interfaces
{
    public interface IP80C552 : I80C51
    {
        /// <summary>
        /// Port 4
        /// </summary>
        byte P4 { get; set; }

        /// <summary>
        /// I2C Port data
        /// </summary>
        byte S1DAT { get; set; }


        Predicate<string>? I2CCommandProcessor { get; set; }
    }
}
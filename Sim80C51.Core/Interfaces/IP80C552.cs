namespace Sim80C51.Interfaces
{
    public interface IP80C552 : I80C51
    {
        /// <summary>
        /// Port 4
        /// </summary>
        byte P4 { get; set; }

        Func<string, byte, byte>? I2CCommandProcessor { get; set; }
    }
}
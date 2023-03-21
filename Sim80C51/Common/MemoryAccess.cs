namespace Sim80C51.Common
{
    public class MemoryAccess
    {
        public bool Read { get; private set; }

        public bool Write { get; private set; }

        private MemoryAccess(bool read, bool write)
        {
            Read = read;
            Write = write;
        }

        public static implicit operator string(MemoryAccess acc) => acc.ToString();
        public static explicit operator MemoryAccess(string access) => new(access.Contains('R'), access.Contains('W'));

        public override string ToString() => $"{(Read ? "R" : "")}{(Write ? "W" : "")}";
    }
}

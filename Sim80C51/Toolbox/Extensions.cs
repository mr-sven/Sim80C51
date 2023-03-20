using System.Linq.Expressions;
using System.Reflection;

namespace Sim80C51.Toolbox
{
    public static class Extensions
    {
        public static string ToAnsiString(this byte[] data)
        {
            return new string(data.Select(c => Convert.ToChar(c < 0x20 ? '.' : c > 0x7e ? '.' : c)).ToArray());
        }

        public static string ToAnsiString(this List<byte> data)
        {
            return data.ToArray().ToAnsiString();
        }

        public static string ToEscapedAnsiString(this byte[] data)
        {
            string result = string.Empty;
            foreach (byte b in data)
            {
                result += b switch
                {
                    (byte)'\"' => "\\\"",
                    (byte)'\\' => @"\\",
                    (byte)'\0' => @"\0",
                    (byte)'\a' => @"\a",
                    (byte)'\b' => @"\b",
                    (byte)'\f' => @"\f",
                    (byte)'\n' => @"\n",
                    (byte)'\r' => @"\r",
                    (byte)'\t' => @"\t",
                    (byte)'\v' => @"\v",
                    >= 0x20 and (< 0x7f or > 0x7f) => Convert.ToChar(b),
                    _ => @"\x" + b.ToString("X2")
                };
            }
            return result;
        }

        public static string ToEscapedAnsiString(this List<byte> data)
        {
            return data.ToArray().ToEscapedAnsiString();
        }

        public static ushort TryGet(this Dictionary<string, object> data, string key, ushort defaultValue)
        {
            if (data.TryGetValue(key, out object? valueObject) && ushort.TryParse(valueObject.ToString(), out ushort valueShort))
            {
                return valueShort;
            }
            return defaultValue;
        }
    }
}

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Sim80C51.WSpace
{
    public class XMemConfig
    {
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string Memory { get; set; } = string.Empty;
        public bool M48TEnabled { get; set; } = false;
    }
}

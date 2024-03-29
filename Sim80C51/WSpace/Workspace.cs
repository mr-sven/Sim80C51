﻿using Sim80C51.Interfaces;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Sim80C51.WSpace
{
    public class Workspace
    {
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string InternalMemory { get; set; } = string.Empty;
        public ushort ProgramCounter { get; set; } = 0;
        public string ProcessorType { get; set; } = string.Empty;

        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string Listing { get; set; } = string.Empty;
        public Dictionary<ushort, XMemConfig> XMem { get; set; } = new();
        public List<ushort> Breakpoints { get; set; } = new();
        public Dictionary<string, object> AdditionalSettings { get; set; } = new();
        public Dictionary<ushort, string> MemoryWatches { get; set; } = new();
        public List<ICallStackEntry> CallStack { get; set; } = new();
    }
}

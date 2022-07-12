using Sim80C51.Toolbox.Wpf;
using System.Reflection;

namespace Sim80C51.Controls
{
    public class IRQMenuItem : NotifyPropertyChanged
    {
        public string Title { get => title; set { title = value; DoPropertyChanged(); } }
        private string title = string.Empty;

        public MethodInfo? Method { get => method; set { method = value; DoPropertyChanged(); } }
        private MethodInfo? method;

        public byte Priority { get => priority; set { priority = value; DoPropertyChanged(); } }
        private byte priority = 0;        
    }
}

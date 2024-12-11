using Sim80C51.Toolbox.Wpf;

namespace Sim80C51.Toolbox
{
    public sealed class ObservableKeyValuePair<TKey, TValue>(TKey key, TValue _value) : NotifyPropertyChanged
    {
        public TKey Key { get { return key; } set { key = value; DoPropertyChanged(); } }

        public TValue Value { get { return _value; } set { _value = value; DoPropertyChanged(); } }
    }
}

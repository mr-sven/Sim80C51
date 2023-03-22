using Sim80C51.Toolbox.Wpf;

namespace Sim80C51.Toolbox
{
    public sealed class ObservableKeyValuePair<TKey, TValue>: NotifyPropertyChanged
    {
        public TKey Key { get { return _key; } set { _key = value; DoPropertyChanged(); } }
        private TKey _key;

        public TValue Value { get { return _value; } set { _value = value; DoPropertyChanged(); } }
        private TValue _value;

        public ObservableKeyValuePair(TKey key, TValue value) { _key = key; _value = value; }
    }
}

using System.Collections.ObjectModel;

namespace Sim80C51.Toolbox
{
    public class ObservableDictionary<TKey, TValue> : ObservableCollection<ObservableKeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public TValue this[TKey Key]
        {
            get
            {
                if (!TryGetValue(Key, out TValue result))
                {
                    throw new ArgumentException("Key not found", nameof(Key));
                }
                return result;
            }
            set
            {
                if (ContainsKey(Key))
                {
                    GetPairByTheKey(Key).Value = value;
                }
                else
                {
                    Add(Key, value);
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException(string.Format("The dictionary already contains the key \"{0}\"", key));
            }

            Add(new ObservableKeyValuePair<TKey, TValue>(key, value));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (ContainsKey(item.Key))
            {
                throw new ArgumentException(string.Format("The dictionary already contains the key \"{0}\"", item.Key));
            }

            Add(item.Key, item.Value);
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            KeyValuePair<TKey, TValue>[] arrayofItems = items.ToArray();
            if (arrayofItems.Any(i => ContainsKey(i.Key)))
            {
                throw new ArgumentException(string.Format("The dictionary already contains the key \"{0}\"", arrayofItems.First(i => ContainsKey(i.Key)).Key));
            }

            foreach (KeyValuePair<TKey, TValue> item in arrayofItems)
            {
                Add(item.Key, item.Value);
            }
        }

        public void AddRange(IEnumerable<ObservableKeyValuePair<TKey, TValue>> items)
        {
            ObservableKeyValuePair<TKey, TValue>[] arrayofItems = items.ToArray();
            if (arrayofItems.Any(i => ContainsKey(i.Key)))
            {
                throw new ArgumentException(string.Format("The dictionary already contains the key \"{0}\"", arrayofItems.First(i => ContainsKey(i.Key)).Key));
            }

            foreach (ObservableKeyValuePair<TKey, TValue> item in arrayofItems)
            {
                Add(item);
            }
        }

        public bool Remove(TKey key)
        {
            List<ObservableKeyValuePair<TKey, TValue>> remove = ThisAsCollection().Where(pair => Equals(key, pair.Key)).ToList();
            foreach (ObservableKeyValuePair<TKey, TValue> pair in remove)
            {
                ThisAsCollection().Remove(pair);
            }

            return remove.Count > 0;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            ObservableKeyValuePair<TKey, TValue> pair = GetPairByTheKey(item.Key);
            if (Equals(pair, default(ObservableKeyValuePair<TKey, TValue>)))
            {
                return false;
            }

            return Equals(pair.Value, item.Value) && ThisAsCollection().Remove(pair);
        }

        public bool ContainsKey(TKey key)
        {
            return !Equals(default(ObservableKeyValuePair<TKey, TValue>), ThisAsCollection().FirstOrDefault(i => Equals(key, i.Key)));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            ObservableKeyValuePair<TKey, TValue> pair = GetPairByTheKey(item.Key);
            return !Equals(pair, default(ObservableKeyValuePair<TKey, TValue>)) && Equals(pair.Value, item.Value);
        }

        public ICollection<TKey> Keys
        {
            get { return (from i in ThisAsCollection() select i.Key).ToList(); }
        }

        public ICollection<TValue> Values
        {
            get { return (from i in ThisAsCollection() select i.Value).ToList(); }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default!;
            ObservableKeyValuePair<TKey, TValue>? pair = GetPairByTheKey(key);

            if (!Equals(pair, default(ObservableKeyValuePair<TKey, TValue>)))
            {
                return false;
            }

            value = pair!.Value;
            return true;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return (from i in ThisAsCollection() select new KeyValuePair<TKey, TValue>(i.Key, i.Value)).ToList().GetEnumerator();
        }

        private bool Equals(TKey firstKey, TKey secondKey)
        {
            return EqualityComparer<TKey>.Default.Equals(firstKey, secondKey);
        }

        private ObservableCollection<ObservableKeyValuePair<TKey, TValue>> ThisAsCollection()
        {
            return this;
        }

        private ObservableKeyValuePair<TKey, TValue> GetPairByTheKey(TKey key) => ThisAsCollection().FirstOrDefault(i => i.Key!.Equals(key))!;
    }
}

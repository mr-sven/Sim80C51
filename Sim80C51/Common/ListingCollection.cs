using System.Collections.ObjectModel;

namespace Sim80C51.Common
{
    public class ListingCollection : ObservableCollection<ListingEntry>
    {
        private readonly Dictionary<ushort, ListingEntry> entriesByAddress = [];
        private readonly Dictionary<string, ListingEntry> entriesByLabel = [];
        
        public new void Remove(ListingEntry entry)
        {
            entriesByAddress.Remove(entry.Address);
            if (!string.IsNullOrEmpty(entry.Label))
            {
                entriesByLabel.Remove(entry.Label);
            }
            base.Remove(entry);
        }

        public new void Clear()
        {
            entriesByAddress.Clear();
            entriesByLabel.Clear();
            base.Clear();
        }

        public new void Add(ListingEntry entry)
        {
            entriesByAddress.Add(entry.Address, entry);
            if (!string.IsNullOrEmpty(entry.Label))
            {
                entriesByLabel.Add(entry.Label, entry);
            }

            for (int i = 0; i < Count; i++)
            {
                if (this[i].Address > entry.Address)
                {
                    base.Insert(i, entry);
                    return;
                }
            }
            base.Add(entry);
        }

        public void RemoveByAddress(ushort address)
        {
            if (entriesByAddress.TryGetValue(address, out ListingEntry? toRemove))
            {
                base.Remove(toRemove);
                entriesByAddress.Remove(address);
                if (!string.IsNullOrEmpty(toRemove.Label))
                {
                    entriesByLabel.Remove(toRemove.Label);
                }
            }
        }

        public bool Contains(ushort address)
        {
            return entriesByAddress.ContainsKey(address);
        }

        public bool Contains(string label)
        {
            return entriesByLabel.ContainsKey(label);
        }

        public ListingEntry? GetByAddress(ushort address)
        {
            if (entriesByAddress.TryGetValue(address, out ListingEntry? entry))
            {
                return entry;
            }

            return null;
        }

        public ListingEntry? GetByLabel(string label)
        {
            if (entriesByLabel.TryGetValue(label, out ListingEntry? entry))
            {
                return entry;
            }
            return null;
        }

        public void SetLabel(ushort address, string label)
        {
            ListingEntry? entry = GetByAddress(address);
            if (entry == null || entry.Label == label || string.IsNullOrEmpty(label))
            {
                return;
            }
            SetLabel(entry, label);
        }

        public void SetLabel(ListingEntry entry, string label)
        {
            if (entry.Label == label || string.IsNullOrEmpty(label))
            {
                return;
            }

            if (string.IsNullOrEmpty(entry.Label))
            {
                entry.Label = label;
                entriesByLabel.Add(entry.Label, entry);
                return;
            }

            entriesByLabel.Remove(entry.Label);
            foreach (ListingEntry uEntry in Items.Where(e => e.Arguments.Count > 0 && e.Arguments.Last() == entry.Label))
            {
                uEntry.Arguments[^1] = label;
                uEntry.UpdateStrings();
            }

            entry.Label = label;
            entriesByLabel.Add(entry.Label, entry);
        }
    }
}

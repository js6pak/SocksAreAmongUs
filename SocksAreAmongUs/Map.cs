using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SocksAreAmongUs
{
    public class Map<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _forward;
        private readonly Dictionary<TValue, TKey> _reverse;

        public Map(IEqualityComparer<TKey> forwardComparer, IEqualityComparer<TValue> reverseComparer)
        {
            _forward = new Dictionary<TKey, TValue>(forwardComparer);
            _reverse = new Dictionary<TValue, TKey>(reverseComparer);

            Forward = new Indexer<TKey, TValue>(_forward);
            Reverse = new Indexer<TValue, TKey>(_reverse);
        }

        public class Indexer<TKey2, TValue2>
        {
            private readonly Dictionary<TKey2, TValue2> _dictionary;

            public Indexer(Dictionary<TKey2, TValue2> dictionary)
            {
                _dictionary = dictionary;
            }

            public TValue2 this[TKey2 index]
            {
                get => _dictionary[index];
                set => _dictionary[index] = value;
            }

            public bool TryGetValue(TKey2 key, [MaybeNullWhen(false)] out TValue2 value)
            {
                return _dictionary.TryGetValue(key, out value);
            }

            public bool Contains(TKey2 key)
            {
                return _dictionary.ContainsKey(key);
            }

            public bool Remove(TKey2 key)
            {
                return _dictionary.Remove(key);
            }
        }

        public void Add(TKey t1, TValue t2)
        {
            _forward.Add(t1, t2);
            _reverse.Add(t2, t1);
        }

        public TValue this[TKey index]
        {
            set
            {
                _forward[index] = value;
                _reverse[value] = index;
            }
        }

        public Indexer<TKey, TValue> Forward { get; }
        public Indexer<TValue, TKey> Reverse { get; }
    }
}

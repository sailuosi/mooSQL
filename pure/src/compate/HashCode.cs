# if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public struct HashCode
    {
        private int _hash;

        public static HashCode Start { get; } = new HashCode() { _hash = 17 };

        public void Add<T>(T value)
        {
            unchecked
            {
                _hash = _hash * 31 + (value?.GetHashCode() ?? 0);
            }
        }
        public void Add<T>(T value, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                Add(value);
                return;
            }

            unchecked
            {
                _hash = _hash * 31 + comparer.GetHashCode(value);
            }
        }
        public void AddBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                Add(0);
                return;
            }

            unchecked
            {
                foreach (var b in bytes)
                {
                    _hash = _hash * 31 + b;
                }
            }
        }

        public int ToHashCode() => _hash;

        public static int Combine<T1>(T1 value1)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                return hash;
            }
        }

        // 可继续添加更多Combine重载...
    }
}
#endif
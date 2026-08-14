using System;

namespace mooSQL.data
{
    /// <summary>
    /// 编排指纹累加器。net6+ 直接使用 <see cref="HashCode.Combine"/>；旧框架使用确定性混洗。
    /// 热路径零堆分配。
    /// </summary>
    public struct ScriptHash
    {
        private int _hash;

        public void Add(int value)
        {
#if NET6_0_OR_GREATER
            _hash = HashCode.Combine(_hash, value);
#else
            _hash = Combine(_hash, value);
#endif
        }

        public void Add(bool value)
        {
#if NET6_0_OR_GREATER
            _hash = HashCode.Combine(_hash, value);
#else
            Add(value ? 1 : 0);
#endif
        }

        public void Add(string value)
        {
#if NET6_0_OR_GREATER
            _hash = HashCode.Combine(_hash, value);
#else
            Add(value == null ? 0 : value.GetHashCode());
#endif
        }

        public void Add(object value)
        {
#if NET6_0_OR_GREATER
            _hash = HashCode.Combine(_hash, value);
#else
            if (value == null)
            {
                Add(0);
                return;
            }
            if (value is string)
            {
                Add((string)value);
                return;
            }
            if (value is int)
            {
                Add((int)value);
                return;
            }
            if (value is bool)
            {
                Add((bool)value);
                return;
            }
            Add(value.GetHashCode());
#endif
        }

        public int ToHashCode()
        {
            return _hash;
        }

#if !NET6_0_OR_GREATER
        /// <summary>仿 System.HashCode 混洗（确定性、无随机种子）。</summary>
        private static int Combine(int h1, int h2)
        {
            unchecked
            {
                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                return ((int)rol5 + h1) ^ h2;
            }
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
	/// <summary>
	/// 使用引用相等（<see cref="RuntimeHelpers.GetHashCode(object)"/>）比较实例，用于避免值语义比较。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
    public class ObjectReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
		/// <summary>单例比较器。</summary>
        public static IEqualityComparer<T> Default = new ObjectReferenceEqualityComparer<T>();

        #region IEqualityComparer<T> Members

		/// <inheritdoc />
        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

		/// <inheritdoc />
        public int GetHashCode(T obj)
        {
            if (obj == null)
                return 0;

            return RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}

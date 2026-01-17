using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    public class ObjectReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Default = new ObjectReferenceEqualityComparer<T>();

        #region IEqualityComparer<T> Members

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            if (obj == null)
                return 0;

            return RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}

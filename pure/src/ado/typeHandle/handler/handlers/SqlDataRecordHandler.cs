using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    internal sealed class SqlDataRecordHandler<T> : ITypeHandler
        where T : IDataRecord
    {
        public object Parse(Type destinationType, object value)
        {
            throw new NotSupportedException();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// SqlDataRecord 类型处理器
    /// </summary>
    /// <typeparam name="T">数据记录类型</typeparam>
    internal sealed class SqlDataRecordHandler<T> : ITypeParser
        where T : IDataRecord
    {
        public object Parse(Type destinationType, object value)
        {
            throw new NotSupportedException();
        }

    }
}

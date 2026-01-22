using System;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{

    /// <summary>
    /// 打包状态，包含哈希码和反序列化函数
    /// </summary>
    internal readonly struct PackUpState
    {
        public readonly int Hash;
        public readonly Func<DbDataReader, DBInstance, object> Func;

        public PackUpState(int hash, Func<DbDataReader, DBInstance, object> func)
        {
            Hash = hash;
            Func = func;
        }
    }
    
}

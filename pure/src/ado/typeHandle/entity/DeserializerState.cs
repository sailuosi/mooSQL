using System;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{

    internal readonly struct DeserializerState
    {
        public readonly int Hash;
        public readonly Func<DbDataReader, DBInstance, object> Func;

        public DeserializerState(int hash, Func<DbDataReader, DBInstance, object> func)
        {
            Hash = hash;
            Func = func;
        }
    }
    
}

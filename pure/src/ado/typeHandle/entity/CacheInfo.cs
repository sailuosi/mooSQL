using System;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace mooSQL.data
{

    internal class CacheInfo
    {
        public DeserializerState Deserializer { get; set; }


        private int hitCount;
        public int GetHitCount() { return Interlocked.CompareExchange(ref hitCount, 0, 0); }
        public void RecordHit() { Interlocked.Increment(ref hitCount); }
    }
    
}

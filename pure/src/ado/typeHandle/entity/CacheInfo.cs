using System;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace mooSQL.data
{

    /// <summary>
    /// 缓存信息，存储反序列化器状态和命中计数
    /// </summary>
    internal class CacheInfo
    {
        public PackUpState Deserializer { get; set; }


        private int hitCount;
        public int GetHitCount() { return Interlocked.CompareExchange(ref hitCount, 0, 0); }
        public void RecordHit() { Interlocked.Increment(ref hitCount); }
    }
    
}

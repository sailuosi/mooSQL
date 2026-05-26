using System;

namespace mooSQL.data
{
    public abstract partial class Dialect
    {
        /// <summary>
        /// 判定异常是否表示数据库连接失联，委托语句方言白名单。
        /// </summary>
        public virtual bool IsConnectionLost(Exception ex) => sentence?.IsConnectionLost(ex) ?? false;
    }
}

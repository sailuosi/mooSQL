using System;

namespace mooSQL.data
{
    /// <summary>
    /// <see cref="SQLCmd"/> 的物理执行段：一条 SQL + 一组参数。
    /// 单段与多段均由本类型承载；多段用于参数上限 / 批大小切片后的同连接顺序执行。
    /// </summary>
    public class SQLSeg
    {
        /// <summary>本段 SQL 文本。</summary>
        public string sql { get; set; }

        /// <summary>本段参数袋。</summary>
        public Paras para { get; set; }

        /// <summary>创建空段。</summary>
        public SQLSeg()
        {
            para = new Paras();
        }

        /// <summary>创建带 SQL 与参数的段（参数会 Copy）。</summary>
        public SQLSeg(string sql, Paras paras)
        {
            this.sql = sql;
            para = new Paras();
            if (paras != null)
                para.Copy(paras);
        }

        /// <summary>本段是否无有效 SQL。</summary>
        public bool Empty => string.IsNullOrWhiteSpace(sql);

        /// <summary>解析延迟参数并写回 <see cref="sql"/>。</summary>
        public SQLSeg EnsureLiveParasResolved()
        {
            if (para != null)
                sql = para.ResolveDelayParas(sql ?? "");
            return this;
        }
    }
}

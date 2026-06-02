using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 本类可直接输出结果，而不是构造基础的SQL
    /// </summary>
    public abstract partial class SQLSentence
    {
        /// <summary>
        /// 查询数据库列表的 SQL 模板。
        /// </summary>
        public virtual string GetDataBaseSql { get; }

        /// <summary>
        /// 查询视图列表的 SQL 模板。
        /// </summary>
        public virtual string GetViewInfoListSql { get; }

        /// <summary>
        /// 查询表列表的 SQL 模板。
        /// </summary>
        public virtual string GetTableInfoListSql { get; }

        /// <summary>
        /// 按表名查询列信息的 SQL 模板。
        /// </summary>
        public virtual string GetColumnInfosByTableNameSql { get; }

        /// <summary>
        /// 按表名查询列标题/注释的 SQL 模板。
        /// </summary>
        public virtual string GetColumnCaptionsByTableNameSql { get; }


        /// <summary>
        /// 字符串类型默认长度（DDL 缺省）。
        /// </summary>
        protected virtual int DefultLength { get; set; } = 255;


        /// <summary>
        /// 数值类型默认长度/精度占位（DDL 缺省）。
        /// </summary>
        protected virtual int DefultNumLength { get; set; } = 11;
    }
}
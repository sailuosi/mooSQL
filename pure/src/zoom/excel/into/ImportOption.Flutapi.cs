// 基础功能说明：

using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public partial class ImportOption
    {
        //服务端配置的助手类方法
        /// <summary>追加一张仅指定物理表名的空表配置并返回该表。</summary>
        /// <param name="name">表名（同时作为 key、DBName）。</param>
        public Table addTable(string name)
        {
            var tb = new context.Table(this);
            tb.name = tb.key = tb.DBName = name;
            tables.Add(tb);
            return tb;
        }
        /// <summary>向 <see cref="ImportOption.KVs"/> 追加一列，仅设置 key。</summary>
        /// <param name="key">列配置唯一键。</param>
        public Column addKVColumn(string key)
        {
            var col = new Column(this);
            col.key = key;
            KVs.Add(col);
            return col;
        }
        /// <summary>创建仅含 key 的列对象（未自动加入列表，用于进一步链式配置）。</summary>
        /// <param name="key">列配置唯一键。</param>
        public Column addColumn(string key)
        {
            var col = new Column(this);
            col.key = key;
            return col;
        }
        /// <summary>
        /// 获取当前配置集合中的列
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Column getKVColumn(string key) {
            foreach (var col in KVs) { 
                if(col.key == key) return col;
            }
            return null;
        }


        /// <summary>
        /// 追加一张表并设置标题、主键列、查重条件与基础 WHERE。
        /// </summary>
        /// <param name="name">逻辑表名。</param>
        /// <param name="caption">显示标题。</param>
        /// <param name="keyCol">主键列名。</param>
        /// <param name="repeatWhere">查重 WHERE 模板。</param>
        /// <param name="baseWhere">核验查询附加条件。</param>
        public Table addTable(string name, string caption, string keyCol, string repeatWhere, string baseWhere)
        {
            var tb = new Table(this);
            tb.name = name;
            tb.caption = caption;
            tb.keyCol = keyCol;
            tb.repeatWhere = repeatWhere;
            tb.baseWhere = baseWhere;
            tables.Add(tb);
            return tb;
        }
    }
}
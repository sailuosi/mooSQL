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
        public Table addTable(string name)
        {
            var tb = new context.Table(this);
            tb.name = tb.key = tb.DBName = name;
            tables.Add(tb);
            return tb;
        }
        public Column addKVColumn(string key)
        {
            var col = new Column(this);
            col.key = key;
            KVs.Add(col);
            return col;
        }
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
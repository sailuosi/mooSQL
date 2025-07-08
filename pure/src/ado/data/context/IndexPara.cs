using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 按位置作为参数的SQL工作类
    /// </summary>
    public class IndexPara
    {
        /// <summary>
        /// 命名参数SQL
        /// </summary>
        public string namedSQL;
        /// <summary>
        /// 索引参数SQL
        /// </summary>
        public string indexedSQL;

        /// <summary>
        /// 命名从参数
        /// </summary>
        public Paras namedPara;
        /// <summary>
        /// 索引的参数
        /// </summary>
        public List<Parameter> indexedPara;

        public string targetParaHolder = "?";

        public IndexPara read(string sql, Paras paras) { 
            this.namedSQL = sql;
            this.namedPara = paras;
            return this;
        }

        public string toPureSQL() {
            var newsql = namedSQL;
            foreach (var pa in namedPara.value)
            {
                var key = pa.Value.key;
                newsql = newsql.Replace(key, pa.Value.val.ToString());
            }
            return newsql;
        }

        /// <summary>
        /// 转为java jdbc的顺序索引参数模式
        /// </summary>
        /// <returns></returns>
        public IndexPara toIndexed() {

            var map= new Dictionary<int, Parameter>();
            //
            foreach (var kv in namedPara.value) {
                var key = kv.Value.key;
                var startIndex = 0;
                var index=namedSQL.IndexOf(key,startIndex);
                while (index != -1) { 
                    map.Add(index,kv.Value);
                    startIndex = index + key.Length;
                    index = namedSQL.IndexOf(key,startIndex);
                }

            }
            
            //然后按照位置索引进行排序

            var ids= new List<int>();
            foreach(var kv in map) { ids.Add(kv.Key); }

            ids.Sort();

            var pslist= new List<Parameter>();

            foreach (var id in ids) {
                pslist.Add(map[id]);
            }
            //然后对SQL进行处理，把所有参数名改为?即可
            var newsql = namedSQL;
            foreach (var pa in pslist)
            {
                var key = pa.key;
                newsql = newsql.Replace(key, this.targetParaHolder);
            }

            this.indexedPara = pslist;
            this.indexedSQL = newsql;
            return this;
        }
    }
}

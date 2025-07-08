
using System.Collections.Generic;
using System.Text;
namespace mooSQL.data
{
    /// <summary>
    /// 本类功能已在 SQLBuilder集成
    /// </summary>
    public class MergeInto
    {
        public string tar;
        public string src;
        public string onPart;
        public List<string[]> updateKV= new List<string[]>();
        public List<string[]> insertKV= new List<string[]>();
        public bool autoSrcCol=true;
        public bool doInsert = true;
        public bool doUpdate = true;
        public MergeInto(string tarTable,string srcTable,string onPart)
        {
            this.tar = tarTable;
            this.src = srcTable;
            this.onPart = onPart;
            //this.autoSrcCol = !isAllSrcSQL;
        }

        /// <summary>
        /// 添加update的键值对
        /// </summary>
        /// <param name="tarField"></param>
        /// <param name="srcField"></param>
        public void addKVu(string tarField,string srcField)
        {
            this.updateKV.Add(new string[] { tarField, srcField });
        }
        /// <summary>
        /// 添加insert
        /// </summary>
        /// <param name="tarField"></param>
        /// <param name="srcField"></param>
        public void addKVi(string tarField, string srcField)
        {
            this.insertKV.Add(new string[] { tarField, srcField });
        }

        public void addKV(string tarField, string srcField)
        {
            var t = new string[] { tarField, srcField };
            this.insertKV.Add(t);
            this.updateKV.Add(t);
        }
        public string toSQL()
        {
            if (updateKV.Count == 0 && insertKV.Count == 0) return "";
            var res = new StringBuilder();
            res.AppendFormat("merge into {0} using {1} on {2}", tar, src, onPart);
            if(doUpdate && updateKV.Count > 0)
            {
                res.Append(" when matched then update set ");
                for (int i = 0; i < updateKV.Count; i++) {
                    res.AppendFormat("{0}={1}", updateKV[i][0], updateKV[i][1]);
                    if (i != updateKV.Count - 1)
                    {
                        res.Append(",");
                    }
                }
            }
            if (doInsert && insertKV.Count > 0)
            {
                res.Append(" when not matched then ");
                var fields = new List<string>();
                var values = new List<string>();
                foreach (string[] kv in  insertKV)
                {
                    fields.Add(kv[0]);
                    values.Add(kv[1]);
                }
                res.AppendFormat("insert({0}) values ({1})", string.Join(",", fields), string.Join(",", values));
            }
            res.Append(";");
            return res.ToString();
        }
    }
}

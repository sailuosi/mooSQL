
using System.Collections.Generic;
using System.Text;
namespace mooSQL.data
{
    /// <summary>
    /// ���๦������ SQLBuilder����
    /// </summary>
    public class MergeInto
    {
        /// <summary>
        /// 字段 tar（string）。
        /// </summary>
        public string tar;
        /// <summary>
        /// 字段 src（string）。
        /// </summary>
        public string src;
        /// <summary>
        /// 字段 onPart（string）。
        /// </summary>
        public string onPart;
        /// <summary>
        /// 字段 updateKV（List<string[]>）。
        /// </summary>
        public List<string[]> updateKV= new List<string[]>();
        /// <summary>
        /// 字段 insertKV（List<string[]>）。
        /// </summary>
        public List<string[]> insertKV= new List<string[]>();
        /// <summary>
        /// 字段 autoSrcCol（bool）。
        /// </summary>
        public bool autoSrcCol=true;
        /// <summary>
        /// 字段 doInsert（bool）。
        /// </summary>
        public bool doInsert = true;
        /// <summary>
        /// 字段 doUpdate（bool）。
        /// </summary>
        public bool doUpdate = true;
        /// <summary>
        /// 初始化 MergeInto（构造）。
        /// </summary>
        public MergeInto(string tarTable,string srcTable,string onPart)
        {
            this.tar = tarTable;
            this.src = srcTable;
            this.onPart = onPart;
            //this.autoSrcCol = !isAllSrcSQL;
        }

        /// <summary>
        /// ���update�ļ�ֵ��
        /// </summary>
        /// <param name="tarField"></param>
        /// <param name="srcField"></param>
        public void addKVu(string tarField,string srcField)
        {
            this.updateKV.Add(new string[] { tarField, srcField });
        }
        /// <summary>
        /// ���insert
        /// </summary>
        /// <param name="tarField"></param>
        /// <param name="srcField"></param>
        public void addKVi(string tarField, string srcField)
        {
            this.insertKV.Add(new string[] { tarField, srcField });
        }

        /// <summary>
        /// addKV 方法。
        /// </summary>
        public void addKV(string tarField, string srcField)
        {
            var t = new string[] { tarField, srcField };
            this.insertKV.Add(t);
            this.updateKV.Add(t);
        }
        /// <summary>
        /// toSQL 方法（返回 string）。
        /// </summary>
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
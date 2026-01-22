
using System;
using System.Collections.Generic;


namespace mooSQL.data
{
    /// <summary>
    /// SQL命令的所有参数
    /// </summary>
    public class Paras
    {
        /// <summary>
        /// 识别ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  Key : Property
        /// </summary>
        public IDictionary<String, Parameter> value = new Dictionary<String, Parameter>();
        /// <summary>
        /// 获取指定名称的参数
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public Parameter GetParameter(string columnName)
        {
            if (value.TryGetValue(columnName, out var parameter))
            {
                return parameter;
            }
            return null;
        }
        /// <summary>
        /// 已添加参数的数量
        /// </summary>
        public int Count{
            get {
                return value.Count;
            }
        }
        /// <summary>
        /// 清空参数，重置格式化计数器
        /// </summary>
        public void Clear()
        {

            this.value.Clear();
            this.fmtIndex = 0;
        }
        /// <summary>
        /// 添加一个SQL参数,这个参数的变量名，必须是增加了数据库变量方言的合法变量名。
        /// </summary>
        /// <param name="key">以数据库变量前缀开头的变量，比如在SQLServer下以 @开头</param>
        /// <param name="val"></param>
        public void Add(string key, Object val) {
            this.AddByPrefix(key, val, null);
        }
        /// <summary>
        /// 在知晓SQL变量标识符的情况下，添加参数。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="varPrefix"></param>
        public void AddByPrefix(string key, Object val, string varPrefix)
        {
            var tar = new Parameter(key, val, varPrefix);
            if (value.ContainsKey(key))
            {
                value[key] = tar;
            }
            else
            {

                value.Add(key, tar);
            }
        }
        /// <summary>
        /// 直接添加自己定义的参数
        /// </summary>
        /// <param name="ps"></param>
        public void Add(Parameter ps) {
            var key = ps.key;
            if (string.IsNullOrWhiteSpace(key)) { 
                key= Guid.NewGuid().ToString();
                key ="live_"+ key.Replace("-", "x");
                ps.key = key;
            }
            if (value.ContainsKey(key))
            {
                value[key] = ps;
            }
            else
            {

                value.Add(key, ps);
            }
        }
        /// <summary>
        /// 添加一个不含数据库变量前缀的参数。1参为参数名，2参为参数在SQL模版中的占位符，3参为参数值。
        /// </summary>
        /// <param name="rawKey"></param>
        /// <param name="placeholer"></param>
        /// <param name="val"></param>
        public void Add(string rawKey, string placeholer, Object val)
        {
            AddRaw(rawKey,placeholer,val);
        }
        /// <summary>
        /// 添加一个不含数据库变量前缀的参数。1参为参数名，2参为参数在SQL模版中的占位符，3参为参数值。
        /// </summary>
        /// <param name="rawKey"></param>
        /// <param name="placeholer"></param>
        /// <param name="val"></param>
        public void AddRaw(string rawKey, string placeholer,Object val)
        {
            var tar = new Parameter(true,rawKey,placeholer,val);
            if (value.ContainsKey(rawKey))
            {
                value[rawKey] = tar;
            }
            else
            {

                value.Add(rawKey, tar);
            }
        }
        /// <summary>
        /// 复制所有的参数到当前Paras对象中
        /// </summary>
        /// <param name="src"></param>
        public void Copy(Paras src) {
            if (src == null) return;
            foreach (var kv in src.value)
            {
                if (value.ContainsKey(kv.Key)) {
                    value[kv.Key] = kv.Value;
                }
                else { 
                    value.Add(kv.Key, kv.Value); 
                }
            }
        }

        private int fmtIndex = 0;
        private string paraPrefix = "psfmt_";
        /// <summary>
        /// 设置用于formatSQL方法中参数前缀
        /// </summary>
        /// <param name="prefix"></param>
        public void setParaPrefix(string prefix) {
            this.paraPrefix = prefix;
        }
        /// <summary>
        /// 格式化SQL语句，使用 string.Format的格式传入参数占位符 {0}...{1}...{2}...处理后参数会被加入到Paras对象中，并返回格式化后的SQL语句。
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public string formatSQL(string SQL, params object[] values)
        {
            fmtIndex++;
            string key = SQL;
            string prefixName = this.paraPrefix + fmtIndex+"_";

            for (int i = 0; i < values.Length; i++)
            {
                string reg = "{" + i + "}";
                var v = values[i];
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else
                {
                    string paraName = prefixName + this.Count + "_" + i;
                    var holderName = "#{" + paraName + "}";
                    key = key.Replace(reg, holderName);
                    this.AddRaw(paraName, holderName, v);
                }

            }

            return key;
        }

    }
}

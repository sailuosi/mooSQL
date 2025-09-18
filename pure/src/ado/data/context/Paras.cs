
using System;
using System.Collections.Generic;


namespace mooSQL.data
{
    /// <summary>
    /// SQL命令的所有参数
    /// </summary>
    public class Paras
    {
        public string Id { get; set; }

        /// <summary>
        ///  Key : Property
        /// </summary>
        public IDictionary<String, Parameter> value = new Dictionary<String, Parameter>();
        public Parameter GetParameter(string columnName)
        {
            if (value.TryGetValue(columnName, out var parameter))
            {
                return parameter;
            }
            return null;
        }

        public int Count{
            get {
                return value.Count;
            }
        }
        public void Clear()
        {

            this.value.Clear();
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


  


    }
}

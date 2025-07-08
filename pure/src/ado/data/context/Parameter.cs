
using mooSQL.data.context;
using mooSQL.data.model;
using System;
using System.Data;


namespace mooSQL.data
{
    /// <summary>
    /// SQL命令的参数。
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// 真正的数据合法变量名，一般带有前缀如@
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// 不含数据库变量修饰的变量名
        /// </summary>
        public string rawKey;
        /// <summary>
        /// 在SQL语句中占位的变量名
        /// </summary>
        public string rawHolder;
        /// <summary>
        /// 是否为粗糙模式，粗糙模式下用户不需要手动输入数据库变量前缀，由系统自动根据 占位符和rawkey 修正追加.
        /// </summary>
        public bool raw = false;

        public object val { get; set; }
        public ITypeHandler handler { get; set; }
        /// <summary>
        /// 数据库映射的类型名
        /// </summary>
        public DbDataType dbType { get; set; }

        public DataType dataType { get; set; }

        public ParameterDirection direction { get; set; }

        public int? size { get; set; }
        public Parameter() { }
        public Parameter(string key, Object val)
        {
            this.key = key;
            this.val = val;
            this.raw = false;
        }
        public Parameter(string rawkey, string holder, Object val)
        {
            this.rawKey = rawkey;
            this.rawHolder = holder;
            this.raw = true;
            this.val = val;
        }
        /// <summary>
        /// 返回是否 为合法的SQL变量
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public bool CheckRaw(string prefix)
        {
            if (raw)
            {
                //if (string.IsNullOrEmpty(rawHolder))
                //{
                //    //在没有设置占位的情况下，可能是用户直接的配置了含其它数据库前缀的key
                //    if (!string.IsNullOrEmpty(key))
                //    {
                //        rawHolder = key;
                //    }
                //    else
                //    {
                //        rawHolder = rawKey;
                //    }
                //}
                if (!string.IsNullOrEmpty(rawKey))
                {
                    key = prefix + rawKey;
                }
                return true;
            }
            if (!key.StartsWith(prefix)) {
                if (!string.IsNullOrEmpty(key))
                {
                    key = prefix + key;
                }
                else if(!string.IsNullOrEmpty(rawKey)){
                    //key为空
                    key = prefix + rawKey;
                }

            }
            return false;
        }
    }
}

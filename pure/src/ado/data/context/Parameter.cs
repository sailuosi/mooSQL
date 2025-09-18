
using mooSQL.data.context;
using mooSQL.data.model;
using System;
using System.Data;
using System.Text.RegularExpressions;


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
        public string rawKey { get; set; }
        /// <summary>
        /// 在SQL语句中占位的变量名
        /// </summary>
        public string rawHolder { get; set; }
        /// <summary>
        /// 在SQL语句中占位的变量名，代表SQL变量在SQL中的位置。
        /// </summary>
        public string holder { get; set; }
        /// <summary>
        /// 数据库变量前缀，如"@","$"等。默认为空字符串""。 若为null，则不添加任何前缀。 若不为null，则在key和rawKey前面加上该值。 若为空字符串"", 则在key和rawKey前面加上holder的前缀。
        /// </summary>
        public string varPrefix { get; set; }
        /// <summary>
        /// 是否为粗糙模式，粗糙模式下用户不需要手动输入数据库变量前缀，由系统自动根据 占位符和rawkey 修正追加.
        /// </summary>
        public bool raw = false;
        /// <summary>
        /// 参数值
        /// </summary>
        public object val { get; set; }
        /// <summary>
        /// 参数类型处理器，用以处理数据库和C#之间的数据转换。 若为null，则使用默认的类型处理器。
        /// </summary>
        public ITypeHandler handler { get; set; }
        /// <summary>
        /// 数据库映射的类型名
        /// </summary>
        public DbDataType dbType { get; set; }
        /// <summary>
        /// 数据类型，代表着数据库字段的类型
        /// </summary>
        public DataFam dataType { get; set; }
        /// <summary>
        /// 参数方向
        /// </summary>
        public ParameterDirection direction { get; set; }
        /// <summary>
        /// 最大值
        /// </summary>
        public int? size { get; set; }
        public Parameter() { }
        /// <summary>
        /// 在知晓数据库变量标识符的时候使用，不传则在执行时自动修正（SQL语句使用模版语法#{key}）。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="varPrefix"></param>
        public Parameter(string key, Object val,string varPrefix = null)
        {
            this.key = key;
            this.val = val;
            this.raw = false;
            this.varPrefix = varPrefix;
            this.holder = varPrefix+ key;
        }
        /// <summary>
        /// 不知晓数据库变量标识符的时候使用。依据占位符名称进行替换，比如rawkey为"id"，holder为"#{id}"，执行时替换SQL的#{id}为"@id"或"$id"。
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="rawkey"></param>
        /// <param name="holder"></param>
        /// <param name="val"></param>
        public Parameter(bool raw,string rawkey, string holder, Object val)
        {
            this.rawKey = rawkey;
            this.rawHolder = holder;
            this.raw = true;
            this.val = val;
            this.holder = holder;
        }
        /// <summary>
        /// 返回是否 进行了修改
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="SQL"></param>
        /// <param name="newSQL"></param>
        /// <returns></returns>
        public bool CheckRaw(string prefix,string SQL,out string newSQL)
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
                //该数据库中真正可用的变量名
                if (!string.IsNullOrEmpty(rawKey))
                {
                    key = prefix + rawKey;
                }
                newSQL= SQL.Replace(rawHolder, key);
                return true;
            }
            if (!key.StartsWith(prefix)) {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    //可能是一个SQL需要放到另外一个数据库中执行的情况，需要修正前缀
                    ////如果参数中没有提供前缀，按照约定，如果SQL使用模版语法#{id}，则在执行时修正
                    if (string.IsNullOrWhiteSpace(this.varPrefix))
                    {

                        var tmpHolder = string.Concat("#{", key, "}");
                        if (SQL.Contains(tmpHolder))
                        {
                            key = prefix + key;
                            newSQL = SQL.Replace(tmpHolder, key);
                            return true;
                        }
                    }
                    else if(varPrefix==prefix){ 

                        var prefixHolder = string.Concat(this.varPrefix, key);
                        if(SQL.Contains(prefixHolder))
                        {
                            key = prefix + key;
                            newSQL = SQL;
                            return false;
                        }
                    }
                    if (this.varPrefix == prefix)
                    {
                        //此时前缀未改，但如果SQL中没有，则修正
                    }
                    //设置了前缀信息
                    var newKey = key;
                    if (!string.IsNullOrWhiteSpace(this.varPrefix))
                    {
                        if (key.StartsWith(this.varPrefix))
                        {
                            key = key.Substring(this.varPrefix.Length);
                            newKey = prefix + key;
                        }
                    }
                    else if(Regex.IsMatch(key, @"^[\$\?@]")){
                        newKey = key.Replace(@"^[\$\?@]", prefix);
                    }
                    if (SQL.Contains(key)) { 
                        newSQL = SQL.Replace(key, newKey);
                        key = newKey;
                        return true;
                    }
                        //else 
                        //走到这里时：
                        key = prefix + key;

                }
                else if( !string.IsNullOrWhiteSpace(rawKey)){
                    //key为空
                    //key = prefix + rawKey;
                    //newSQL = SQL.Replace(tmpHolder, key);
                }

            }
            newSQL = SQL;
            return false;
        }
    }
}

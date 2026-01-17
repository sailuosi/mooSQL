
using mooSQL.data.model;
using System;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 一个准备执行的SQL命令，包含SQL文本，参数等。
    /// </summary>
    public class SQLCmd
    {
        /// <summary>
        /// 预执行的命令
        /// </summary>
        public SQLCmd() { 
            this.para= new Paras();
        }
        /// <summary>
        /// 创建预执行的命令
        /// </summary>
        /// <param name="sql"></param>
        public SQLCmd(string sql) {
            this.sql = sql;
            this.para = new Paras();
        }
        /// <summary>
        /// 创建预执行的命令
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        public SQLCmd(string sql,Paras paras) {
            this.sql=sql;
            if (para == null) {
                para = new Paras();
            }
            this.para.Copy(paras);
        }
        /// <summary>
        /// 创建预执行的SQL
        /// </summary>
        public string sql { get; set; }
        /// <summary>
        /// 参数体集合
        /// </summary>
        public Paras para { get; set; }
        /// <summary>
        /// 命令类型,默认 Text
        /// </summary>
        public CommandType? cmdType {  get; set; }
        /// <summary>
        /// SQL语句类型
        /// </summary>
        public QueryType type { get; set; }
        /// <summary>
        /// SQL语句的超时设置
        /// </summary>
        public int timeout { get; set; }
        /// <summary>
        /// 复制存在的参数到本实例中
        /// </summary>
        /// <param name="pa"></param>
        public void copy(Paras pa) { 
            para.Copy(pa);
        }
        /// <summary>
        /// SQL是否为空
        /// </summary>
        public bool Empty
        {
            get {
                return string.IsNullOrWhiteSpace(sql); 
            }
        }
        /// <summary>
        /// 传递
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public T giveTo<T>(Func<SQLCmd,T> func) {
            return func(this);
            //return tar;
        }
        /// <summary>
        /// 传递
        /// </summary>
        /// <param name="tar"></param>
        /// <returns></returns>
        public ISQLCmdTaker giveTo(ISQLCmdTaker tar)
        {
            tar.TakeOver(this);
            return tar;
        }
        /// <summary>
        /// 转换为原始SQL语句，不带参数占位符。
        /// </summary>
        /// <param name="paraPrefix"></param>
        /// <returns></returns>
        public string toRawSQL(string paraPrefix="") {

            var sql = this.sql;
            if (para == null) return sql;
            foreach (var item in para.value)
            {
                if (sql.Contains(item.Value.holder)) {
                    sql = sql.Replace(item.Value.holder, "'" + item.Value.val.ToString() + "'");
                }
                else
                {
                    sql = sql.Replace(paraPrefix + item.Key, "'" + item.Value.val.ToString() + "'");
                }
                    
            }
            return sql;
        }

    
    }
}

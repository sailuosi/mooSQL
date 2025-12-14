
using System.Data;
using System.Data.Common;


namespace mooSQL.data.context
{
    /// <summary>
    /// DBCommand 命令的执行环境 ，持有对执行过程进行参数调整的内容。代理着 ISQLCommand对象。
    /// </summary>
    public class CmdBuilder
    {

        public CommandType cmdType = CommandType.Text;
        /// <summary>
        /// SQL 的内容
        /// </summary>
        public string cmdText = "";
        /// <summary>
        /// 事务
        /// </summary>
        public DbTransaction transaction;
        /// <summary>
        /// 超时设置
        /// </summary>
        public int timeout = 300;
        /// <summary>
        /// SQL参数
        /// </summary>
        public Paras para { get; internal set; }

        public SQLCmd cmd {  get; set; }
        /// <summary>
        /// 配置SQL命令和参数
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        public void reset(SQLCmd cmd) {
            cmdText =cmd.sql;
            para =cmd.para;
            this.cmd = cmd;
            if (cmd.cmdType != null)
            {
                cmdType = cmd.cmdType.Value;
            }
            if (cmd.timeout > 0) {
                timeout = cmd.timeout;
            }
            
        }
        /// <summary>
        /// 执行参数修正，适配数据库
        /// </summary>
        /// <param name="prefix"></param>
        public void repairParas( string prefix)
        {

            if (this.para != null)
            {
                foreach (var kv in this.para.value)
                {
                    //把占位变量名，替换为数据库认可的注册变量名。
                    if (kv.Value.CheckRaw(prefix,this.cmdText,out var newSQL))
                    {
                        this.cmdText = newSQL;
                           // this.cmdText.Replace(kv.Value.rawHolder, kv.Value.key);
                    }
                }
            }
        }
        /// <summary>
        /// 输出SQL
        /// </summary>
        /// <param name="paraPrefix"></param>
        /// <returns></returns>
        public string toRawSQL(string paraPrefix = "")
        {

            var sql = this.cmdText;
            if (para == null) return sql;
            foreach (var item in para.value)
            {
                if (sql.Contains(item.Value.holder))
                {
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

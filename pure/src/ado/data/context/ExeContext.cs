


namespace mooSQL.data.context
{
    /// <summary>
    /// 执行上下文  ，持有SQL命令sqlCommand、执行器、数据库方言 。
    /// </summary>
    public class ExeContext {
        /// <summary>
        /// 创建执行上下文
        /// </summary>
        public ExeContext() { 
        
        
        }
        public CmdBuilder cmd = null;

        /// <summary>
        /// 数据库会话
        /// </summary>
        public ExeSession session;
        /// <summary>
        /// 数据库的方言 
        /// </summary>
        public Dialect dialect;
        /// <summary>
        /// 当前的数据库实例
        /// </summary>
        public DBInstance DBLive { get; set; }
    } 

}
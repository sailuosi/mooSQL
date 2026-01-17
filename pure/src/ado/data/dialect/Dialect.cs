




using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq;
using System;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库方言类：主要处理通用数据库 执行命令的获取。
    /// </summary>
    public abstract partial class Dialect //: IAdo, IDisposable
    {
        /// <summary>
        /// 数据库的版本演进信息
        /// </summary>
        protected List<DBVersion> Versions { get; set; }

        private DBVersion _CurVersion;

        /// <summary>
        /// 当前数据库版本信息。
        /// </summary>
        public DBVersion CurVersion {
            get {
                if (this._CurVersion == null) {
                    this._CurVersion= this.CheckVersion();
                }
                return this._CurVersion;
            }
        }
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance dbInstance;
        /// <summary>
        /// SQL语法方言的处理类。
        /// </summary>
        public SQLExpression expression;

        /// <summary>
        /// SQL语句创建
        /// </summary>
        public SQLSentence sentence;

        public SooSQLFunction function;
        public SooOption Option {  get; set; }
        #region 数据库的命令执行方言
        /// <summary>
        /// 数据库参数
        /// </summary>
        public DataBase db;
        /// <summary>
        /// 获取一个 DbCommand
        /// </summary>
        /// <returns></returns>
        public abstract DbCommand getCommand();

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <returns></returns>
        public abstract DbConnection getConnection();
        /// <summary>
        /// 获取数据读取类
        /// </summary>
        /// <returns></returns>
        public abstract DbDataAdapter getDataAdapter();
        /// <summary>
        /// 获取.net 命令创建器
        /// </summary>
        /// <returns></returns>
        public abstract DbCommandBuilder getCmdBuilder();

        /// <summary>
        /// 获取 DbBulkCopy
        /// </summary>
        /// <returns></returns>
        public abstract DbBulkCopy GetBulkCopy();
        /// <summary>
        /// 最大的参数数量
        /// </summary>
        public int paramMaxSize = 2500;
        /// <summary>
        /// 如果数据库的自身参数化有个性化的处理
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public virtual string addCmdPara(DbCommand cmd, Paras para) {
            string msg = string.Empty;
            if(para != null && para.value.Count>0)
            {
                foreach (var dbParam in para.value.Values)
                {
                    AddCmdPara(cmd, dbParam);
                }
            }


            return msg;
        }
        /// <summary>
        /// 为cmd命令添加参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public abstract DbParameter AddCmdPara(DbCommand cmd, Parameter para);

        /// <summary>
        /// 为cmd命令添加来源于 DataTable列的 参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameterName"></param>
        /// <param name="odbcType"></param>
        /// <param name="size"></param>
        /// <param name="sourceColumn"></param>
        /// <returns></returns>
        public abstract DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type odbcType, int size, string sourceColumn);


        #endregion

        /// <summary>
        /// SQL模型的解析器
        /// </summary>
        public ClauseTranslateVisitor clauseTranslator {  get; set; }
        /// <summary>
        /// 映射面板
        /// </summary>
        public MappingPanel mapping { get; set; }
    }
}
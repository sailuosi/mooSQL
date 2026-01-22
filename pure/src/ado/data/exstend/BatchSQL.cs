


using System.Data.Common;

using System.Collections.Generic;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// 批量SQL 工作类，内部通过SQLBuilder创建SQL  通过DBRunHelper 执行SQL
    /// </summary>
    public class BatchSQL
    {
        /// <summary>
        /// 传入包含了数据库配置的数据库实例
        /// </summary>
        /// <param name="DB"></param>
        public BatchSQL(DBInstance DB)
        {
            this.DBLive = DB;
            this.builder = new SQLBuilder(DB.expression);
            this.runner = new DBRunner(DB);
        }
        /// <summary>
        /// 根据SQL编辑器类创建，自动获取数据库实例、运行器。
        /// </summary>
        /// <param name="builder"></param>
        public BatchSQL(SQLBuilder builder)
        {
            this.DBLive = builder.DBLive;
            this.builder = builder;
            this.runner = new DBRunner(builder.DBLive);
        }
        /// <summary>
        /// 功能部件
        /// </summary>
        public SQLBuilder builder;

        public SQLBuilder Builder { get { return this.builder; } }  

        private DBRunner runner;
        [Obsolete("属性已废弃，请使用 DBLive 属性")]
        public DBInstance DBInstance
        {
            get {
                return this.DBLive;
            }
        }
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance DBLive { get; set; }
        // 方法局变
        [Obsolete("属性已废弃，用户不应访问和操作")]
        public Dictionary<int, SQLCmd> rows = new Dictionary<int, SQLCmd>();
        /// <summary>
        /// 因需要保留SQL的顺序执行性，改为使用List存储待执行的SQL命令
        /// </summary>
        private List<SQLCmd> toRunCmds= new List<SQLCmd>();
        /// <summary>
        /// 已添加的SQL行数量
        /// </summary>
        public int Count
        {
            get { 
                return toRunCmds.Count;
            }
        }
        /// <summary>
        /// 是否在 提交行SQL时自动根据 batchSize 运行SQL;
        /// </summary>
        public bool autoSubmit = false;
        /// <summary>
        /// 批量执行时的批次大小
        /// </summary>
        public int batchSize = 100;
        /// <summary>
        /// 为了安全执行update ，对SQL队列按照;切割后，分别执行。
        /// </summary>
        public bool safeSplit = true;
        /// <summary>
        /// 事务名称
        /// </summary>
        public string transName;
        public DbTransaction trans;
        /// <summary>
        /// 标记在常规的执行中是否使用事务
        /// </summary>
        private bool hasTrans = false;
        /// <summary>
        /// 是否将写入值参数化，默认true，
        /// </summary>
        public bool isParamed = true;
        /// <summary>
        /// 是否使用事务来执行批量处理
        /// </summary>
        public bool useTransaction = false;
        /// <summary>
        /// 是否异步
        /// </summary>
        public bool async = false;
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="transname"></param>
        /// <param name="position"></param>
        public void beginTransaction(string transname, int position)
        {
            //开启事务
            this.transName = transname;
            this.hasTrans = true;
        }
        /// <summary>
        /// 提交事务
        /// </summary>
        public void commit()
        {

        }
        /// <summary>
        /// 数据库执行器,用于处理事务的逻辑
        /// </summary>
        public DBExecutor Executor { get; private set; }
        private bool _printSQL = false;
        private Action<string> onSQLPrint;
        /// <summary>
        /// 打印执行的SQL
        /// </summary>
        /// <param name="onPrint"></param>
        /// <returns></returns>
        public BatchSQL print(Action<string> onPrint)
        {
            this._printSQL = true;
            this.onSQLPrint = onPrint;
            return this;
        }
        /// <summary>
        /// 注册事务
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public BatchSQL useTrans(DBExecutor executor)
        {
            this.Executor = executor;
            return this;
        }
        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public BatchSQL addParam(string key, Object value)
        {
            builder.addPara(key, value);
            return this;
        }
        /// <summary>
        /// 直接添加一段SQL，无参数
        /// </summary>
        /// <param name="SQL"></param>
        public BatchSQL addSQL(string SQL)
        {
            if (SQL == null)
            {
                return this;
            }
            SQLCmd cmd = new SQLCmd(SQL);
            this.toRunCmds.Add(cmd);
            return this;
        }
        /// <summary>
        /// 添加一个SQL命令 ，带参数
        /// </summary>
        /// <param name="SQL"></param>
        public BatchSQL addSQL(SQLCmd SQL)
        {
            if (SQL == null) {
                return this;
            }
            this.toRunCmds.Add( SQL);
            return this;
        }
        /// <summary>
        /// 添加一组SQL
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public BatchSQL addSQLs(List<SQLCmd> SQL)
        {
            if (SQL == null)
            {
                return this;
            }
            foreach (SQLCmd cmd in SQL) {
                this.toRunCmds.Add(cmd);
            }

            
            return this;
        }
        /// <summary>
        /// 清空SQL编辑器 并返回它。如果要保存它的SQL，请调用 addInsert 或 addUpdate
        /// </summary>
        /// <returns></returns>
        public SQLBuilder newRow() { 
            this.builder.clear();
            return this.builder;
        }

        /// <summary>
        /// 为cmd创建更新语句（等待执行）。(自动清空kv)
        /// </summary>
        public BatchSQL addUpdate()
        {
            var cmd= this.builder.toUpdate();
            if (!cmd.Empty) {
                this.toRunCmds.Add(cmd);
            }
            
            return this;
        }
        /// <summary>
        /// 为命令cmd创建插入语句。（等待执行）(自动清空kv)
        /// </summary>
        public BatchSQL addInsert()
        {
            var cmd=  this.builder.toInsert();
            this.toRunCmds.Add(cmd);
            return this;
        }
        /// <summary>
        /// 执行操作，执行所有积累的语句。（通过addInsert/addUpdate积累）(使用defaultPostion连接位)
        /// </summary>
        /// <returns></returns>
        public int exeNonQuery()
        {
            return exeCmd();
        }

        /// <summary>
        /// 异步执行查询
        /// </summary>
        /// <returns></returns>
        public int exeNonQueryAsync()
        {
            bool old = this.async;
            this.async = true;
            var res = exeCmd();
            this.async = old;
            return res;
        }
        private int exeCmd()
        {
            if (toRunCmds == null || toRunCmds.Count == 0) return 0;
            var res = 0;
            if (this.hasTrans|| this.useTransaction)
            {
                //在启用事务的情况下，数据库连接和事务都只初始化一次
                if (this.Executor == null)
                {
                    runner.useTransation(tranMode.Once);
                }
            }

            foreach (var cmd in toRunCmds) {

                if (this._printSQL && this.onSQLPrint !=null)
                {
                    onSQLPrint(cmd.toRawSQL(this.DBLive.expression.paraPrefix));
                }
            }

            res= runner.exeNonQuery(toRunCmds);
            toRunCmds.Clear();
            return res;
        }


        /// <summary>
        /// 执行更新
        /// </summary>
        /// <param name="tableKey"></param>
        /// <param name="fromPart"></param>
        /// <returns></returns>
        public int update(string tableKey, string fromPart)
        {
            var res = 0;
            var cmd = this.builder.toUpdate();
            runner.addSQL(cmd.sql)
                .addParas(cmd.para)
                .exeNonQuery();
            runner.clear();

            return res;
        }
        /// <summary>
        /// 清空
        /// </summary>
        public void clear()
        {
            this.toRunCmds.Clear();
            //this.para.Clear();
            this.builder.clear();
            this.runner.clear();
            this._printSQL = false;
            this.onSQLPrint = null;
        }



    }


}

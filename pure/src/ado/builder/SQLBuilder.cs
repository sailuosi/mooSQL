
using mooSQL.data.builder;
using mooSQL.utils;

using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;


namespace mooSQL.data
{
    /// <summary>
    /// SQL 语句的创建器。由于数据库方言的问题，需要注入 数据库方言的表达式类 
    /// </summary>
    public partial class SQLBuilder:IDisposable
    {
        /*****注入成员**/
        
       /// <summary>
       /// 释放资源，由于集成了事务功能，当使用事务时，需要释放资源。
       /// </summary>
        public void Dispose()
        {
            if(this.Executor != null)
            {
                this.Executor.Dispose();
            }
        }


        /// <summary>
        /// 数据库核心运行实例
        /// </summary>
        public DBInstance DBLive { get; private set; }
        /// <summary>
        /// 核心运行实例 MooClient
        /// </summary>
        public MooClient MooClient
        {
            get {
                if (this.DBLive != null)
                {
                    return DBLive.client;
                }
                return null;
            }
        }
        /// <summary>
        /// 客户端核心实例
        /// </summary>
        public MooClient Client
        {
            get
            {
                if (this.DBLive != null)
                {
                    return DBLive.client;
                }
                return null;
            }
        }
        /// <summary>
        /// 数据库方言处理类。
        /// </summary>
        public Dialect Dialect
        {
            get {
                return DBLive.dialect;
            }
        }
        /// <summary>
        /// 数据库执行器,用于处理事务的逻辑
        /// </summary>
        public DBExecutor Executor { get; private set; }

        /// <summary>
        /// 数据库方言表达式 
        /// </summary>
        public SQLExpression expression;
        /// <summary>
        /// 默认 -1 此时为禁用状态。禁用状态下必须传入数据库实例 DbInstance
        /// </summary>
        public int position = -1;

        internal ISooCache cache;

        internal int defaultCacheTimeout = 3000;


        private int _whereCount = 0;

        private SqlCTE CTECollection;

        private CleanWay _AutoClearWay { get; set; }
        /// <summary>
        /// 配置自动清理方式，默认为每次执行修改或删除后清理
        /// </summary>
        /// <param name="way"></param>
        /// <returns></returns>
        public SQLBuilder configClear(CleanWay way) { 
            this._AutoClearWay = way;
            return this;
        }
        /// <summary>
        /// 设置数据库连接位
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public SQLBuilder setPosition(int position)
        {
            this.position = position;
            return this;
        }
        /// <summary>
        /// 当前的set配置下的字段数
        /// </summary>
        public int ColumnCount
        {
            get { 
                return current.columns.Count;
            }
        }
        /// <summary>
        /// 检查是否已set了字段，通过字段名判断是否存在。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool containSetColumn(string name)
        {
            return current.getFieldByKey(name)!=-1;
        }

        /// <summary>
        /// 当前的from计数
        /// </summary>
        public int FromCount
        {
            get
            {
                return current.fromPart.Count;
            }
        }

        private bool _printSQL = false;
        private Action<string> onSQLPrint;
        /// <summary>
        /// 打印执行的SQL
        /// </summary>
        /// <param name="onPrint"></param>
        /// <returns></returns>
        public SQLBuilder print(Action<string> onPrint)
        {
            this._printSQL = true;
            this.onSQLPrint = onPrint;
            return this;
        }
        /// <summary>
        /// 设置缓存实例
        /// </summary>
        /// <param name="cacher"></param>
        /// <returns></returns>
        public SQLBuilder setCacheHolder(ISooCache cacher)
        {
            this.cache = cacher;
            return this;
        }

        /// <summary>
        /// 设置数据库实例，此时优先级高于position,将不会再通过position获取
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public SQLBuilder setDBInstance(DBInstance db)
        {
            this.DBLive = db;
            this.expression = db.expression;
            return this;
        }

        /** 功能成员 **/



        internal Dictionary<String, SqlGoup> groups = new Dictionary<String, SqlGoup>();



        internal SqlGoup current;


        internal string cacheKey = "";

        internal int cacheTimeout = 300;

        /**
         * 分组模式下的最终执行器。
         */
        internal UnionCollection unionHolder;
        /**
         * 上一个信息组。
         */
        public SqlGoup preSQL;
        /// <summary>
        /// 参数化前缀种子，传入后将作为所有参数名的前缀。
        /// </summary>
        private string seed = string.Empty;
        /// <summary>
        /// 参数化前缀种子，传入后将作为所有参数名的前缀。
        /// </summary>
        public string paraSeed
        {
            get
            {
                return seed;
            }
        }
        /// <summary>
        /// 层深，递归调用时增长
        /// </summary>
        public int level = 0;
        /// <summary>
        /// 当前操作的名称，默认为空字符串。
        /// </summary>
        public string name = "";
        /// <summary>
        /// 参数存储体
        /// </summary>
        public Paras ps = new Paras();
        /// <summary>
        /// 当执行buildwhere后，缓存结果到这里，以便后续副作用使用。
        /// </summary>
        public string preWhere = "";

        
        /// <summary>
        /// 可选 notEmpty all notNull 默认 notEmpty
        /// </summary>
         
        public string paraRule = "notEmpty";//

        private bool opened = true;

        /// <summary>
        /// 命名的SQL构建器，便于调试和追踪。
        /// </summary>
        /// <param name="name"></param>
        public SQLBuilder(string name)
        {
            this.name = name;
            init();
        }
        /// <summary>
        /// SQL构建器。
        /// </summary>
        public SQLBuilder()
        {
            this.name = "";
            init();
        }
        /// <summary>
        /// SQL构建器，延迟初始化。
        /// </summary>
        /// <param name="lazyInit"></param>
        public SQLBuilder(bool lazyInit)
        {
            this.name = "";
            if (!lazyInit) {
                init();
            }
            
        }
        /// <summary>
        /// SQL构建器，传入表达式实例。
        /// </summary>
        /// <param name="expression"></param>
        public SQLBuilder(SQLExpression expression)
        {
            this.name = "";
            this.expression = expression;
            init();
        }

        private void init() {
            this.CTECollection = new SqlCTE();
            this.unionHolder = new UnionCollection();
            this._AutoClearWay = CleanWay.AfterModify;
            this.newGroup();
        }



        /// <summary>
        /// 开启事务，此后的所有的操作在commit前都会在一个事务中
        /// </summary>
        /// <returns></returns>
        public SQLBuilder beginTransaction() { 
            this.Executor= new  DBExecutor(this.DBLive);
            Executor.beginTransaction();
            return this;
        }
        /// <summary>
        /// 使用一个已开启的事务执行器，此后的所有操作都在同一个事务中。
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public SQLBuilder useTransaction(DBExecutor executor)
        {
            this.Executor = executor;
            return this;
        }
        /// <summary>
        /// 提交事务，如果autoRollBack为true则在执行出错时自动回滚
        /// </summary>
        /// <param name="autoRollBack"></param>
        /// <exception cref="Exception"></exception>
        public void commit(bool autoRollBack = true) {
            if (this.Executor == null) { 
                throw new Exception("事务未开启");
            }
            Executor.commit(autoRollBack);
            this.Executor = null;
        }

        /// <summary>
        /// 清空所有配置信息到默认，相当于重新new SQLBuilder
        /// </summary>
        /// <returns></returns>

        public SQLBuilder reset()
        {
            this.groups.Clear();
            this.unionHolder.Clear();
            this.CTECollection.Clear();
            this.ps.Clear();
            this.preWhere = "";
            this._AutoClearWay =  CleanWay.AfterModify;
            paraRule = "notEmpty";
            opened = true;
            this.name = "kitTb_0";
            this.newGroup();
            return this;
        }

        internal SQLBuilder newGroup()
        {
            string tbname = this.name;
            var gpkey = string.Format("gp_{0}_{1}_{2}", seed, tbname, (groups.Count + 1));

            this.newGroup(gpkey, this.groups.Count + "");
            return this;
        }
        /// <summary>
        /// 创建一个SQL信息组，并置为当前组
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
   
        private SQLBuilder newGroup(string name, string key)
        {
            SqlGoup goup = new SqlGoup(name, key, this);
            goup.position = this.position;
            goup.ps = this.ps;
            
            groups.Add(name, goup);
            this.current = goup;
            return this;
        }

        /** 一些工具方法  **/


        private string replace(string src, string regx, string tar)
        {
            //src.replaceAll("(.*)google(.*)", "runoob" )
            //return Pattern.compile(regx, Pattern.CASE_INSENSITIVE).matcher(src).replaceAll(tar);
            return Regex.Replace(src, regx, tar);
        }
        /// <summary>
        /// SQL注入过滤，防止SQL注入攻击。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="onlyWrite"></param>
        /// <returns></returns>
        public string SqlFilter(string source, bool onlyWrite)
        {
            //sql注入过滤
            if (!onlyWrite)
            {
                //半角括号替换为全角括号
                source = replace(source, "[']", "'''");
                source = replace(source, "(select|from)", "");
            }

            //去除执行SQL语句的命令关键字
            source = replace(source, "(insert|update|delete|drop|truncate|declare|xp_cmdshell|exec|execute)", "");
            source = replace(source, "/add", "");
            source = replace(source, "net user", "");
            //去除系统存储过程或扩展存储过程关键字
            source = replace(source, "xp_", "x p_");
            source = replace(source, "sp_", "s p_");
            //防止16进制注入
            source = replace(source, "0x", "0 x");
            return source;
        }
        //public string paraRule = "notEmpty";//


        /// <summary>
        /// 返回已经包装的命名参数名，可以直接拼接再SQL中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>

        public string addPara(string key, Object val)
        {
            return this.current.addPara(key, val);
        }
        /// <summary>
        /// 添加列表参数，返回一个命名参数列表。可以直接拼接再SQL中
        /// </summary>
        /// <param name="list"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public List<string> addListPara(IEnumerable<object> list, string prefix)
        {
            return this.current.addListPara(list, prefix);
        }
        /// <summary>
        /// 设置缓存键值，用于缓存查询结果。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public SQLBuilder setCache(string key,int timeout)
        {
            this.cacheKey = key;
            this.cacheTimeout=timeout;
            return this;
        }

        /// <summary>
        /// 设置一个SQL参数前缀。
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public SQLBuilder setSeed(string seed) { 
            this.seed=seed;
            return this;
        }
        /// <summary>
        /// 检查一次条件，使得后续的一次 set/where/whereLike/whereFormat方法得以执行
        /// </summary>
        /// <param name="isPass"></param>
        /// <returns></returns>

        public SQLBuilder ifs(bool isPass)
        {
            this.opened = isPass;
            return this;
        }
        /// <summary>
        /// 自定义条件
        /// </summary>
        /// <param name="isPass"></param>
        /// <param name="whenTrue"></param>
        /// <param name="whenFalse"></param>
        /// <returns></returns>
        public SQLBuilder ifs(bool isPass,Action whenTrue, Action whenFalse)
        {
            if (isPass)
            {
                whenTrue();
            }
            else { 
                whenFalse();
            }
            return this;
        }


        /// <summary>
        /// 清空当前SQL构造器 参数体、添加列集合、选择列、from部分、翻页设置、where条件等所有信息，相当于重新获取一个SQL分组实例。
        /// 未清空的：seed,level,
        /// </summary>
        /// <returns></returns>

        public SQLBuilder clear()
        {
            this.unionHolder.Clear();
            this.CTECollection.Clear();
            this.cacheKey = string.Empty;
            this.cacheTimeout = this.defaultCacheTimeout;

            current.clear();
            if (this.groups.Count > 1) {
                this.groups.Clear();
                this.groups.Add(current.key,current);
            }
            return this;
        }

        /// <summary>
        /// 清空 where条件构造器的所有成果。
        /// </summary>
        /// <returns></returns>

        public SQLBuilder clearWhere()
        {
            this.current.clearWhere();
            return this;
        }

        /// <summary>
        /// 重置翻页信息为默认的不翻页。
        /// </summary>
        /// <returns></returns>
  
        public SQLBuilder clearPage()
        {
            this.current.clearPage();
            return this;
        }


        /// <summary>
        /// 构建where条件部分，并放入到 preWhere中，然后返回条件信息。
        /// </summary>
        /// <returns></returns>

        public string buildWhere()
        {
            string conditon = current.buildWhere();
            this.preWhere = conditon;
            return conditon;
        }
        /// <summary>
        /// 获取当前的构造器的where条件。
        /// </summary>
        /// <returns></returns>
        public string buildWhereContent()
        {
            string conditon = current.buildWhereContent();
            return conditon;
        }

        /***一组查询功能套件***/






        /// <summary>
        /// 自定义SQL的前置SQL
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SQLBuilder prefix(string SQLString)
        {
            current.prefix(SQLString);
            return this;
        }
        /// <summary>
        /// 配置SQL的自定义尾随部分
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SQLBuilder subfix(string SQLString)
        {
            current.subfix(SQLString);
            return this;
        }






        /// <summary>
        /// 复制上一组SQL配置的 select 部分
        /// </summary>
        /// <returns></returns>

        public SQLBuilder copyPreSelect()
        {
            current.copySelect(preSQL);
            return this;
        }
        /// <summary>
        /// 复制上一组SQL配置的from
        /// </summary>
        /// <returns></returns>
        public SQLBuilder copyPreFrom()
        {
            current.copyFrom(preSQL);
            return this;
        }
        /// <summary>
        /// 复制上一组SQL配置的where
        /// </summary>
        /// <returns></returns>
        public SQLBuilder copyPreWere()
        {
            current.copyWhere(preSQL);
            return this;
        }



        private string buildCountSQL()
        {
            string res = current.buildCountSQL();
            return res;
        }
        /// <summary>
        /// 多行插入时的行索引
        /// </summary>
        public int InsertRowIndex
        {
            get {
                return current.RowIndex;
            }
        }

        /// <summary>
        /// 当未配置时，直接返回空字符串
        /// </summary>
        /// <returns></returns>
    
        private string buildOrderBy()
        {
            return current.buildOrderBy();
        }

        #region 秋天的收获 ---SQL的最终生成，以 to开头的一组方法
        /// <summary>
        /// 创建 select 语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toSelect()
        {
            string sql = "";
            if (this.unionHolder.Count == 0)
            {
                sql = current.buildSelect();
            }
            else
            {
                sql = unionHolder.build();
            }

            if (!CTECollection.Empty) {
                //创建CTE表达式
                var cte = Dialect.expression.buildCET(CTECollection);
                if (!string.IsNullOrWhiteSpace(cte)) { 
                    sql = cte+" "+sql;
                }
            }

            return new SQLCmd(sql,ps);
        }



        /// <summary>
        ///  创建 select count(*) from ... 语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toSelectCount()
        {
            if (this.unionHolder.Count == 0)
            {
                string cksql = this.buildCountSQL();
                return new SQLCmd(cksql, ps);
            }
            else
            {
                string sql = unionHolder.buildCount();
                return new SQLCmd(sql, ps);
            }
        }
        /// <summary>
        /// 创建包含参数信息的 插入语句。
        /// </summary>
        /// <returns></returns>
        public SQLCmd toInsert() {
            string sql = current.buildInsert();
            return new SQLCmd(sql, ps);
        }
        /// <summary>
        /// 创建 insert from语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toInsertFrom()
        {
            var sql =  current.buildInsertFrom();
            return new SQLCmd(sql, ps); 
        }
        /// <summary>
        /// 创建update 语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toUpdate()
        {
            var sql=string.Empty;

            sql = current.buildUpdate();
                
            
            return new SQLCmd(sql, ps);

        }
        /// <summary>
        /// 创建update from 语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toUpdateFrom()
        {
            if (current.wherePart.Count == 0)
            {
                return new SQLCmd();
            }

            string sql = current.buildUpdateFrom();
            return new SQLCmd(sql, ps);
        }
        /// <summary>
        /// 创建 delete from 语句
        /// </summary>
        /// <returns></returns>

        public SQLCmd toDelete()
        {
            string sql = current.buildDelete();
            return new SQLCmd(sql, ps);
        }
        /// <summary>
        /// 创建 merge into语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toMergeInto()
        {
            string sql = current.buildMerge();
            return new SQLCmd(sql, ps);
        }

        #endregion


 

        #region 独立SQL生成方法，与上下文无关的
        /// <summary>
        /// 获取 select * from table where 1=2 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string getEmptySelect(string tableName) {
            return string.Format("select * from {0} where 1=2 ", tableName);
        }


        #endregion
    }
}




using mooSQL.data.context;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;



namespace mooSQL.data
{
    /// <summary>
    /// SQL创建工具类，子类必须重载根据连接位获取数据库的方法
    /// </summary>
    public partial class SQLBuilder
    {

        /// <summary>
        /// 获取数据库实例的委托
        /// </summary>
        public Func<int, DBInstance> loadDBInstance;
        /// <summary>
        /// 获取数据库实例，由初始化工厂执行调用，本身并不使用。
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual DBInstance getDB(int position)
        {
            if (this.loadDBInstance == null)
            {
                throw new Exception("未定义数据获取方式");
            }
            return this.loadDBInstance(position);
        }

        /// <summary>
        /// 执行一次修改的SQL语句
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public int exeNonQuery(string SQL, Paras? para= null)
        {
            if(para==null) para= new Paras();
            CheckDB();
            var cmd = new SQLCmd(SQL, para);
            return exeNonQuery(cmd);
        }

        private void CheckDB()
        {
            if (this.DBLive == null && position > -1)
            {
                this.DBLive = this.getDB(position);
            }
            if (this.DBLive == null)
            {
                throw new Exception("数据库实例未找到！");
            }
        }
        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int exeNonQuery(SQLCmd sql)
        {
            if (string.IsNullOrWhiteSpace(sql.sql)) return 0;
            if (this._printSQL)
            {
                printSQL(sql.sql, sql.para);
            }
            return DBLive.ExeNonQuery(sql, Executor);
        }
        /// <summary>
        /// 批量执行
        /// </summary>
        /// <param name="cmds"></param>
        /// <returns></returns>
        public int exeNonQuery(IEnumerable<SQLCmd> cmds)
        {
            if (cmds==null ||cmds.Count()==0) return 0;
            if (this._printSQL)
            {
                foreach (SQLCmd cmd in cmds) {
                    printSQL(cmd.sql, cmd.para);
                }
                
            }
            return DBLive.ExeNonQuery(cmds, Executor);
        }
        /// <summary>
        /// 执行一次select查询语句
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataTable exeQuery(string SQL, Paras? para=null)
        {
            if (para == null) para = new Paras();
            if (this._printSQL)
            {
                printSQL(SQL, para);
            }
            var cmd= new SQLCmd(SQL,para);
            return DBLive.ExeQuery(cmd, Executor);
        }

        public DataTable exeQuery(SQLCmd sql)
        {
            if (sql.para == null) sql.para = new Paras();
            if (this._printSQL)
            {
                printSQL(sql.sql, sql.para);
            };
            return DBLive.ExeQuery(sql, Executor);
        }
        /// <summary>
        /// 执行一次select查询语句，返回泛型集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public IEnumerable<T> exeQuery<T>(string SQL, Paras? para=null)
        {
            if (para == null) para = new Paras();
            if (this._printSQL)
            {
                printSQL(SQL, para);
            }
            var cmd = new SQLCmd(SQL, para);
            return DBLive.ExeQuery<T>(cmd, Executor);
        }
        /// <summary>
        /// 执行一次select查询语句，返回泛型集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public IEnumerable<T> exeQuery<T>(SQLCmd SQL)
        {
            if (SQL.para == null) SQL.para = new Paras();
            if (this._printSQL)
            {
                printSQL(SQL.sql, SQL.para);
            }

            return DBLive.ExeQuery<T>(SQL, Executor);
        }
        private void printSQL(string SQL, Paras para)
        {
            if (this._printSQL)
            {

                if (para != null && onSQLPrint != null)
                {
                    var sql = paraReplaceInto(SQL, para);
                    this.onSQLPrint(sql);
                }

            }
        }

        /// <summary>
        /// 将参数放入SQL
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public string paraReplaceInto(string SQL, Paras para)
        {
            var sql = SQL;
            foreach (var item in para.value)
            {
                sql = sql.Replace(Dialect.expression.paraPrefix + item.Key, "'" + item.Value.val.ToString() + "'");
            }
            return sql;
        }

        /// <summary>
        /// 查询第一列第一个值。没查到时返回-1
        /// </summary>
        /// <returns></returns>
        public int exeQueryCount(SQLCmd sqlCmd)
        {
            var dt = exeQuery(sqlCmd);
            if (dt.Rows.Count > 0)
            {
                var val = dt.Rows[0][0];
                return Convert.ToInt32(val);
            }
            return -1;
        }
        /// <summary>
        /// 根据上下文创建插入语句，可以是单行插入、多行插入、select from等
        /// </summary>
        /// <returns></returns>
        public int doInsert()
        {

            int res = exeNonQuery(toInsert());
            if (_AutoClearWay== CleanWay.AfterModify||_AutoClearWay== CleanWay.Always) clear();
            return res;
        }

        /// <summary>
        /// 注意！为防止误操作，where条件项不得为空。
        /// </summary>
        /// <returns></returns>

        public int doInsertFrom()
        {
            var sql = this.toInsertFrom();
            if (sql.Empty) return 0;

            int res = exeNonQuery(sql);
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always) clear();
            return res;
        }


        /// <summary>
        /// 执行更新语句，默认会自动clear 条件不得为空，如强制更新所有，可以设置1=1
        /// </summary>
        /// <returns></returns>

        public int doUpdate()
        {
            int res = exeNonQuery(this.toUpdate());
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always) clear();
            return res;
        }

        /// <summary>
        /// 根据 tablename/from/where/set 等部分的设置，创建update from 语句
        /// </summary>
        /// <returns></returns>

        public int doUpdateFrom()
        {
            var sql = this.toUpdateFrom();
            if (sql.Empty)
            {
                return -1;
            }

            int res = exeNonQuery(sql);
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always) clear();
            return res;
        }
        /// <summary>
        /// 创建 merge into 语句并立即执行，执行后清理配置
        /// </summary>
        /// <returns></returns>
        public int doMergeInto()
        {
            int res = exeNonQuery(this.toMergeInto());
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always) clear();
            return res;
        }
        /// <summary>
        /// 执行 delete SQL 默认完成后自动clear
        /// </summary>
        /// <returns></returns>

        public int doDelete()
        {
            if (current.wherePart.Count == 0)
            {
                return -1;
            }

            var sql = this.toDelete();

            int res = exeNonQuery(sql);
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always) clear();
            return res;
        }

        //    //删除之前先查询
        //    public int doDeleteBefore(){
        //        if(current.conditionsCount==0){
        //            return -1;
        //        }
        //
        //       string sql=current.buildSelect();
        //        ps.SQL =sql;
        //        int res= DBAccess.RunSQLReturnValInt(ps);
        //        if(autoClear) clear();
        //        return res;
        //    }

        private ISooCache cacheHolder
        {
            get
            {
                if (cache == null)
                {
                    if (Client.Cache != null)
                    {
                        cache = Client.Cache;
                    }
                    else
                    {
                        cache = CacheFacory.getHashCache();
                    }

                }
                return cache;
            }
        }
        /// <summary>
        /// 根据上下文配置获取查询结果。
        /// </summary>
        /// <returns></returns>
        public DataTable query()
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                DataTable dt = cacheHolder.Get<DataTable>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return null;
            }
            var res= exeQuery(sql);
            if (this._AutoClearWay == CleanWay.Always) { 
                clear();
            }
            return res;
        }
        /// <summary>
        /// 分页查询，返回分页数据和总数。
        /// </summary>
        /// <returns></returns>
        public PagedDataTable queryPaged() {
            var oldMode = this._AutoClearWay;
            this._AutoClearWay = CleanWay.Never;
            var dt = this.query();
            var total = this.count();
            var res = new PagedDataTable()
            {
                Items = dt,
                Total = total,
                PageNum = this.current.pageNum,
                PageSize = current.pageSize
            };
            this._AutoClearWay = oldMode;
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return res;
        }
        /// <summary>
        /// 泛型法，分页查询，返回分页数据和总数。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public PageOutput<T> queryPaged<T>()
        {
            var oldMode = this._AutoClearWay;
            this._AutoClearWay = CleanWay.Never;
            var dt = this.query<T>();
            var total = this.count();
            var res = new PageOutput<T>()
            {
                Items = dt,
                Total = total,
                PageNum = this.current.pageNum,
                PageSize = current.pageSize
            };
            this._AutoClearWay = oldMode;
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return res;
        }

        /// <summary>
        /// 泛型法，查询多行数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> query<T>()
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                IEnumerable<T> dt = cacheHolder.Get<IEnumerable<T>>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return null;
            }

            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return exeQuery<T>(sql);
        }
        /// <summary>
        /// 查询首列的数据，并转换为某个类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> queryFirstField<T>()
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                IEnumerable<T> dt = cacheHolder.Get<IEnumerable<T>>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return null;
            }
            var res= DBLive.ExeQueryFirstField<T>(sql,Executor);
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return res;
        }
        /// <summary>
        /// 查询单行数据，只会读取第一行，忽略后续数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T queryFirst<T>()
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                var dt = cacheHolder.Get<T>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return default;
            }
            if (this._printSQL)
            {
                printSQL(sql.sql, sql.para);
            }
            var res= DBLive.ExeQueryRow<T>(sql,Executor);
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return res;
        }


        /// <summary>
        /// 查询单行数据，查询唯一的一行数据，多行或没有都是null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T queryUnique<T>()
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                var dt = cacheHolder.Get<T>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return default;
            }
            if (this._printSQL)
            {
                printSQL(sql.sql, sql.para);
            }
            var res= DBLive.ExeQueryUniqueRow<T>(sql,Executor);
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return res;

        }


        /// <summary>
        /// 查询一个数据，只读第一行第一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T queryScalar<T>()
        {
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                var dt = cacheHolder.Get<T>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return default;
            }
            if (this._printSQL)
            {
                printSQL(sql.sql, sql.para);
            }
            var tar= DBLive.ExeQueryScalar<T>(sql, Executor);
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return tar;
        }

        /// <summary>
        /// 增加泛型法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)
        {

            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                //尝试获取
                TResult dt = cacheHolder.Get<TResult>(cacheKey);
                if (dt != null)
                {
                    return dt;
                }
            }

            var sql = this.toSelect();

            if (string.IsNullOrEmpty(sql.sql))
            {
                return default(TResult);
            }
            var tar= DBLive.ExecuteCmd(sql, (cmd, cont) =>
            {
                return onRuning(cont, typeof(T));
            });

            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return tar;
        }


        /// <summary>
        /// 依据自定义的行读取规则，来创建目标类的list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createEntity"></param>
        /// <returns></returns>
        public List<T> query<T>(Func<DataRow, T> createEntity)
        {
            var dt = query();
            if (dt == null)
            {
                return new List<T>();
            }
            var t = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                var v = createEntity(row);
                if (v != null)
                {
                    t.Add(v);
                }
            }
            return t;
        }
        /// <summary>
        /// 查询结果为唯一一行记录的结果，非1行结果返回null
        /// </summary>
        /// <returns></returns>

        public DataRow queryRow()
        {
            DataTable dt = this.query();
            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0];
            }
            return null;
        }


        /// <summary>
        /// 依据自定义的行读取规则，来创建目标类的list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public T queryRow<T>(Func<DataRow, T> builder)
        {
            DataTable dt = this.query();
            if (dt.Rows.Count == 1)
            {
                return builder(dt.Rows[0]);
            }
            return default(T);
        }

        /// <summary>
        /// 获取第一行一列的int值结果，查询结果必须为1行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public int queryRowInt(int defaultVal)
        {
            var row = queryRow();
            if (row == null)
            {
                return defaultVal;
            }
            return TypeAs.asInt(row[0], defaultVal);
        }
        /// <summary>
        /// 返回字符串值
        /// </summary>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public string queryRowString(string defaultVal)
        {
            var row = queryRow();
            if (row == null)
            {
                return defaultVal;
            }
            return TypeAs.asString(row[0], defaultVal);
        }
        /// <summary>
        /// 获取第一行一列的double值结果，查询结果必须为1行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public double queryRowDouble(double defaultVal)
        {
            var row = queryRow();
            if (row == null)
            {
                return defaultVal;
            }
            return TypeAs.asDouble(row[0], defaultVal);
        }

        /// <summary>
        /// 查询结果为唯一一行记录第一列的结果，非1行结果返回null
        /// </summary>
        /// <returns></returns>

        public Object queryRowValue()
        {
            DataTable dt = this.query();
            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0][0];
            }
            return null;
        }
        /// <summary>
        /// 返回查询结果的计数，使用 select count(*) 执行
        /// </summary>
        /// <returns></returns>
        public int count()
        {
            var cmd = toSelectCount();
            var tar= exeQueryCount(cmd);
            if (this._AutoClearWay == CleanWay.Always)
            {
                clear();
            }
            return tar;
        }




        public DataTable exeQuery(string orderByPart, string readsql, int pageSize, int pageNum)
        {
            string sql = this.expression.wrapPageOrder(orderByPart, readsql, pageSize, pageNum);
            return exeQuery(sql, null);
        }
        /// <summary>
        /// 根据某个字段，查询是否存在记录
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool checkExistKey(string key, Object value)
        {
            return checkExistKey(key, value, this.name);
        }
        /// <summary>
        /// 根据某个字段，查询是否存在记录
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool checkExistKey(string key, Object value, string tableName)
        {
            var kitTmp = this.copy();
            int cc = kitTmp.from(tableName)
                .where(key, value)
                .count();
            kitTmp = null;
            return cc > 0;
        }
    }
}
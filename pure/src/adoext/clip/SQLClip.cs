using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


using mooSQL.data.clip;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 强类型版本的SQLBuilder，是SQLBuilder加了语法糖的上层类，而不是替代物。其工作引擎为SQLBuilder类。
    /// </summary>
    public partial class SQLClip
    {

        internal ClipProvider provider {  get; set; }
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance DBLive { get; set; }
        /// <summary>
        /// 当前上下文，用于存放当前的SQL语句片段。
        /// </summary>
        public ClipContext Context { get; internal set; }
        /// <summary>
        /// 根据数据库，创建一个全新的Clip实例。
        /// </summary>
        /// <param name="DB"></param>
        public SQLClip(DBInstance DB,SQLBuilder builder=null) { 
            this.provider = new ClipProvider();
            this.DBLive = DB;
            provider.clip = this;
            if (builder == null) {
                builder = DB.useSQL();
            }
            this.Context = new ClipContext(builder);
        }

        /// <summary>
        /// 通过Clip构造Clip，会复用该Clip的一切成员。
        /// </summary>
        /// <param name="parent"></param>
        internal SQLClip(SQLClip parent)
        {
            this.provider = parent.provider;
            this.DBLive = parent.DBLive;
            provider.clip = this;

            this.Context= parent.Context;
        }
        /// <summary>
        /// 清除所有语句，重置Clip。开始新的查询
        /// </summary>
        /// <returns></returns>
        public SQLClip clear() { 
            this.Context.clear();
            return this;
        }
        /// <summary>
        /// 注册打印回调，用于调试SQL语句。
        /// </summary>
        /// <param name="onPrint"></param>
        /// <returns></returns>
        public SQLClip print(Action<string> onPrint)
        {
            Context.Builder.print(onPrint);
            return this;
        }
        /// <summary>
        /// 传递事务
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public SQLClip useTransaction(DBExecutor core)
        {
            Context.Builder.useTransaction(core);
            return this;
        }
        /// <summary>
        /// 唯一
        /// </summary>
        /// <returns></returns>
        public SQLClip distinct()
        {
            Context.Builder.distinct();
            return this;
        }


        /// <summary>
        /// from语句，用于指定查询的主体表。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public SQLClip from<T>(out T table) where T:new() { 
            var tar= new T();
            table = tar;

            var bt = new ClipTable()
            {
                BindValue = tar,
                EnityType = typeof(T),
                TableInfo = DBLive.client.EntityCash.getEntityInfo<T>(),
                BType= ClipTableType.FromBy
            };
            this.Context.BindFrom(tar, bt);
          
            return this;
        }
        /// <summary>
        /// 设置from部分的别名。仅在没有where表达式使用。
        /// </summary>
        /// <param name="asName"></param>
        /// <returns></returns>
        internal SQLClip setFromAsName(string asName) { 
            var tb=this.Context.getFromTable();
            tb.Alias = asName;
            return this;
        }

        internal SQLClip wherePKIs<T>(object pkValue) { 
            var en=DBLive.client.EntityCash.getEntityInfo<T>();
            var pks=en.GetPK();
            if(pks.Count==1)
            {
                var pk = pks[0];
                var tb = this.Context.getFromTable();
                if (tb.Alias.HasText())
                {
                    this.Context.Builder.where(tb.Alias + "." + pk.DbColumnName, pkValue);
                }
                else {
                    this.Context.Builder.where( pk.DbColumnName, pkValue);
                }
            }
            else
            {
                throw new Exception("复合主键不支持此方法，请使用where表达式指定条件");
            }   
            return this;
        }

        /// <summary>
        /// 直接设置from部分的表名。此为手动指定表名使用，仍要先用from语句指定实体。
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public SQLClip from(string tableName) {
            if (this.Context._fromTarget == null) {
                throw new Exception("必须先指定绑定的查询实体类");
            }
            var tb = this.Context.getFromTable();
            tb.BSrc = ClipTableSrc.LiveTable;
            tb.querySQL = tableName;
            return this;
        }
        /// <summary>
        /// 绑定实体的，同时手动指定表名。用于动态分表时使用。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tbname"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public SQLClip from<T>(string tbname,out T table) where T : new()
        {
            var tar = new T();
            table = tar;

            var bt = new ClipTable()
            {
                BindValue = tar,
                EnityType = typeof(T),
                TableInfo = DBLive.client.EntityCash.getEntityInfo<T>(),
                BType = ClipTableType.FromBy,
                BSrc = ClipTableSrc.LiveTable,
                querySQL = tbname
            };
            this.Context.BindFrom(tar, bt);

            return this;
        }
        /// <summary>
        /// 构造JOIN语句，用于连接其他表。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <returns></returns>
        public ClipJoin<J> join<J>(out J tableJ, string joinPrefix = "join") where J : new()
        {
            tableJ = new J();
            //tableJ = AnonyTypeUtil.CreateInstanceWithDefaults<J>();
            var join = new ClipJoin<J>(this);
            join.JoinTarget = tableJ;
            join.JoinType = joinPrefix;
            var bt = new ClipTable()
            {
                BindValue = tableJ,
                EnityType = typeof(J),
                TableInfo = DBLive.client.EntityCash.getEntityInfo<J>(),
                BType = ClipTableType.JoinBy,
                BSrc = ClipTableSrc.Entity
            };
            this.Context.BindJoin(tableJ,bt);
            return join;
        }
        /// <summary>
        /// 构造JOIN语句，用于连接其他表。支持子查询作为JOIN对象。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <param name="joinPrefix"></param>
        /// <param name="subfrom"></param>
        /// <returns></returns>
        public ClipJoin<J> join<J>(out J tableJ, string joinPrefix, Func<SQLClip, SQLClip<J>> subfrom)
        {

            var bro = Context.Builder.getBrotherBuilder();
            var sub = DBLive.useClip(bro);
            subfrom(sub);
            var sql =  sub.toSelect().sql ;

            tableJ = AnonyTypeUtil.CreateInstanceWithDefaults<J>();
            var join = new ClipJoin<J>(this);
            join.JoinTarget = tableJ;
            join.JoinType = joinPrefix;
            var bt = new ClipTable()
            {
                BindValue = tableJ,
                EnityType = typeof(J),
                BType = ClipTableType.JoinBy,
                BSrc = ClipTableSrc.SubSQL,
                querySQL = sql,
            };
            this.Context.BindJoin(tableJ, bt);
            return join;

        }

        public ClipJoin<J> LeftJoin<J>(out J tableJ) where J : new()
        {
            return join<J>(out tableJ, "LEFT JOIN");
        }

        /// <summary>
        /// 构造LEFT JOIN语句，用于连接其他表。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <returns></returns>
        public ClipJoin<J> LeftJoin<J>(out J tableJ, Func<SQLClip, SQLClip<J>> subfrom) where J : new() { 
            return join<J>(out tableJ, "LEFT JOIN", subfrom);
        }
        /// <summary>
        /// 构造RIGHT JOIN语句，用于连接其他表。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <returns></returns>
        public ClipJoin<J> RightJoin<J>(out J tableJ) where J : new()
        {
            return join<J>(out tableJ, "RIGHT JOIN");
        }
        /// <summary>
        /// 构造FULL JOIN语句，用于连接其他表。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <returns></returns>
        public ClipJoin<J> FullJoin<J>(out J tableJ) where J : new()
        {
            return join<J>(out tableJ, "FULL JOIN");
        }

        /// <summary>
        ///  构造SELECT语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="selectCondition"></param>
        /// <returns></returns>
        public SQLClip<R> select<R>(Expression<Func<R>> selectCondition) { 
            //provider.PatchSelect(selectCondition);
            provider.PatchSelect(selectCondition);
            return new SQLClip<R>(this) {};
        }
        /// <summary>
        /// 查询某个表的所有字段。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLClip<R> select<R>(R val) where R:class
        {
            //provider.PatchSelect(selectCondition);
            if (!Context.BindTables.ContainsKey(val)) { 
                throw new Exception("只允许使用绑定的表变量，未绑定表：" + val.GetType().Name);
            }
            provider.PatchSelect(()=>val);
            return new SQLClip<R>(this) { };
        }


        /// <summary>
        /// 构造TOP语句。例如：top(10) 即 select top 10 * from ...;
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public SQLClip top(int num) { 
            Context.Builder.top(num);
            return this;
        }

        /// <summary>
        /// 构造GROUP BY语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="groupCondition"></param>
        /// <returns></returns>
        public SQLClip groupBy<R>(Expression<Func<R>> groupCondition) {
            //Builder.groupBy(groupCondition);
            provider.PatchGroupBy(groupCondition);
            return this;
        }
        /// <summary>
        /// 构造HAVING语句。
        /// </summary>
        /// <param name="groupCondition"></param>
        /// <returns></returns>
        public SQLClip having(Expression<Func<bool>> groupCondition)
        {
            //Builder.groupBy(groupCondition);
            provider.PatchHaving(groupCondition);
            return this;
        }
        /// <summary>
        /// 构造ORDER BY语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="orderCondition"></param>
        /// <returns></returns>
        public SQLClip orderBy<R>(Expression<Func<R>> orderCondition) { 
            //Builder.orderBy(orderCondition);
            provider.PatchOrderBy(orderCondition);
            return this;
        }
        /// <summary>
        /// 构造ORDER BY DESC语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="orderCondition"></param>
        /// <returns></returns>
        public SQLClip orderByDesc<R>(Expression<Func<R>> orderCondition)
        {
            //Builder.orderBy(orderCondition);
            provider.PatchOrderBy(orderCondition,true);
            return this;
        }
        /// <summary>
        /// 获取构造好的SQL语句。
        /// </summary>
        /// <returns></returns>
        public SQLCmd toSelect() { 
            return Context.Builder.toSelect();
        }
        /// <summary>
        /// 返回总行数
        /// </summary>
        /// <returns></returns>
        public int count() {
            provider.PatchBeforeSelect();
            return Context.Builder.count();
        }
    }

}

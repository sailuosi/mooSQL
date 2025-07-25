using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


using mooSQL.data.clip;

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
        /// 构造JOIN语句，用于连接其他表。
        /// </summary>
        /// <typeparam name="J"></typeparam>
        /// <param name="tableJ"></param>
        /// <returns></returns>
        public ClipJoin<J> join<J>(out J tableJ, string joinPrefix = "join") where J : new()
        {
            tableJ = new J();
            var join = new ClipJoin<J>(this);
            join.JoinTarget = tableJ;
            join.JoinType = joinPrefix;
            var bt = new ClipTable()
            {
                BindValue = tableJ,
                EnityType = typeof(J),
                TableInfo = DBLive.client.EntityCash.getEntityInfo<J>(),
                BType = ClipTableType.JoinBy,
            };
            this.Context.BindJoin(tableJ,bt);
            return join;
        }

        /// <summary>
        /// 构造普通where语句
        /// </summary>
        /// <param name="whereCondition"></param>
        /// <returns></returns>
        public SQLClip where(Expression<Func<bool>> whereCondition) { 
            provider.PatchWhere(whereCondition);
            return this;
        }
        /// <summary>
        /// 构造自定义SQL语句，用于直接使用原始的SQL语句。例如：useSQL(x=>x.where("id",1)); 即 where id=1; 相当于 Builder.where("id",1); 
        /// </summary>
        /// <param name="doProtoSQLBuilder"></param>
        /// <returns></returns>
        public SQLClip useSQL(Action<SQLBuilder> doProtoSQLBuilder)
        {
            if (doProtoSQLBuilder != null) {
                doProtoSQLBuilder(Context.Builder);
            }
            
            return this;
        }

        /// <summary>
        /// 构造where语句，用于指定字段和值。例如：where(x=>x.id,1) 即 where id=1; 相当于 Builder.where("id",1); 但是前者更安全，后者更灵活。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, R value)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.where(field, value);
            }
            return this;
        }
        /// <summary>
        /// 支持操作符定义的where语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, R value,string op)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.where(field, value, op);
            }
            return this;
        }
        /// <summary>
        /// 构造in语句，用于指定字段和值集合。例如：whereIn(x=>x.id,new int[]{1,2}) 即 where id in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            //Builder.orderBy(orderCondition);
            var field= provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field)) {
                Context.Builder.whereIn(field, values);
            }
            return this;
        }
        /// <summary>
        /// 构造not in语句，用于指定字段和值集合。例如：whereNotIn(x=>x.id,new int[]{1,2}) 即 where id not in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereNotIn(field, values);
            }
            return this;
        }
        /// <summary>
        /// 构造like语句，用于模糊查询字段。例如：whereLike(x=>x.name,"abc") 即 where name like '%abc%' 默认是两边模糊匹配。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="searchTxt"></param>
        /// <returns></returns>
        public SQLClip whereLike(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereLike(field, searchTxt);
            }
            return this;
        }
        /// <summary>
        /// 构造左like语句，即模糊查询左侧字段。例如：LIKE 'abc%' 而不是 LIKE '%abc'。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="searchTxt"></param>
        /// <returns></returns>
        public SQLClip whereLikeLeft(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereLikeLeft(field, searchTxt);
            }
            return this;
        }


        /// <summary>
        /// 使用子查询，并指定操作符
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="op"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, string op, Func<SQLClip,SQLClip<R>> doSubSelect) {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                var bro = Context.Builder.getBrotherBuilder();
                var sub = DBLive.useClip(bro);
                doSubSelect(sub);
                var sql= " (" + sub.toSelect().sql + ") ";
                Context.Builder.where(field, sql,op,false);
            }
            return this;
        }
        /// <summary>
        /// 子查询模式的where in
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect) {
            return where(fieldSelector, "IN", doSubSelect);
        }
        /// <summary>
        /// 子查询模式的where not in 语句。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            return where(fieldSelector, "NOT IN", doSubSelect);
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
        /// 暴露一个原始的SQLBulder构建方法
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public SQLClip where(string SQL)
        {
            Context.Builder.where(SQL);
            return this;
        }
        /// <summary>
        /// 暴露原始的SQL条件构建方法。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLClip where(string key, Object val, string op, bool paramed)
        {
            Context.Builder.where(key, val, op, paramed, null);
            return this;
        }
        /// <summary>
        /// 开启一个新的分支，用于构造AND语句。
        /// </summary>
        /// <returns></returns>
        public SQLClip sink() { 
            Context.Builder.sink();
            return this;
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
        /// 开启一个新的分支，用于构造OR语句。
        /// </summary>
        /// <returns></returns>
        public SQLClip sinkOR() {
            Context.Builder.sinkOR();
            return this;
        }
        /// <summary>
        /// 回溯上一个分支。
        /// </summary>
        /// <returns></returns>
        public SQLClip rise() {
            Context.Builder.rise();
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
    }
    /// <summary>
    /// Clip的泛型版本，用于构造特定类型的更新、删除语句。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class SQLClip<T> : SQLClip {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="DB"></param>
        public SQLClip(DBInstance DB) : base(DB) { }
        /// <summary>
        /// 构造，用于复制一个Clip实例。
        /// </summary>
        /// <param name="clip"></param>
        public SQLClip(SQLClip clip) : base(clip)
        {


        }
        /// <summary>
        /// 设置翻页参数
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public SQLClip<T> setPage(int pageSize, int pageNum) { 
            
            Context.Builder.setPage(pageSize, pageNum);
            return this;
        }


        /// <summary>
        /// 查询出唯一结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public T queryUnique() {
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryScalar<T>();
            }
            else {
                return Context.Builder.queryUnique<T>();
            }
        }
        /// <summary>
        /// 查询出列表结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> queryList()
        {
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryFirstField<T>();
            }
            else
            {
                return Context.Builder.query<T>();
            }
        }
        /// <summary>
        /// 查询出分页结果。
        /// </summary>
        /// <returns></returns>
        public PageOutput<T> queryPage() { 
        
            return Context.Builder.queryPaged<T>();
        }
    }
    /// <summary>
    /// JOIN语句构造中间过渡类
    /// </summary>
    public class ClipJoin<T> {
        /// <summary>
        /// 根Clip实例引用。
        /// </summary>
        public SQLClip root;
        /// <summary>
        /// JOIN的目标表实例引用。
        /// </summary>
        public object JoinTarget { get; set; }
        /// <summary>
        /// JOIN的类型，例如INNER JOIN、LEFT JOIN等。
        /// </summary>
        public string JoinType { get; set; }
        public ClipJoin(SQLClip roo) { 
            this.root = roo;
        }
        /// <summary>
        /// 构造JOIN语句的ON条件。
        /// </summary>
        /// <param name="joinCondition"></param>
        /// <returns></returns>
        public SQLClip on(Expression<Func<bool>> joinCondition) { 
            root.provider.PatchJoin(joinCondition,this);
            return this.root;
        }
    }
}

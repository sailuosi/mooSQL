
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 接入了一个实体类型的SQLBuilder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SQLBuilder<T>:SQLBuilder
    {

        public SQLBuilder(DBInstance DB) { 
            this.setDBInstance(DB);
        }

        protected SetExpressionSQLBuildVisitor setVisitor;
        protected WhereExpressionVisitor whereVisitor;
        protected SelectExpressionVisitor selectVisitor;

        private FastCompileContext _context;
        public FastCompileContext CompileContext
        {
            get {
                if (_context != null) {
                    return _context;
                }
                this._context = new FastCompileContext();
                _context.initByBuilder(this);

                return this._context;
            }
        }
        /// <summary>
        /// 使用lamada的方式，设置实体的字段赋值
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public SQLBuilder<T> set(Expression<Func<T, bool[]>> selector) {
            //调用访问器直接将表达式转为SQL

            if (setVisitor == null) { 
                setVisitor = new SetExpressionSQLBuildVisitor(CompileContext);
            }
            //-		selector	{d => new [] {(d.Di_Code == "a"), (d.Di_Code == "b")}}	System.Linq.Expressions.Expression<System.Func<HHNY.NET.Application.Entity.HHDutyItem, bool[]>> {System.Linq.Expressions.Expression1<System.Func<HHNY.NET.Application.Entity.HHDutyItem, bool[]>>}

            setVisitor.Visit(selector);

            return this;
        }
        /// <summary>
        /// 设置当前实体类T的数据库表名
        /// </summary>
        /// <returns></returns>
        public SQLBuilder<T> setTable() {
            var tbname = DBLive.client.EntityCash.getTableName(typeof(T));
            this.setTable(tbname);
            return this;
        }

        public SQLBuilder<T> where(Expression<Func<T, bool>> howIsWhere) {
            if (whereVisitor == null) {
                this.whereVisitor = new WhereExpressionVisitor(CompileContext);
            }
            var t= whereVisitor.Visit(howIsWhere.Body);
            return this;
        }
        /// <summary>
        /// 字段选择
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public SQLBuilder<T> select(Expression<Func<T, IEnumerable<object>>> selector, bool autoFrom = true) {
            if (selectVisitor == null)
            {
                this.selectVisitor = new SelectExpressionVisitor(CompileContext);
            }
            if (autoFrom)
            {
                //自动添加from子句
                var para = selector.Parameters[0];
                var tbname = DBLive.client.EntityCash.getTableName(para.Type);
                var asName = para.Name;
                this.from(tbname + " as " + asName);
                selectVisitor.VisitFrom(selector.Body, asName);
                return this;
            }
            selectVisitor.VisitFrom(selector.Body, "");
            return this;
        

        }
    }
}

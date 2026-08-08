using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.linq.Linq;
using mooSQL.linq.translator;

namespace mooSQL.data
{
    /// <summary>
    /// Ext LINQ 编译查询 API：形状编译一次，参数经每次表达式的 ParameterAccessor 取值。
    /// </summary>
    public static class ExtCompiledQueryExtensions
    {
        /// <summary>
        /// 编译 <paramref name="factory"/> 产生的 IQueryable 形状，返回 <c>(db, arg) => List&lt;T&gt;</c>。
        /// 每次执行仍构建表达式树以供参数访问，但跳过 <see cref="QueryMate.CreateQuery"/> 全量编译（复用 SentenceBag）。
        /// </summary>
        public static Func<DBInstance, TParam, List<T>> CompileQuery<T, TParam>(
            this DBInstance db,
            Expression<Func<DBInstance, TParam, IQueryable<T>>> factory)
            where T : notnull
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var compiledFactory = factory.Compile();
            var sample = compiledFactory(db, default!);
            var sampleExpr = sample.Expression;
            var bag = QueryMate.GetQuery<T>(db, ref sampleExpr, out _);
            SentenceExecutor.FinalizeBag(bag, db);

            return (liveDb, param) =>
            {
                var q = compiledFactory(liveDb, param);
                return SentenceExecutor.ExecuteList<T>(bag, liveDb, q.Expression);
            };
        }

        /// <summary>
        /// 无额外参数的编译查询（仅绑定 DBInstance）。
        /// </summary>
        public static Func<DBInstance, List<T>> CompileQuery<T>(
            this DBInstance db,
            Expression<Func<DBInstance, IQueryable<T>>> factory)
            where T : notnull
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var compiledFactory = factory.Compile();
            var sample = compiledFactory(db);
            var sampleExpr = sample.Expression;
            var bag = QueryMate.GetQuery<T>(db, ref sampleExpr, out _);
            SentenceExecutor.FinalizeBag(bag, db);

            return liveDb =>
            {
                var q = compiledFactory(liveDb);
                return SentenceExecutor.ExecuteList<T>(bag, liveDb, q.Expression);
            };
        }

        /// <summary>
        /// 对已有 <see cref="IQueryable{T}"/> 表达式编译一次，返回接受「同形状 live 表达式」的执行器。
        /// </summary>
        public static Func<Expression, List<T>> CompileQueryExpression<T>(this DBInstance db, Expression queryExpression)
            where T : notnull
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (queryExpression == null) throw new ArgumentNullException(nameof(queryExpression));

            var expr = queryExpression;
            var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
            SentenceExecutor.FinalizeBag(bag, db);

            return liveExpr => SentenceExecutor.ExecuteList<T>(bag, db, liveExpr);
        }
    }
}

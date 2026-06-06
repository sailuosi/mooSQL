using System;

namespace mooSQL.linq
{
    /// <summary>
    /// 推荐别名：<see cref="DbFunc.ExpressionAttribute"/>。标记 LINQ Lambda 中可翻译为 SQL 的方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class DbFuncExpressionAttribute : DbFunc.ExpressionAttribute
    {
        public DbFuncExpressionAttribute(string? expression) : base(expression) { }

        public DbFuncExpressionAttribute(string expression, params int[] argIndices) : base(expression, argIndices) { }

        public DbFuncExpressionAttribute(string configuration, string expression) : base(configuration, expression) { }

        public DbFuncExpressionAttribute(string configuration, string expression, params int[] argIndices)
            : base(configuration, expression, argIndices) { }
    }
}

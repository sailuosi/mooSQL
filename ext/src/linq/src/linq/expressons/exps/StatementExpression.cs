using System;
using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.translator;

namespace mooSQL.linq.Expressions;

/// <summary>
/// 编译链上的序列/语句产物节点（Fast 同构：成功解析后折叠回表达式树）。
/// </summary>
sealed class StatementExpression : Expression, IEquatable<StatementExpression>
{
    public StatementExpression(
        IClauseContext buildContext,
        ClauseCompileContext? compileContext = null,
        Expression? projection = null)
    {
        BuildContext   = buildContext ?? throw new ArgumentNullException(nameof(buildContext));
        CompileContext = compileContext;
        Projection     = projection ?? buildContext.Expression;
    }

    public IClauseContext BuildContext { get; }

    public ClauseCompileContext? CompileContext { get; }

    public Expression? Projection { get; }

    public SelectQueryClause SelectQuery => BuildContext.SelectQuery;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => BuildContext.ElementType;

    public override bool CanReduce => false;

    public static StatementExpression FromBuildContext(
        IClauseContext buildContext,
        ClauseCompileContext? compileContext = null,
        Expression? projection = null)
        => new(buildContext, compileContext, projection);

    public BaseSentence ToStatement() => BuildContext.GetResultStatement();

    public StatementExpression WithBuildContext(IClauseContext buildContext)
    {
        if (ReferenceEquals(buildContext, BuildContext))
            return this;

        return new StatementExpression(buildContext, CompileContext, Projection);
    }

    public StatementExpression WithProjection(Expression? projection)
    {
        if (ReferenceEquals(projection, Projection))
            return this;

        return new StatementExpression(BuildContext, CompileContext, projection);
    }

    protected override Expression Accept(ExpressionVisitor visitor)
    {
        if (visitor is ExpressionVisitorBase baseVisitor)
            return baseVisitor.VisitStatementExpression(this);

        return base.Accept(visitor);
    }

    public override string ToString()
        => $"Stmt({ClauseContextDebuggingHelper.GetContextInfo(BuildContext)}::{Type.Name})";

    public bool Equals(StatementExpression? other)
        => other != null
           && ReferenceEquals(BuildContext, other.BuildContext)
           && ReferenceEquals(CompileContext, other.CompileContext);

    public override bool Equals(object? obj) => obj is StatementExpression other && Equals(other);

    public override int GetHashCode() => BuildContext.GetHashCode();
}

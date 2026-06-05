using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

/// <summary>
/// 双工访问器编译会话：统一从 <see cref="ClauseExpressionVisitor"/> 根 Visit 产出 <see cref="StatementExpression"/>。
/// </summary>
internal sealed class StatementCompileSession
{
    readonly BuildInfo _buildInfo;
    readonly Expression _originalExpression;

    StatementCompileSession(
        ClauseCompileContext context,
        ClauseMethodVisitor methodVisitor,
        ClauseExpressionVisitor expressionVisitor,
        BuildInfo buildInfo,
        Expression originalExpression)
    {
        Context = context;
        MethodVisitor = methodVisitor;
        ExpressionVisitor = expressionVisitor;
        _buildInfo = buildInfo;
        _originalExpression = originalExpression;
    }

    public ClauseCompileContext Context { get; }

    public ClauseMethodVisitor MethodVisitor { get; }

    public ClauseExpressionVisitor ExpressionVisitor { get; }

    public static StatementCompileSession Create(ClauseSqlTranslator translator, BuildInfo buildInfo)
    {
        var originalExpression = buildInfo.Expression;
        var expanded = translator.ExpandToRoot(buildInfo.Expression, buildInfo);
        if (!ReferenceEquals(expanded, originalExpression))
            buildInfo = new BuildInfo(buildInfo, expanded);

        var context = new ClauseCompileContext(translator, buildInfo);
        var methodVisitor = new ClauseMethodVisitor { Context = context };
        var expressionVisitor = new ClauseExpressionVisitor(methodVisitor) { Context = context };
        methodVisitor.Buddy = expressionVisitor;

        return new StatementCompileSession(context, methodVisitor, expressionVisitor, buildInfo, originalExpression);
    }

    /// <summary>始终经 <see cref="ClauseExpressionVisitor"/> 完整访问表达式树。</summary>
    public Expression VisitRoot(Expression expression)
        => ExpressionVisitor.Visit(expression);

    public BuildSequenceResult ToBuildSequenceResult(Expression? resultExpr, ClauseSqlTranslator translator)
    {
        if (resultExpr is StatementExpression stmt && stmt.BuildContext is { } buildContext)
        {
            Context.StatementResult = stmt;
            var stmtResult = BuildSequenceResult.FromContext(buildContext);
#if DEBUG
            if (!_buildInfo.IsTest)
                QueryHelper.DebugCheckNesting(buildContext.GetResultStatement(), _buildInfo.IsSubQuery);
#endif
            translator.RegisterSequenceExpression(buildContext, _originalExpression);

            if (!stmtResult.IsSequence)
                return BuildSequenceResult.Error(_originalExpression);

            return stmtResult;
        }

        if (resultExpr is SqlErrorExpression error)
            return BuildSequenceResult.Error(error);

        return BuildSequenceResult.NotSupported();
    }
}

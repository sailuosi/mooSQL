using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

/// <summary>
/// Where/Having lambda 内谓词表达式访问器（Phase E）。
/// 处理 <see cref="WhereFieldLINQExtensions"/> 等 mooSQL 谓词扩展；其余仍委托 <see cref="ClauseSqlTranslator"/>。
/// </summary>
internal sealed class ClausePredicateVisitor
{
    readonly ClauseSqlTranslator _translator;
    readonly IBuildContext _sequence;

    public ClausePredicateVisitor(ClauseSqlTranslator translator, IBuildContext sequence)
    {
        _translator = translator;
        _sequence = sequence;
    }

    /// <summary>构建 WHERE/HAVING 搜索条件（统一入口）。</summary>
    public bool BuildSearchCondition(
        Expression expression,
        ProjectFlags flags,
        SearchConditionWord searchCondition,
        [NotNullWhen(false)] out SqlErrorExpression? error)
        => _translator.BuildSearchCondition(_sequence, expression, flags, searchCondition, out error);

    /// <summary>将谓词表达式转换为 SQL 条件片段。</summary>
    public IExpWord? ConvertPredicate(Expression predicate)
    {
        if (predicate == null)
            return null;

        return _translator.ConvertToSql(_sequence, predicate.Unwrap());
    }

    /// <summary>
    /// 识别并转换 mooSQL Where 字段扩展（Like/InList/IsNull 等）。
    /// </summary>
    public static bool TryConvertMooExtension(
        ClauseSqlTranslator builder,
        IBuildContext? context,
        MethodCallExpression expression,
        ProjectFlags flags,
        [NotNullWhen(true)] out IAffirmWord? predicate)
    {
        predicate = null;

        if (context == null || expression.Method.DeclaringType != typeof(WhereFieldLINQExtensions))
            return false;

        predicate = expression.Method.Name switch
        {
            nameof(WhereFieldLINQExtensions.Like)           => ConvertLike(builder, context, expression, flags),
            nameof(WhereFieldLINQExtensions.LikeLeft)       => ConvertLikeLeft(builder, context, expression, flags),
            nameof(WhereFieldLINQExtensions.InList)         => ConvertInList(builder, context, expression, flags),
            nameof(WhereFieldLINQExtensions.IsNull)           => ConvertIsNull(builder, context, expression, isNot: false, flags),
            nameof(WhereFieldLINQExtensions.IsNotNull)      => ConvertIsNull(builder, context, expression, isNot: true, flags),
            nameof(WhereFieldLINQExtensions.IsNullOrWhiteSpace) => ConvertIsNullOrWhiteSpace(builder, context, expression, flags),
            _ => null
        };

        return predicate != null;
    }

    /// <summary>扩展方法在表达式树中为静态调用（Object 为 null，实参在 Arguments）。</summary>
    static bool TryGetExtensionField(MethodCallExpression e, [NotNullWhen(true)] out Expression? fieldExpr, out Expression[] valueArgs)
    {
        if (e.Method.IsStatic && e.Method.DeclaringType == typeof(WhereFieldLINQExtensions))
        {
            if (e.Arguments.Count < 1)
            {
                fieldExpr = null;
                valueArgs = Array.Empty<Expression>();
                return false;
            }

            fieldExpr = e.Arguments[0];
            valueArgs = e.Arguments.Count > 1
                ? e.Arguments.Skip(1).ToArray()
                : Array.Empty<Expression>();
            return true;
        }

        if (e.Object != null)
        {
            fieldExpr = e.Object;
            valueArgs = e.Arguments.ToArray();
            return true;
        }

        fieldExpr = null;
        valueArgs = Array.Empty<Expression>();
        return false;
    }

    static IAffirmWord? ConvertLike(ClauseSqlTranslator builder, IBuildContext context, MethodCallExpression e, ProjectFlags flags)
    {
        if (!TryGetExtensionField(e, out var fieldExpr, out var valueArgs) || valueArgs.Length < 1)
            return null;

        var descriptor = builder.SuggestColumnDescriptor(context, fieldExpr, valueArgs[0], flags);
        var field = ClauseFieldVisitor.ConvertField(builder, context, fieldExpr, flags) ?? builder.ConvertToSql(context, fieldExpr, unwrap: false, columnDescriptor: descriptor);

        var pattern = builder.ConvertToSql(context, valueArgs[0], flags, unwrap: false, columnDescriptor: descriptor);

        return new Like(field, false, pattern, null);
    }

    static IAffirmWord? ConvertLikeLeft(ClauseSqlTranslator builder, IBuildContext context, MethodCallExpression e, ProjectFlags flags)
    {
        if (!TryGetExtensionField(e, out var fieldExpr, out var valueArgs) || valueArgs.Length < 1)
            return null;

        var descriptor = builder.SuggestColumnDescriptor(context, fieldExpr, valueArgs[0], flags);
        var field = ClauseFieldVisitor.ConvertField(builder, context, fieldExpr, flags) ?? builder.ConvertToSql(context, fieldExpr, unwrap: false, columnDescriptor: descriptor);
        var pattern = builder.ConvertToSql(context, valueArgs[0], unwrap: false, columnDescriptor: descriptor);
        var caseSensitive = new ValueWord(typeof(bool?), null);

        return new SearchString(field, false, pattern, SearchString.SearchKind.StartsWith, caseSensitive);
    }

    static IAffirmWord? ConvertInList(ClauseSqlTranslator builder, IBuildContext context, MethodCallExpression e, ProjectFlags flags)
    {
        if (!TryGetExtensionField(e, out var fieldExpr, out var valueArgs) || valueArgs.Length < 1)
            return null;

        var descriptor = builder.SuggestColumnDescriptor(context, fieldExpr, flags);
        var field = ClauseFieldVisitor.ConvertField(builder, context, fieldExpr, flags) ?? builder.ConvertToSql(context, fieldExpr, unwrap: false, columnDescriptor: descriptor);
        var withNull = builder.DBLive.dialect.Option.CompareNullsAsValues ? false : (bool?)null;
        var listExpr = valueArgs[0].Unwrap();

        switch (listExpr.NodeType)
        {
            case ExpressionType.NewArrayInit:
            {
                var newArr = (NewArrayExpression)listExpr;
                if (newArr.Expressions.Count == 0)
                    return AffirmWord.False;

                var values = new IExpWord[newArr.Expressions.Count];
                for (var i = 0; i < newArr.Expressions.Count; i++)
                    values[i] = builder.ConvertToSql(context, newArr.Expressions[i], columnDescriptor: descriptor);

                return new InList(field, withNull, false, values);
            }
            default:
                if (builder.CanBeCompiled(listExpr, false))
                {
                    var parameter = builder.ParametersContext.BuildParameter(
                        context,
                        listExpr,
                        descriptor,
                        forceConstant: false,
                        buildParameterType: ParametersContext.BuildParameterType.InPredicate)!.SqlParameter;
                    parameter.IsQueryParameter = false;
                    return new InList(field, withNull, false, parameter);
                }

                return null;
        }
    }

    static IAffirmWord? ConvertIsNull(ClauseSqlTranslator builder, IBuildContext context, MethodCallExpression e, bool isNot, ProjectFlags flags)
    {
        if (!TryGetExtensionField(e, out var fieldExpr, out _))
            return null;

        var field = ClauseFieldVisitor.ConvertField(builder, context, fieldExpr, flags) ?? builder.ConvertToSql(context, fieldExpr, unwrap: false, flags: flags);
        return new IsNull(field, isNot);
    }

    static IAffirmWord? ConvertIsNullOrWhiteSpace(ClauseSqlTranslator builder, IBuildContext context, MethodCallExpression e, ProjectFlags flags)
    {
        if (!TryGetExtensionField(e, out var fieldExpr, out _))
            return null;

        var descriptor = builder.SuggestColumnDescriptor(context, fieldExpr, flags);
        var field = ClauseFieldVisitor.ConvertField(builder, context, fieldExpr, flags) ?? builder.ConvertToSql(context, fieldExpr, unwrap: false, columnDescriptor: descriptor);
        var empty = builder.ConvertToSql(context, Expression.Constant(string.Empty), unwrap: false, columnDescriptor: descriptor);

        var isNull = new IsNull(field, false);
        var isEmpty = new ExprExpr(field, AffirmWord.Operator.Equal, empty, null);
        return new SearchConditionWord(true).Add(isNull).Add(isEmpty);
    }
}

using System.Linq.Expressions;

namespace mooSQL.data.call;

/// <summary>
/// 将语句产物 <see cref="Expression"/> 回传到表达式访问器链（对齐 <see cref="ExpressionCall"/>）。
/// </summary>
public class StatementCall : MethodCall
{
    public StatementCall() : base("Statement", null)
    {
    }

    public Expression? Value { get; set; }

    public override MethodCall Accept(MethodVisitor visitor)
        => visitor.VisitStatement(this);
}

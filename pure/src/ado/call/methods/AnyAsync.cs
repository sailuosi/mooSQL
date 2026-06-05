namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 AnyAsync 的调用节点（AnyAsyncCall）。</summary>
public class AnyAsyncCall : MethodCall
{
    public AnyAsyncCall() : base("AnyAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitAnyAsync(this);
}

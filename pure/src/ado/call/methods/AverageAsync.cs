namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 AverageAsync 的调用节点（AverageAsyncCall）。</summary>
public class AverageAsyncCall : MethodCall
{
    public AverageAsyncCall() : base("AverageAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitAverageAsync(this);
}

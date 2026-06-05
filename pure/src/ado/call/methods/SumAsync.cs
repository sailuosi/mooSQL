namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 SumAsync 的调用节点（SumAsyncCall）。</summary>
public class SumAsyncCall : MethodCall
{
    public SumAsyncCall() : base("SumAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitSumAsync(this);
}

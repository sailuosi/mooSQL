namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 FirstAsync 的调用节点（FirstAsyncCall）。</summary>
public class FirstAsyncCall : MethodCall
{
    public FirstAsyncCall() : base("FirstAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitFirstAsync(this);
}

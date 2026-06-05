namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 LongCountAsync 的调用节点（LongCountAsyncCall）。</summary>
public class LongCountAsyncCall : MethodCall
{
    public LongCountAsyncCall() : base("LongCountAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitLongCountAsync(this);
}

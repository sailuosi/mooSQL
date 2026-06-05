namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 CountAsync 的调用节点（CountAsyncCall）。</summary>
public class CountAsyncCall : MethodCall
{
    public CountAsyncCall() : base("CountAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitCountAsync(this);
}

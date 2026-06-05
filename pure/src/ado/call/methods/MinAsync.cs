namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 MinAsync 的调用节点（MinAsyncCall）。</summary>
public class MinAsyncCall : MethodCall
{
    public MinAsyncCall() : base("MinAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitMinAsync(this);
}

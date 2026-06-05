namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 AllAsync 的调用节点（AllAsyncCall）。</summary>
public class AllAsyncCall : MethodCall
{
    public AllAsyncCall() : base("AllAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitAllAsync(this);
}

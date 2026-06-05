namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 SingleAsync 的调用节点（SingleAsyncCall）。</summary>
public class SingleAsyncCall : MethodCall
{
    public SingleAsyncCall() : base("SingleAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitSingleAsync(this);
}

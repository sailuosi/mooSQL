namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 ContainsAsync 的调用节点（ContainsAsyncCall）。</summary>
public class ContainsAsyncCall : MethodCall
{
    public ContainsAsyncCall() : base("ContainsAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitContainsAsync(this);
}

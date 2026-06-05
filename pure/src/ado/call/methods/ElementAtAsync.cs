namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 ElementAtAsync 的调用节点（ElementAtAsyncCall）。</summary>
public class ElementAtAsyncCall : MethodCall
{
    public ElementAtAsyncCall() : base("ElementAtAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitElementAtAsync(this);
}

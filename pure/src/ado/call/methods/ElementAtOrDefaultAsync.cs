namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 ElementAtOrDefaultAsync 的调用节点（ElementAtOrDefaultAsyncCall）。</summary>
public class ElementAtOrDefaultAsyncCall : MethodCall
{
    public ElementAtOrDefaultAsyncCall() : base("ElementAtOrDefaultAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitElementAtOrDefaultAsync(this);
}

namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 SingleOrDefaultAsync 的调用节点（SingleOrDefaultAsyncCall）。</summary>
public class SingleOrDefaultAsyncCall : MethodCall
{
    public SingleOrDefaultAsyncCall() : base("SingleOrDefaultAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitSingleOrDefaultAsync(this);
}

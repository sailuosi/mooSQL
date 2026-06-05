namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 FirstOrDefaultAsync 的调用节点（FirstOrDefaultAsyncCall）。</summary>
public class FirstOrDefaultAsyncCall : MethodCall
{
    public FirstOrDefaultAsyncCall() : base("FirstOrDefaultAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitFirstOrDefaultAsync(this);
}

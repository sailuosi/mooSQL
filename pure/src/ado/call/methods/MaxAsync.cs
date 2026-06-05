namespace mooSQL.data.call;

/// <summary>LINQ 扩展方法 MaxAsync 的调用节点（MaxAsyncCall）。</summary>
public class MaxAsyncCall : MethodCall
{
    public MaxAsyncCall() : base("MaxAsync", null) { }

    public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitMaxAsync(this);
}

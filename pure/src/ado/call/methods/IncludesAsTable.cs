namespace mooSQL.data.call
{
    /// <summary>
    /// LINQ 扩展方法 IncludesAsTable 的调用节点。
    /// </summary>
    public class IncludesAsTableCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitIncludesAsTable(this);

        public IncludesAsTableCall() : base("IncludesAsTable", null) { }
    }
}

namespace mooSQL.data.call
{
    /// <summary>
    /// LINQ 扩展方法 ThenInclude 的调用节点。
    /// </summary>
    public class ThenIncludeCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitThenInclude(this);

        public ThenIncludeCall() : base("ThenInclude", null) { }
    }
}

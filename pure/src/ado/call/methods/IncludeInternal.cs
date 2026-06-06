namespace mooSQL.data.call
{
    /// <summary>
    /// 编译器内部 Includes 链节点。
    /// </summary>
    public class IncludeInternalCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor) => visitor.VisitIncludeInternal(this);

        public IncludeInternalCall() : base("IncludeInternal", null) { }
    }
}

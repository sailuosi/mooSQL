namespace mooSQL.data.call
{
    /// <summary>
    /// LINQ / 查询扩展方法 useQueryable 的调用节点（UseQueryableCall）。
    /// </summary>
    public class UseQueryableCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUseQueryable(this);
        }

        /// <summary>
        /// 创建 useQueryable 方法调用节点。
        /// </summary>
        public UseQueryableCall() : base("useQueryable", null)
        {
        }
    }
}

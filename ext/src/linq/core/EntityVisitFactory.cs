using mooSQL.data;
using mooSQL.linq;

namespace mooSQL.linq.translator
{
    /// <summary>
    /// Fast 风格 <see cref="LinqDbFactory"/> 另轨入口：注入后走 <see cref="EntityVisitCompiler"/>。
    /// <para>
    /// <b>不在</b> 生产主入口 <c>DBInstance.useQueryable</c> / <c>AsQueryable</c> 链路上。
    /// 主路径为 <c>ExtLinqEntry</c> → <c>DbQuery</c> → <c>QueryMate</c> → <c>SentenceExecutor</c>。
    /// 本工厂仅用于测试（如 <c>LinqSqliteTestHelper</c>）或显式注入 <see cref="LinqDbFactory"/> 的场景；
    /// 编译结果仍落到同一套 <c>QueryMate</c> + <c>SentenceExecutor</c>。
    /// </para>
    /// </summary>
    public class EntityVisitFactory : LinqDbFactory
    {
        public override IQueryCompiler GetQueryCompiler(DBInstance DB)
        {
            return new EntityVisitCompiler(DB);
        }
    }
}

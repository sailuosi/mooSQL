using mooSQL.linq;
using mooSQL.linq.translator;

namespace mooSQL.data
{
    /// <summary>
    /// Ext 方言基类：自管 <see cref="IMemberTranslator"/>（与方言实例同寿缓存）。
    /// </summary>
    public abstract class ExtDialect : Dialect
    {
        IMemberTranslator? _memberTranslator;

        /// <summary>
        /// 与方言实例同寿的成员翻译器；构造后只读。含 DbFuncRegistry 包装。
        /// </summary>
        public IMemberTranslator MemberTranslator
            => _memberTranslator ??= new RegistryAwareMemberTranslator(CreateMemberTranslator(), this);

        /// <summary>
        /// 各方言返回内部翻译器（不含 Registry 包装）。
        /// </summary>
        protected virtual IMemberTranslator CreateMemberTranslator()
            => new DefaultMemberTranslator();
    }
}

using System;
using System.Linq.Expressions;

namespace mooSQL.data
{
    /// <summary>
    /// 实体关系 Fluent 构建器（对标 CRL PropertyBuilder.Relation）。
    /// </summary>
    public sealed class EntityRelationBuilder<T> where T : class
    {
        readonly EntityContext _ctx;

        /// <summary>创建构建器。</summary>
        public EntityRelationBuilder(EntityContext ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <summary>
        /// 注册 <typeparamref name="T"/> 与 <typeparamref name="TJoin"/> 的等值关联，并尝试自动绑定两侧导航属性。
        /// </summary>
        public EntityRelationBuilder<T> Relation<TJoin>(Expression<Func<T, TJoin, bool>> expression)
            where TJoin : class
        {
            var info = EntityRelationParser.ParseEquality(expression);
            // 规范化：配置侧 T 可能在等号右侧（如 a.UserId == b.Id 时 T=Blog 仍在左）
            // Parser 按左右成员定 Type1/Type2，与 CRL 一致，无需强制 T 为 Type1。
            _ctx.Relations.RegisterBidirectional(info);
            BindAfterRegister(typeof(T), typeof(TJoin), preferredNavOnT: null);
            return this;
        }

        /// <summary>
        /// 注册关联并显式指定 <typeparamref name="T"/> 上的导航属性（消歧）。
        /// </summary>
        public EntityRelationBuilder<T> Relation<TJoin>(
            Expression<Func<T, object>> nav,
            Expression<Func<T, TJoin, bool>> expression)
            where TJoin : class
        {
            var navName = EntityRelationParser.ParseNavMemberName(nav);
            var info = EntityRelationParser.ParseEquality(expression);
            _ctx.Relations.RegisterBidirectional(info);
            BindAfterRegister(typeof(T), typeof(TJoin), preferredNavOnT: navName);
            return this;
        }

        void BindAfterRegister(Type ownerT, Type joinT, string preferredNavOnT)
        {
            // 确保两侧已分析（可早于业务首次查询）
            _ctx.getEntityInfo(ownerT);
            _ctx.getEntityInfo(joinT);

            // 正向：无 nav 时多匹配须抛；有 nav 时精绑
            EntityRelationBinder.BindOwnerToRelated(_ctx, ownerT, joinT, preferredNavOnT, throwOnAmbiguous: true);
            // 反向：多匹配不阻断（可另侧 configureEntity 消歧）
            EntityRelationBinder.BindOwnerToRelated(_ctx, joinT, ownerT, preferredNavName: null, throwOnAmbiguous: false);
        }
    }
}

using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq.translator;

/// <summary>
/// 组合翻译器：先查 Pure <see cref="mooSQL.data.translation.DbFuncRegistry"/>，再委托方言 MemberTranslator。
/// </summary>
internal sealed class RegistryAwareMemberTranslator : IMemberTranslator
{
    readonly IMemberTranslator _inner;
    readonly DBInstance _db;

    public RegistryAwareMemberTranslator(IMemberTranslator inner, DBInstance db)
    {
        _inner = inner;
        _db = db;
    }

    public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
    {
        DbFuncRegistryBootstrap.EnsureRegistered(_db);

        if (memberExpression is MethodCallExpression mc
            && mc.Method.DeclaringType == typeof(DbFunc)
            && _db.dialect.dbFuncRegistry.Resolve(mc.Method) != null)
        {
            // 注册表命中：仍走 DbFunc 属性翻译（ExpressionAttribute / Extension），注册表供 inspect 与 Pure 迁移。
        }

        return _inner.Translate(translationContext, memberExpression, translationFlags);
    }
}

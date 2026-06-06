using System.Linq.Expressions;
using mooSQL.data.translation;

namespace mooSQL.linq.Linq.Translation
{
    /// <summary>Ext LINQ 成员翻译注册表（Pure <see cref="TranslationRegistration{TContext}"/> 特化）。</summary>
    public class TranslationRegistration : TranslationRegistration<ITranslationContext>
    {
    }
}

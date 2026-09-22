#if NET6_0_OR_GREATER
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.Linq.Translation;
using mooSQL.linq.translator;

namespace mooSQL.data;

/// <summary>ClickHouse MemberTranslator：DatePart/DateAdd 走 Pure <see cref="SQLExpression"/>。</summary>
public class ClickHouseMemberTranslator : DefaultMemberTranslator
{
    protected override IMemberTranslator CreateDateMemberTranslator() => new DateFunctionsTranslator();

    sealed class DateFunctionsTranslator : DateFunctionsTranslatorBase
    {
        protected override IExpWord? TranslateDateTimeDatePart(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            IExpWord dateTimeExpression,
            DbFunc.DateParts datepart)
        {
            var factory = translationContext.ExpressionFactory;
            var intDataType = factory.GetDbDataType(typeof(int));
            var template = DateSqlTemplateResolver.ResolveDatePartFormat(translationContext.DBLive.dialect.expression, datepart);
            if (template == null)
                return null;

            return factory.Fragment(intDataType, template, dateTimeExpression);
        }

        protected override IExpWord? TranslateDateTimeOffsetDatePart(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            IExpWord dateTimeExpression,
            DbFunc.DateParts datepart)
            => TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

        protected override IExpWord? TranslateDateTimeDateAdd(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            IExpWord dateTimeExpression,
            IExpWord increment,
            DbFunc.DateParts datepart)
        {
            var factory = translationContext.ExpressionFactory;
            var dateType = factory.GetDbDataType(dateTimeExpression);
            var template = DateSqlTemplateResolver.ResolveDateAddFormat(translationContext.DBLive.dialect.expression, datepart);
            if (template == null)
                return null;

            return factory.Fragment(dateType, template, increment, dateTimeExpression);
        }

        protected override IExpWord? TranslateDateTimeOffsetDateAdd(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            IExpWord dateTimeExpression,
            IExpWord increment,
            DbFunc.DateParts datepart)
            => TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
    }
}
#endif

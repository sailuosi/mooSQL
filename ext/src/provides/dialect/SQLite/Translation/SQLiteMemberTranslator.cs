using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq.Translation;
using mooSQL.linq.translator;

namespace mooSQL.linq.DataProvider.SQLite.Translation;

/// <summary>SQLite MemberTranslator：DatePart 等日期片段走 Pure <see cref="SQLExpression"/>。</summary>
public class SQLiteMemberTranslator : DefaultMemberTranslator
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
            var template = ResolveDatePartTemplate(translationContext.DBLive.dialect.expression, datepart);
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
            var template = ResolveDateAddTemplate(translationContext.DBLive.dialect.expression, datepart);
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

        static string? ResolveDatePartTemplate(SQLExpression expression, DbFunc.DateParts part)
        {
            const string date = "{0}";
            return part switch
            {
                DbFunc.DateParts.Year         => expression.datePartYear(date),
                DbFunc.DateParts.Quarter      => expression.datePartQuarter(date),
                DbFunc.DateParts.Month        => expression.datePartMonth(date),
                DbFunc.DateParts.DayOfYear    => expression.datePartDayOfYear(date),
                DbFunc.DateParts.Day          => expression.datePartDay(date),
                DbFunc.DateParts.Week         => expression.datePartWeek(date),
                DbFunc.DateParts.WeekDay      => expression.datePartWeekDay(date),
                DbFunc.DateParts.Hour         => expression.datePartHour(date),
                DbFunc.DateParts.Minute       => expression.datePartMinute(date),
                DbFunc.DateParts.Second       => expression.datePartSecond(date),
                DbFunc.DateParts.Millisecond  => expression.datePartMillisecond(date),
                _                             => null
            };
        }

        static string? ResolveDateAddTemplate(SQLExpression expression, DbFunc.DateParts part)
        {
            const string amount = "{0}";
            const string date = "{1}";
            return part switch
            {
                DbFunc.DateParts.Year         => expression.dateAddYear(amount, date),
                DbFunc.DateParts.Quarter      => expression.dateAddQuarter(amount, date),
                DbFunc.DateParts.Month        => expression.dateAddMonth(amount, date),
                DbFunc.DateParts.Week         => expression.dateAddWeek(amount, date),
                DbFunc.DateParts.Day          => expression.dateAddDay(amount, date),
                DbFunc.DateParts.DayOfYear    => expression.dateAddDayOfYear(amount, date),
                DbFunc.DateParts.WeekDay      => expression.dateAddWeekDay(amount, date),
                DbFunc.DateParts.Hour         => expression.dateAddHour(amount, date),
                DbFunc.DateParts.Minute       => expression.dateAddMinute(amount, date),
                DbFunc.DateParts.Second       => expression.dateAddSecond(amount, date),
                DbFunc.DateParts.Millisecond  => expression.dateAddMillisecond(amount, date),
                _                             => null
            };
        }
    }
}

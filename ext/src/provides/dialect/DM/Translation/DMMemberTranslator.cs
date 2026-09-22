using System;
using System.Globalization;
using System.Linq.Expressions;
using mooSQL.linq;
using mooSQL.linq.Common;
using mooSQL.linq.SqlQuery;
using mooSQL.linq.Linq.Translation;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦 LINQ 成员翻译（Oracle 兼容 EXTRACT/INTERVAL/TO_TIMESTAMP）。
    /// </summary>
    public class DMMemberTranslator : ProviderMemberTranslatorDefault
    {
        class SqlTypesTranslation : SqlTypesTranslationDefault
        {
            protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
                => MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(19, 4));

            protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
                => MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(10, 4));

            protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
            {
                if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
                    return null;

                return MakeSqlTypeExpression(translationContext, methodCall, typeof(string),
                    t => t.WithLength(length).WithDataType(DataFam.VarChar)
                        .WithDbType($"VarChar({length.ToString(CultureInfo.InvariantCulture)})"));
            }
        }

        public class DateFunctionsTranslator : DateFunctionsTranslatorBase
        {
            protected override IExpWord? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, DbFunc.DateParts datepart)
            {
                var factory = translationContext.ExpressionFactory;
                var intDataType = factory.GetDbDataType(typeof(int));
                var dataTimeType = factory.GetDbDataType(dateTimeExpression);

                string? partStr = null;
                string? extractStr = null;

                switch (datepart)
                {
                    case DbFunc.DateParts.Year: extractStr = "YEAR"; break;
                    case DbFunc.DateParts.Quarter: partStr = "Q"; break;
                    case DbFunc.DateParts.Month: extractStr = "MONTH"; break;
                    case DbFunc.DateParts.DayOfYear: partStr = "DDD"; break;
                    case DbFunc.DateParts.Day: extractStr = "DAY"; break;
                    case DbFunc.DateParts.Week: partStr = "WW"; break;
                    case DbFunc.DateParts.WeekDay:
                    {
                        var weekDayFunc = factory.Mod(
                            factory.Increment(
                                factory.Sub(intDataType,
                                    factory.Function(dataTimeType, "TRUNC", dateTimeExpression),
                                    factory.Function(dataTimeType, "TRUNC", dateTimeExpression, factory.Value("IW"))
                                )
                            ),
                            factory.Value(7));
                        return factory.Increment(weekDayFunc);
                    }
                    case DbFunc.DateParts.Hour: extractStr = "HOUR"; break;
                    case DbFunc.DateParts.Minute: extractStr = "MINUTE"; break;
                    case DbFunc.DateParts.Second: extractStr = "SECOND"; break;
                    case DbFunc.DateParts.Millisecond: partStr = "FF"; break;
                    default:
                        return null;
                }

                IExpWord resultExpression;
                if (extractStr != null)
                {
                    resultExpression = factory.Function(intDataType, "EXTRACT",
                        factory.Fragment(intDataType, extractStr + " FROM {0}", dateTimeExpression));
                }
                else
                {
                    resultExpression = factory.Function(intDataType, "TO_NUMBER",
                        factory.Function(dataTimeType, "TO_CHAR", dateTimeExpression, factory.Value(partStr)));

                    if (datepart == DbFunc.DateParts.Millisecond)
                        resultExpression = factory.Div(intDataType, resultExpression, factory.Value(intDataType, 1000));
                }

                return resultExpression;
            }

            protected override IExpWord? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment,
                DbFunc.DateParts datepart)
            {
                var factory = translationContext.ExpressionFactory;
                var dateType = factory.GetDbDataType(dateTimeExpression);
                var intervalType = factory.GetDbDataType(increment).WithDataType(DataFam.Interval);

                string expStr;
                switch (datepart)
                {
                    case DbFunc.DateParts.Year: expStr = "INTERVAL '1' YEAR"; break;
                    case DbFunc.DateParts.Quarter: expStr = "INTERVAL '3' MONTH"; break;
                    case DbFunc.DateParts.Month: expStr = "INTERVAL '1' MONTH"; break;
                    case DbFunc.DateParts.DayOfYear:
                    case DbFunc.DateParts.WeekDay:
                    case DbFunc.DateParts.Day: expStr = "INTERVAL '1' DAY"; break;
                    case DbFunc.DateParts.Week: expStr = "INTERVAL '7' DAY"; break;
                    case DbFunc.DateParts.Hour: expStr = "INTERVAL '1' HOUR"; break;
                    case DbFunc.DateParts.Minute: expStr = "INTERVAL '1' MINUTE"; break;
                    case DbFunc.DateParts.Second: expStr = "INTERVAL '1' SECOND"; break;
                    case DbFunc.DateParts.Millisecond: expStr = "INTERVAL '0.001' SECOND"; break;
                    default:
                        return null;
                }

                var intervalExpression = factory.Multiply(intervalType, increment, factory.Fragment(intervalType, expStr));
                return factory.Add(dateType, dateTimeExpression, intervalExpression);
            }

            protected override IExpWord? TranslateMakeDateTime(
                ITranslationContext translationContext,
                DbDataType resulType,
                IExpWord year,
                IExpWord month,
                IExpWord day,
                IExpWord? hour,
                IExpWord? minute,
                IExpWord? second,
                IExpWord? millisecond)
            {
                var factory = translationContext.ExpressionFactory;
                var stringDataType = factory.GetDbDataType(typeof(string));
                var intDataType = factory.GetDbDataType(typeof(int));

                IExpWord CastToLength(IExpWord expression, int stringLength)
                    => factory.Cast(expression, stringDataType.WithLength(stringLength));

                IExpWord PartExpression(IExpWord expression, int padSize)
                {
                    if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
                        return factory.Value(stringDataType, intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0'));

                    return factory.Function(stringDataType, "LPad",
                        CastToLength(expression, padSize),
                        factory.Value(intDataType, padSize),
                        factory.Value(stringDataType, "0"));
                }

                var yearString = PartExpression(year, 4);
                var monthString = PartExpression(month, 2);
                var dayString = PartExpression(day, 2);

                hour ??= factory.Value(intDataType, 0);
                minute ??= factory.Value(intDataType, 0);
                second ??= factory.Value(intDataType, 0);
                millisecond ??= factory.Value(intDataType, 0);

                var resultExpression = factory.Concat(
                    yearString, factory.Value(stringDataType, "-"),
                    monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
                    PartExpression(hour, 2), factory.Value(stringDataType, ":"),
                    PartExpression(minute, 2), factory.Value(stringDataType, ":"),
                    PartExpression(second, 2), factory.Value(stringDataType, "."),
                    PartExpression(millisecond, 3)
                );

                return factory.Function(resulType, "TO_TIMESTAMP", resultExpression, factory.Value(stringDataType, "YYYY-MM-DD HH24:MI:SS.FF3"));
            }

            protected override IExpWord? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
            {
                var factory = translationContext.ExpressionFactory;
                var dateType = factory.GetDbDataType(dateExpression);
                return factory.Function(dateType.WithDataType(DataFam.Time), "TO_CHAR", dateExpression, factory.Value("HH24:MI:SS"));
            }

            protected override IExpWord? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
                => translationContext.ExpressionFactory.Function(translationContext.GetDbDataType(dateExpression), "TRUNC", dateExpression);
        }

        protected class DMMathMemberTranslator : MathMemberTranslatorBase
        {
            protected override IExpWord? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
            {
                var factory = translationContext.ExpressionFactory;
                return factory.Function(factory.GetDbDataType(xValue), "GREATEST", xValue, yValue);
            }

            protected override IExpWord? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
            {
                var factory = translationContext.ExpressionFactory;
                return factory.Function(factory.GetDbDataType(xValue), "LEAST", xValue, yValue);
            }
        }

        protected override IMemberTranslator CreateSqlTypesTranslator()
            => new SqlTypesTranslation();

        protected override IMemberTranslator CreateDateMemberTranslator()
            => new DateFunctionsTranslator();

        protected override IMemberTranslator CreateMathMemberTranslator()
            => new DMMathMemberTranslator();

        protected override IExpWord? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
        {
            var factory = translationContext.ExpressionFactory;
            return factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Sys_Guid");
        }
    }
}

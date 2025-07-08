using System;
using System.Globalization;
using System.Linq.Expressions;

namespace mooSQL.linq.DataProvider.SqlServer.Translation
{
	using Common;
	using Linq.Translation;

	using mooSQL.data;
	using mooSQL.data.model;

	using SqlQuery;

	public class SqlServerMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		public class SqlServerDateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			public static string? DatePartToStr(Sql.DateParts part)
			{
				return part switch
				{
					Sql.DateParts.Year => "year",
					Sql.DateParts.Quarter => "quarter",
					Sql.DateParts.Month => "month",
					Sql.DateParts.DayOfYear => "dayofyear",
					Sql.DateParts.Day => "day",
					Sql.DateParts.Week => "week",
					Sql.DateParts.WeekDay => "weekday",
					Sql.DateParts.Hour => "hour",
					Sql.DateParts.Minute => "minute",
					Sql.DateParts.Second => "second",
					Sql.DateParts.Millisecond => "millisecond",
					_ => null
				};
			}

			protected override IExpWord? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, Sql.DateParts datepart)
			{
				var partStr = DatePartToStr(datepart);

				if (partStr == null)
					return null;

				var factory   = translationContext.ExpressionFactory;
				var intDbType = factory.GetDbDataType(typeof(int));

				var resultExpression = factory.Function(intDbType, "DatePart", factory.Fragment(intDbType, partStr), dateTimeExpression);

				return resultExpression;
			}

			protected override IExpWord? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override IExpWord? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(dateTimeExpression);

				var partStr = DatePartToStr(datepart);

				if (partStr == null)
				{
					return null;
				}

				var resultExpression = factory.Function(dateType, "DateAdd", factory.Fragment(factory.GetDbDataType(typeof(string)), partStr), increment, dateTimeExpression);
				return resultExpression;
			}

			protected override IExpWord? TranslateDateTimeOffsetDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment, Sql.DateParts datepart)
			{
				return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
			}

			protected override IExpWord? TranslateMakeDateTime(
				ITranslationContext translationContext,
				DbDataType          resulType,
                IExpWord      year,
                IExpWord      month,
                IExpWord      day,
                IExpWord?     hour,
                IExpWord?     minute,
                IExpWord?     second,
                IExpWord?     millisecond)
			{
				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.VarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

                IExpWord CastToLength(IExpWord expression, int stringLength)
				{
					return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

                IExpWord PartExpression(IExpWord expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
						return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
					}

					return factory.Function(stringDataType, "RIGHT",
						factory.Concat(factory.Value(stringDataType, "0"), CastToLength(expression, padSize)),
						factory.Value(intDataType, padSize));
				}

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				if (hour != null || minute != null || second != null || millisecond != null)
				{
					hour        ??= factory.Value(intDataType, 0);
					minute      ??= factory.Value(intDataType, 0);
					second      ??= factory.Value(intDataType, 0);
					millisecond ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						resultExpression,
						factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2), factory.Value(stringDataType, "."),
						PartExpression(millisecond, 3)
					);
				}

				resultExpression = factory.Cast(resultExpression, resulType);

				return resultExpression;
			}

			protected override IExpWord? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), true);

				return cast;
			}

			protected override IExpWord? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				return TranslateDateTimeTruncationToDate(translationContext, dateExpression, translationFlags);
			}

			protected override IExpWord? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Fragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			}

			protected override IExpWord? TranslateDateOnlyDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment, Sql.DateParts datepart)
			{
				return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
			}

			protected override IExpWord? TranslateDateOnlyDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServerDateFunctionsTranslator();
		}

		protected override IExpWord? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "NewID");

			return timePart;
		}
	}
}

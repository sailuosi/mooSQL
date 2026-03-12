using System;
using System.Globalization;
using System.Linq.Expressions;

namespace mooSQL.linq.DataProvider.MySql.Translation
{
	using Common;
	using SqlQuery;
	using Linq.Translation;
	using mooSQL.data.model;
	using mooSQL.data;

	public class MySqlMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Boolean));

			protected override Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertFloat(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(29, 10));

			protected override Expression? ConvertReal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Decimal).WithPrecisionScale(29, 10));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));

			protected override Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.Char));
			}

			protected override Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Char));

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.Char));
			}

			protected override Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(string));

				dbDataType = dbDataType.WithDataType(DataFam.Char);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
			}
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override IExpWord? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, Sql.DateParts datepart)
			{
				var factory     = translationContext.ExpressionFactory;
				var intDataType = factory.GetDbDataType(typeof(int));

				string partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year:    partStr = "year"; break;
					case Sql.DateParts.Quarter: partStr = "quarter"; break;
					case Sql.DateParts.Month:   partStr = "month"; break;
					case Sql.DateParts.DayOfYear:
					{
						return factory.Function(intDataType, "DayOfYear", dateTimeExpression);
					}
					case Sql.DateParts.Day:  partStr = "day"; break;
					case Sql.DateParts.Week: partStr = "week"; break;
					case Sql.DateParts.WeekDay:
					{
						var addDaysFunc = factory.Function(factory.GetDbDataType(dateTimeExpression), "Date_Add", dateTimeExpression,
							factory.Fragment(intDataType, "interval {0} day", factory.Value(intDataType, 1)));

						var weekDayFunc = factory.Function(intDataType, "WeekDay", addDaysFunc);

						return factory.Increment(weekDayFunc);
					}
					case Sql.DateParts.Hour:        partStr = "hour"; break;
					case Sql.DateParts.Minute:      partStr = "minute"; break;
					case Sql.DateParts.Second:      partStr = "second"; break;
					case Sql.DateParts.Millisecond:
					{
						// (MICROSECOND(your_datetime_column) DIV 1000) 

						var microsecondFunc = factory.Div(intDataType, factory.Function(intDataType, "Microsecond", dateTimeExpression), 1000);
						return microsecondFunc;
					}
					default:
						return null;
				}

				var extractDbType = intDataType;

				var resultExpression = factory.Function(extractDbType, "Extract", factory.Fragment(intDataType, partStr + " from {0}", dateTimeExpression));

				return resultExpression;
			}

			protected override IExpWord? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment,
				Sql.DateParts                                                       datepart)
			{
				var factory       = translationContext.ExpressionFactory;
				var dateType      = factory.GetDbDataType(dateTimeExpression);
				var intDbType     = factory.GetDbDataType(typeof(int));
				var intervalType  = intDbType.WithDataType(DataFam.Interval);

				string expStr;
				switch (datepart)
				{
					case Sql.DateParts.Year:        expStr = "Interval {0} Year"; break;
					case Sql.DateParts.Quarter:     expStr = "Interval {0} Quarter"; break;
					case Sql.DateParts.Month:       expStr = "Interval {0} Month"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day:         expStr = "Interval {0} Day"; break;
					case Sql.DateParts.Week:        expStr = "Interval {0} Week"; break;
					case Sql.DateParts.Hour:        expStr = "Interval {0} Hour"; break;
					case Sql.DateParts.Minute:      expStr = "Interval {0} Minute"; break;
					case Sql.DateParts.Second:      expStr = "Interval {0} Second"; break;
					case Sql.DateParts.Millisecond: expStr = "Interval {0} Millisecond"; break;
					default:
						return null;
				}

				var resultExpression = factory.Function(dateType, "Date_Add", dateTimeExpression, factory.Fragment(intervalType, expStr, increment));

				return resultExpression;
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
				var stringDataType = factory.GetDbDataType(typeof(string));
				var intDataType    = factory.GetDbDataType(typeof(int));

                IExpWord CastToLength(IExpWord expression, int stringLength)
				{
					return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

                IExpWord PartExpression(IExpWord expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						return factory.Value(stringDataType, intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0'));
					}

					return factory.Function(stringDataType, "LPad",
						CastToLength(expression, padSize),
						factory.Value(intDataType, padSize),
						factory.Value(stringDataType, "0"));
				}

				var yearString  = CastToLength(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				hour        ??= factory.Value(intDataType, 0);
				minute      ??= factory.Value(intDataType, 0);
				second      ??= factory.Value(intDataType, 0);
				millisecond ??= factory.Value(intDataType, 0);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
					PartExpression(hour, 2), factory.Value(stringDataType, ":"),
					PartExpression(minute, 2), factory.Value(stringDataType, ":"),
					PartExpression(second, 2), factory.Value(stringDataType, "."),
					PartExpression(millisecond, 3)
				);

				resultExpression = factory.Function(resulType, "STR_TO_DATE", resultExpression, factory.Value(stringDataType, "%Y-%m-%d %H:%i:%s.%f"));

				return resultExpression;
			}

			protected override IExpWord? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(dateExpression), "Date", dateExpression);
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

		protected override IExpWord? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Uuid");

			return timePart;
		}
	}
}

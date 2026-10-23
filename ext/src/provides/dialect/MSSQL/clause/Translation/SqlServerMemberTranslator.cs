using System;
using System.Globalization;
using System.Linq.Expressions;

namespace mooSQL.data
{
	using mooSQL.linq;
	using mooSQL.linq.utils;
	using mooSQL.linq.translator;
	using mooSQL.data.model;
	using mooSQL.linq.clause;
	/// <summary>
	/// SqlServerMemberTranslator type.
	/// </summary>

	public class SqlServerMemberTranslator : DefaultMemberTranslator
	{
		/// <summary>
		/// SqlTypesTranslation
		/// </summary>
		/// <summary>
		/// SqlTypesTranslation
		/// </summary>
		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
	/// <summary>
	/// ConvertDateTimeOffset
	/// </summary>
	
			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));
		}
		/// <summary>
		/// SqlServerDateFunctionsTranslator
		/// </summary>

		public class SqlServerDateFunctionsTranslator : DateFunctionsTranslatorBase
		{
	/// <summary>
	/// Translates TranslateDateTimeDatePart for this dialect.
	/// </summary>
	
			protected override IExpWord? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, DbFunc.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;
				var intDbType = factory.GetDbDataType(typeof(int));
				var template = DateSqlTemplateResolver.ResolveDatePartFormat(translationContext.DBLive.dialect.expression, datepart);
				if (template == null)
					return null;

				return factory.Fragment(intDbType, template, dateTimeExpression);
			}
/// <summary>
/// Translates TranslateDateTimeOffsetDatePart for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, DbFunc.DateParts datepart)
				=> TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
/// <summary>
/// Translates TranslateDateTimeDateAdd for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment,
				DbFunc.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(dateTimeExpression);
				var template = DateSqlTemplateResolver.ResolveDateAddFormat(translationContext.DBLive.dialect.expression, datepart);
				if (template == null)
					return null;

				return factory.Fragment(dateType, template, increment, dateTimeExpression);
			}
/// <summary>
/// Translates TranslateDateTimeOffsetDateAdd for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateTimeOffsetDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment, DbFunc.DateParts datepart)
				=> TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
/// <summary>
/// Translates TranslateMakeDateTime for this dialect.
/// </summary>
	

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
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataFam.VarChar);
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
/// <summary>
/// Translates TranslateDateTimeTruncationToDate for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataFam.Date), true);

				return cast;
			}
/// <summary>
/// Translates TranslateDateTimeOffsetTruncationToDate for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				return TranslateDateTimeTruncationToDate(translationContext, dateExpression, translationFlags);
			}
/// <summary>
/// Translates TranslateSqlGetDate for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Fragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			}
/// <summary>
/// Translates TranslateDateOnlyDateAdd for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateOnlyDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, IExpWord increment, DbFunc.DateParts datepart)
			{
				return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
			}
/// <summary>
/// Translates TranslateDateOnlyDatePart for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateDateOnlyDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, IExpWord dateTimeExpression, DbFunc.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}
		}
/// <summary>
/// Creates the SQL types translator.
/// </summary>
	

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}
/// <summary>
/// Creates the date/time member translator.
/// </summary>
	

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServerDateFunctionsTranslator();
		}
/// <summary>
/// Translates Guid.NewGuid() to dialect SQL.
/// </summary>
	

		protected override IExpWord? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "NewID");

			return timePart;
		}
	}
}

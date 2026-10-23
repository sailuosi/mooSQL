using System.Linq.Expressions;

namespace mooSQL.data
{
	using mooSQL.linq;
	using mooSQL.linq.utils;
	using mooSQL.linq.translator;
	using mooSQL.data.model;
	using mooSQL.linq.clause;
	/// <summary>
	/// SqlServer2005MemberTranslator type.
	/// </summary>

	public class SqlServer2005MemberTranslator : SqlServerMemberTranslator
	{
		class SqlTypes2005Translation : SqlTypesTranslation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));
		}

		class DateFunctionsTranslator2005 : SqlServerDateFunctionsTranslator
		{
			protected override IExpWord? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, IExpWord dateExpression, TranslationFlags translationFlags)
			{
				// DATEADD(dd, DATEDIFF(dd, 0, YourDateTimeColumn), 0)

				var factory = translationContext.ExpressionFactory;

				var intDataType = factory.GetDbDataType(typeof(int));
				var dateType = factory.GetDbDataType(dateExpression);

				var datePart = factory.Fragment(DbDataType.Undefined, "dd");
				var dateDiff = factory.Function(intDataType, "DateDiff", datePart, factory.Value(intDataType, 0), dateExpression);
				var dateAdd  = factory.Function(dateType, "DateAdd", datePart, dateDiff, factory.Value(intDataType, 0));

				return dateAdd;
			}
		}
/// <summary>
/// Creates the SQL types translator.
/// </summary>
	

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypes2005Translation();
		}
/// <summary>
/// Creates the date/time member translator.
/// </summary>
	

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator2005();
		}
	}
}

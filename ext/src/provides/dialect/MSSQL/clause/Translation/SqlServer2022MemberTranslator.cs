using System.Linq.Expressions;

namespace mooSQL.data
{
	using mooSQL.linq;
	using mooSQL.linq.translator;
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// SqlServer2022MemberTranslator type.
	/// </summary>

	public class SqlServer2022MemberTranslator : SqlServer2012MemberTranslator
	{
		/// <summary>
		/// SqlServer2022MathMemberTranslator
		/// </summary>
		/// <summary>
		/// SqlServer2022MathMemberTranslator
		/// </summary>
		protected class SqlServer2022MathMemberTranslator : MathMemberTranslatorBase
		{
	/// <summary>
	/// Translates TranslateMaxMethod for this dialect.
	/// </summary>
	
			protected override IExpWord? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "GREATEST", xValue, yValue);
			}
/// <summary>
/// Translates TranslateMinMethod for this dialect.
/// </summary>
	

			protected override IExpWord? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "LEAST", xValue, yValue);
			}
		}
/// <summary>
/// Creates the math member translator.
/// </summary>
	

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlServer2022MathMemberTranslator();
		}
	}
}

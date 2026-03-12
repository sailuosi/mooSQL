using System.Linq.Expressions;

namespace mooSQL.linq.DataProvider.SqlServer.Translation
{
	using Linq.Translation;
    using mooSQL.data.model;
    using SqlQuery;

	public class SqlServer2022MemberTranslator : SqlServer2012MemberTranslator
	{
		protected class SqlServer2022MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override IExpWord? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "GREATEST", xValue, yValue);
			}

			protected override IExpWord? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, IExpWord xValue, IExpWord yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "LEAST", xValue, yValue);
			}
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlServer2022MathMemberTranslator();
		}
	}
}

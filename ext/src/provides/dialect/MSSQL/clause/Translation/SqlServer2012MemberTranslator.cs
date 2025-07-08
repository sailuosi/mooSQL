namespace mooSQL.linq.DataProvider.SqlServer.Translation
{
	using Common;
	using Linq.Translation;

	using mooSQL.data.model;

	using SqlQuery;

	public class SqlServer2012MemberTranslator : SqlServerMemberTranslator
	{
		public class SqlServer2012DateFunctionsTranslator : SqlServerDateFunctionsTranslator
		{
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
				var factory     = translationContext.ExpressionFactory;
				var intDataType = factory.GetDbDataType(typeof(int));

				hour        ??= factory.Value(intDataType, 0);
				minute      ??= factory.Value(intDataType, 0);
				second      ??= factory.Value(intDataType, 0);
				millisecond ??= factory.Value(intDataType, 0);

				var resultExpression = factory.Function(resulType, "DATETIMEFROMPARTS", year, month, day, hour, minute, second, millisecond);

				return resultExpression;
			}
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2012DateFunctionsTranslator();
		}
	}
}

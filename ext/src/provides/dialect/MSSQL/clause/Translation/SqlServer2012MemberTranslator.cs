namespace mooSQL.data
{
	using mooSQL.linq;
	using mooSQL.linq.utils;
	using mooSQL.linq.translator;
	using mooSQL.data.model;
	using mooSQL.linq.clause;
	/// <summary>
	/// SqlServer2012MemberTranslator type.
	/// </summary>

	public class SqlServer2012MemberTranslator : SqlServerMemberTranslator
	{
		/// <summary>
		/// SqlServer2012DateFunctionsTranslator
		/// </summary>
		/// <summary>
		/// SqlServer2012DateFunctionsTranslator
		/// </summary>
		public class SqlServer2012DateFunctionsTranslator : SqlServerDateFunctionsTranslator
		{
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
/// <summary>
/// Creates the date/time member translator.
/// </summary>
	

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2012DateFunctionsTranslator();
		}
	}
}

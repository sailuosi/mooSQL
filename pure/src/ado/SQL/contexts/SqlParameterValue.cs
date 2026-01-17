using System.Diagnostics;

namespace mooSQL.data.model
{
	using Common;

	[DebuggerDisplay("{ProviderValue}, {DbDataType}")]
	public class SQLParameterValue
	{
		public SQLParameterValue(object? providerValue, DbDataType dbDataType)
		{
			ProviderValue = providerValue;
			DbDataType    = dbDataType;
		}

		public object?    ProviderValue { get; }
		public DbDataType DbDataType    { get; }
	}
}

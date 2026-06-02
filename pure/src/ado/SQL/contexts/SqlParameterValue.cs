using System.Diagnostics;

namespace mooSQL.data.model
{
	using Common;

	[DebuggerDisplay("{ProviderValue}, {DbDataType}")]
	/// <summary>
	/// 类型 SQLParameterValue。
	/// </summary>
	public class SQLParameterValue
	{
		/// <summary>
		/// 初始化 SQLParameterValue（构造）。
		/// </summary>
		public SQLParameterValue(object? providerValue, DbDataType dbDataType)
		{
			ProviderValue = providerValue;
			DbDataType    = dbDataType;
		}

		/// <summary>
		/// 属性 ProviderValue（object?）。
		/// </summary>
		public object?    ProviderValue { get; }
		/// <summary>
		/// 属性 DbDataType（DbDataType）。
		/// </summary>
		public DbDataType DbDataType    { get; }
	}
}
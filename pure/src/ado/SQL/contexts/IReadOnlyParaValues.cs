using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	/// <summary>
	/// 接口 IReadOnlyParaValues。
	/// </summary>
	public interface IReadOnlyParaValues
	{
		/// <summary>
		/// 内部成员说明。
		/// </summary>
		bool TryGetValue(ParameterWord parameter, [NotNullWhen(true)] out SQLParameterValue? value);


	}
}
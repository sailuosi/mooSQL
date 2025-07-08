using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	public interface IReadOnlyParaValues
	{
		bool TryGetValue(ParameterWord parameter, [NotNullWhen(true)] out SQLParameterValue? value);


	}
}

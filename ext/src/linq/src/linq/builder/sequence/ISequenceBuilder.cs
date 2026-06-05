using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	interface ISequenceBuilder
	{
		BuildSequenceResult BuildSequence(ClauseSqlTranslator builder, BuildInfo buildInfo);
		bool                IsSequence   (ClauseSqlTranslator builder, BuildInfo buildInfo);
	}
}

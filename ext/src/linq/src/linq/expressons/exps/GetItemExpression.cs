using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	using Linq.Builder;
	using Mapping;
    using mooSQL.data;
    using Reflection;

	sealed class GetItemExpression : Expression
	{
		public GetItemExpression(Expression expression, DBInstance mappingSchema)
		{
			Expression = expression;
			_type       = EagerLoading.GetEnumerableElementType(expression.Type, mappingSchema);
		}

		readonly Type       _type;

		public          Expression     Expression { get; }
		public override Type           Type       => _type;
		public override ExpressionType NodeType   => ExpressionType.Extension;
		public override bool           CanReduce  => true;

		public override Expression Reduce()
		{
			var gi = Methods.Enumerable.First.MakeGenericMethod(_type);

			return Call(null, gi, Expression);
		}
	}
}

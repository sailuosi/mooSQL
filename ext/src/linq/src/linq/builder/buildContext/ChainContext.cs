using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	using Common.Internal;
	using mooSQL.linq.Expressions;
    using mooSQL.utils;

	internal sealed class ChainContext : SequenceContextBase
	{
		public ChainContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall)
			: base(parent, sequence, null)
		{
			MethodCall = methodCall;
			_returnType     = methodCall.Method.ReturnType;
			_methodName     = methodCall.Method.Name;

			if (_returnType.IsGenericType && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				_returnType = _returnType.GetGenericArguments()[0];
				_methodName = _methodName.Replace("Async", "");
			}
		}

		readonly string _methodName;
		readonly Type   _returnType;

		public SqlPlaceholderExpression Placeholder = null!;
		public MethodCallExpression     MethodCall { get; }

		static int CheckNullValue(bool isNull, object context)
		{
			if (isNull)
			{
				throw new InvalidOperationException(
					$"Function '{context}' returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
			}

			return 0;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
				return path;

			if (flags.IsAggregationRoot() || flags.IsAssociationRoot())
			{
				var corrected = SequenceHelper.CorrectExpression(path, this, Sequence);
				return corrected;
			}

			if (Placeholder == null)
				return path;

			Expression result = Placeholder;

			if (!_returnType.IsReferType() && flags.IsExpression())
			{
				result = Expression.Block(
					Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
						new SqlReaderIsNullExpression(Placeholder, false), Expression.Constant(_methodName)),
					Placeholder);
			}

			return result;
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return null;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ChainContext(null, context.CloneContext(Sequence), context.CloneExpression(MethodCall))
			{
				Placeholder = context.CloneExpression(Placeholder)
			};
		}
	}
}

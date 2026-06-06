using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	sealed class OfTypeContext : PassThroughContext
	{
		public Type EntityType { get; }

		public OfTypeContext(IClauseContext context, Type entityType)
			: base(context)
		{
			EntityType = entityType;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			var corrected = base.BuildProjection(path, flags);

			var noConvert = corrected.UnwrapConvert();

			if (SequenceHelper.IsSameContext(path, this)
			    && EntityType != noConvert.Type
			    && noConvert is SqlGenericConstructorExpression { ConstructType: SqlGenericConstructorExpression.CreateType.Full })
			{
				corrected = Builder.BuildFullEntityExpression(DB, path, EntityType, flags);
			}

			return corrected;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new OfTypeContext(context.CloneContext(Context), EntityType);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.linq.Expressions;
    using mooSQL.utils;
    using SqlQuery;

	[BuildsMethodCall("OfType")]
	sealed class OfTypeBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			if (sequence is TableBuilder.TableContext table
				&& table.InheritanceMapping.Count > 0)
			{
				var objectType = methodCall.Type.GetGenericArguments()[0];

				if (table.ObjectType.IsSameOrParentOf(objectType))
				{
					if (!buildInfo.IsTest)
					{
						var predicate = builder.MakeIsPredicate(table, objectType);

						if (predicate.GetType() != typeof(Expr))
							sequence.SelectQuery.Where.EnsureConjunction().Add(predicate);
					}

					return BuildSequenceResult.FromContext(new OfTypeContext(sequence, objectType));
				}
			}
			else
			{
				var toType   = methodCall.Type.GetGenericArguments()[0];
				var gargs    = methodCall.Arguments[0].Type.GetGenericArguments(typeof(IEnumerable<>));
				var fromType = gargs == null ? typeof(object) : gargs[0];

				if (toType.IsSubclassOf(fromType))
				{
					for (var type = toType.BaseType; type != null && type != typeof(object); type = type.BaseType)
					{
						var en= builder.DBLive.client.EntityCash.getEntityInfo(type);

						if (en !=null)
						{
							if (!buildInfo.IsTest)
							{
								var predicate = MakeIsPredicate(builder, sequence, fromType, toType);

								sequence.SelectQuery.Where.EnsureConjunction().Add(predicate);
							}

							return BuildSequenceResult.FromContext(new OfTypeContext(sequence, toType));
						}
					}
				}
			}

			return BuildSequenceResult.FromContext(sequence);
		}

		static IAffirmWord MakeIsPredicate(ExpressionBuilder builder, IBuildContext context, Type fromType, Type toType)
		{
			var en = builder.DBLive.client.EntityCash.getEntityInfo(fromType);

			var table          = new TableWord(en);


			return builder.MakeIsPredicate((context, table), context, null, toType,
				static (context, name) =>
				{
					var field  = context.table.FindFieldByMemberName(name) ?? throw new LinqException($"Field {name} not found in table {context.table}");
					var member = field.ColumnDescriptor.PropertyInfo;

					var contextRef = new ContextRefExpression(member.DeclaringType!, context.context);
					var expr       = Expression.MakeMemberAccess(contextRef, member);
					var sql        = context.context.Builder.ConvertToSql(contextRef.BuildContext, expr);

					return sql;
				});
		}

		#region OfTypeContext

		sealed class OfTypeContext : PassThroughContext
		{
			public Type EntityType { get; }

			public OfTypeContext(IBuildContext context, Type entityType)
				: base(context)
			{
				EntityType = entityType;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				var corrected = base.MakeExpression(path, flags);

				var noConvert = corrected.UnwrapConvert();

				if (SequenceHelper.IsSameContext(path, this)
				    && EntityType != noConvert.Type
				    && noConvert is SqlGenericConstructorExpression { ConstructType: SqlGenericConstructorExpression.CreateType.Full })
				{
					corrected = Builder.BuildFullEntityExpression(DB, path, EntityType, flags);
				}

				return corrected;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new OfTypeContext(context.CloneContext(Context), EntityType);
			}
		}

		#endregion
	}
}

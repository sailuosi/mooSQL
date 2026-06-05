using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.data;
    using mooSQL.data.mapping;

    partial class ExpressionBuilder
	{
		bool IsAssociationInRealization(Expression? expression, MemberInfo member, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			if (InternalExtensions.IsAssociation(member, DBLive))
			{
				associationMember = member;
				return true;
			}

			if (expression?.Type.IsInterface == true)
			{
				if (expression is ContextRefExpression contextRef && contextRef.BuildContext.ElementType != expression.Type)
				{
					var newMember = contextRef.BuildContext.ElementType.GetMemberEx(member);
					if (newMember != null)
					{
						if (InternalExtensions.IsAssociation(newMember, DBLive))
						{
							associationMember = newMember;
							return true;
						}
					}
				}
			}

			associationMember = null;
			return false;
		}

		public bool IsAssociation(Expression expression, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			switch (expression)
			{
				case MemberExpression memberExpression:
					return IsAssociationInRealization(memberExpression.Expression, memberExpression.Member, out associationMember);

				case MethodCallExpression methodCall:
					return IsAssociationInRealization(methodCall.Object, methodCall.Method, out associationMember);

				default:
					associationMember = null;
					return false;
			}
		}

		AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
		{
			memberInfo = null;

			Type objectType;
			if (expression is MemberExpression memberExpression)
			{
				if (!IsAssociationInRealization(memberExpression.Expression, memberExpression.Member,
					    out var associationMember))
					return null;

				var type = associationMember.ReflectedType ?? associationMember.DeclaringType;
				if (type == null)
					return null;
				objectType = type;
			}
			else if (expression is MethodCallExpression methodCall)
			{
				if (!IsAssociationInRealization(methodCall.Object, methodCall.Method, out var associationMember))
					return null;

				var type = methodCall.Method.IsStatic ? methodCall.Arguments[0].Type : associationMember.DeclaringType;
				if (type == null)
					return null;
				objectType = type;
			}
			else
				return null;

			if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
				memberInfo = new AccessorMember(expression);

			if (memberInfo == null)
				return null;

			var entityInfo = DBLive.client.EntityCash.getEntityInfo(objectType);
			if (entityInfo == null)
				return null;

			var descriptor = GetAssociationDescriptor(memberInfo, entityInfo);
			if (descriptor == null && !onlyCurrent && memberInfo.MemberInfo.DeclaringType != entityInfo.Type)
			{
				var declaringEntity = DBLive.client.EntityCash.getEntityInfo(memberInfo.MemberInfo.DeclaringType!);
				if (declaringEntity != null)
					descriptor = GetAssociationDescriptor(memberInfo, declaringEntity);
			}

			return descriptor;
		}

		AssociationDescriptor? GetAssociationDescriptor(AccessorMember accessorMember, EntityInfo entityInfo)
		{
			var member = accessorMember.MemberInfo;

			if (member.MemberType == MemberTypes.Method)
			{
				var attribute = member.GetAttribute<AssociationAttribute>();
				if (attribute != null)
					return CreateAssociationDescriptor(entityInfo.Type, member, attribute);
			}
			else if (member.MemberType is MemberTypes.Property or MemberTypes.Field)
			{
				var attribute = member.GetAttribute<AssociationAttribute>();
				if (attribute != null)
					return CreateAssociationDescriptor(entityInfo.Type, member, attribute);

				return TryInferAssociationDescriptor(entityInfo, member);
			}

			return null;
		}

		static AssociationDescriptor CreateAssociationDescriptor(Type entityType, MemberInfo member, AssociationAttribute attribute)
		{
			return new AssociationDescriptor(
				entityType,
				member,
				attribute.GetThisKeys(),
				attribute.GetOtherKeys(),
				attribute.ExpressionPredicate,
				attribute.Predicate,
				attribute.QueryExpressionMethod,
				attribute.QueryExpression,
				attribute.Storage,
				attribute.AssociationSetterExpressionMethod,
				attribute.AssociationSetterExpression,
				attribute.ConfiguredCanBeNull,
				attribute.AliasName);
		}

		AssociationDescriptor? TryInferAssociationDescriptor(EntityInfo entityInfo, MemberInfo member)
		{
			var memberType = member.GetMemberType();
			if (memberType.IsValueType)
				return null;

			if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType) && memberType != typeof(string))
			{
				var childType = TypeHelper.GetEnumerableElementType(memberType);
				var childEntity = DBLive.client.EntityCash.getEntityInfo(childType);
				if (childEntity == null)
					return null;

				foreach (var col in childEntity.Columns)
				{
					if (!col.IsFK || col.thatTable != entityInfo.DbTableName)
						continue;

					var parentKey = entityInfo.GetPK().FirstOrDefault()?.PropertyName ?? col.thatField;
					return new AssociationDescriptor(
						entityInfo.Type,
						member,
						new[] { parentKey },
						new[] { col.PropertyName },
						null, null, null, null, null, null, null, true, null);
				}
			}
			else
			{
				var targetEntity = DBLive.client.EntityCash.getEntityInfo(memberType);
				if (targetEntity == null)
					return null;

				foreach (var col in entityInfo.Columns)
				{
					if (!col.IsFK || col.thatTable != targetEntity.DbTableName)
						continue;

					return new AssociationDescriptor(
						entityInfo.Type,
						member,
						new[] { col.PropertyName },
						new[] { col.thatField },
						null, null, null, null, null, null, null, true, null);
				}
			}

			return null;
		}

		Dictionary<SqlCacheKey, Expression>? _associations;

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, IBuildContext? forContext, ProjectFlags flags)
		{
			var associationDescriptor = GetAssociationDescriptor(expression, out var memberInfo);

			if (associationDescriptor == null || memberInfo == null)
				return expression;

			var associationRoot = (ContextRefExpression)MakeExpression(rootContext.BuildContext, rootContext, flags.AssociationRootFlag());

			_associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

			var cacheFlags = flags.RootFlag() & ~(ProjectFlags.Subquery | ProjectFlags.ExtractProjection | ProjectFlags.ForceOuterAssociation);
			var key        = new SqlCacheKey(expression, associationRoot.BuildContext, null, null, cacheFlags);

			if (_associations.TryGetValue(key, out var associationExpression))
				return associationExpression;

			LoadWithInfo? loadWith     = null;
			MemberInfo[]? loadWithPath = null;

			var   prevIsOuter = flags.HasFlag(ProjectFlags.ForceOuterAssociation);
			bool? isOptional  = prevIsOuter ? true : null;

			if (rootContext.BuildContext.IsOptional)
				isOptional = true;

			var table = SequenceHelper.GetTableOrCteContext(rootContext.BuildContext);

			if (table != null)
			{
				loadWith     = table.LoadWithRoot;
				loadWithPath = table.LoadWithPath;
				if (table.IsOptional)
					isOptional = true;
			}

			if (forContext?.IsOptional == true)
				isOptional = true;

			if (!associationDescriptor.IsList)
				isOptional = isOptional == true || associationDescriptor.CanBeNull;

			Expression? notNullCheck = null;
			if (associationDescriptor.IsList && (prevIsOuter || flags.IsSubquery()) && !flags.IsExtractProjection())
			{
				var keys = MakeExpression(forContext, rootContext, flags.SqlFlag().KeyFlag());
				if (forContext != null)
					notNullCheck = ExtractNotNullCheck(forContext, keys, flags.SqlFlag());
			}

			var association = AssociationHelper.BuildAssociationQuery(this, rootContext, memberInfo,
				associationDescriptor, notNullCheck, !associationDescriptor.IsList, loadWith, loadWithPath, ref isOptional);

			associationExpression = association;

			if (!associationDescriptor.IsList && !flags.IsSubquery() && !flags.IsExtractProjection())
			{
				var buildInfo = new BuildInfo(forContext, association, new SelectQueryClause())
				{
					IsTest = flags.IsTest(),
					SourceCardinality = isOptional == true ? SourceCardinality.ZeroOrOne : SourceCardinality.One,
					IsAssociation = true
				};

				var sequence = BuildSequence(buildInfo);

				if (!flags.IsTest())
				{
					if (!IsSupportedSubquery(rootContext.BuildContext, sequence, out var errorMessage))
						return new SqlErrorExpression(null, expression, errorMessage, expression.Type, true);
				}

				var alias = associationDescriptor.GenerateAlias();
				var applySupported = DBLive.dialect.Option.ProviderFlags.IsApplyJoinSupported;

				if (applySupported)
				{
					sequence.SetAlias(alias);
				}
				else if (sequence is FirstSingleBuilder.FirstSingleContext firstSingle)
				{
					firstSingle.JoinAlias = alias;
				}

				if (forContext != null)
					sequence = new ScopeContext(sequence, forContext);

				associationExpression = new ContextRefExpression(association.Type, sequence);
			}
			else
			{
				associationExpression = SqlAdjustTypeExpression.AdjustType(associationExpression, expression.Type, DBLive);
			}

			if (!flags.IsExtractProjection())
				_associations[key] = associationExpression;

			return associationExpression;
		}

		Expression? ExtractNotNullCheck(IBuildContext context, Expression expr, ProjectFlags flags)
		{
			SqlPlaceholderExpression? notNull = null;

			if (expr is SqlPlaceholderExpression placeholder)
				notNull = placeholder.MakeNullable();

			if (notNull == null)
			{
				List<Expression> expressions = new();
				if (!CollectNullCompareExpressions(context, expr, expressions) || expressions.Count == 0)
					return null;

				List<SqlPlaceholderExpression> placeholders = new(expressions.Count);

				foreach (var expression in expressions)
				{
					var predicateExpr = ConvertToSqlExpr(context, expression, flags.SqlFlag());
					if (predicateExpr is SqlPlaceholderExpression current)
						placeholders.Add(current);
				}

				notNull = placeholders
					.FirstOrDefault(pl => !pl.Sql.CanBeNullable(NullabilityContext.NonQuery));
			}

			if (notNull == null)
				return null;

			var notNullPath = notNull.Path;

			if (notNullPath.Type.IsValueType && !notNullPath.Type.IsNullable())
				notNullPath = Expression.Convert(notNullPath, typeof(Nullable<>).MakeGenericType(notNullPath.Type));

			return Expression.NotEqual(notNullPath, Expression.Constant(null, notNullPath.Type));
		}

		public static Expression AdjustType(Expression expression, Type desiredType, DBInstance mappingSchema)
		{
			if (desiredType.IsSameOrParentOf(expression.Type))
				return expression;

			if (typeof(IGrouping<,>).IsSameOrParentOf(desiredType))
			{
				if (typeof(IEnumerable<>).IsSameOrParentOf(expression.Type))
					return expression;
			}

			var elementType = TypeHelper.GetEnumerableElementType(desiredType);

			var result = (Expression?)null;

			if (desiredType.IsArray)
			{
				var method = typeof(IQueryable<>).IsSameOrParentOf(expression.Type)
					? Methods.Queryable.ToArray
					: Methods.Enumerable.ToArray;

				result = Expression.Call(method.MakeGenericMethod(elementType),
					expression);
			}
			else if (typeof(IOrderedEnumerable<>).IsSameOrParentOf(desiredType))
			{
				result = expression;
			}
			else if (!typeof(IQueryable<>).IsSameOrParentOf(desiredType) && !desiredType.IsArray)
			{
				var convertExpr = TypeConverterUtil.GetConvertType( mappingSchema,
					typeof(IEnumerable<>).MakeGenericType(elementType), desiredType);
				if (convertExpr != null)
					result = convertExpr.GetBody(expression);
			}

			if (result == null)
			{
				result = expression;
				if (result.Type == typeof(object))
				{
					result = Expression.Convert(result, typeof(IEnumerable<>).MakeGenericType(elementType));
				}

				if (!typeof(IQueryable<>).IsSameOrParentOf(result.Type))
				{
					result = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
						result);
				}

				if (typeof(ITable<>).IsSameOrParentOf(desiredType))
				{
					var tableType = typeof(PersistentTable<>).MakeGenericType(elementType);
					result = Expression.New(tableType.GetConstructor(new[] { result.Type })!,
						result);
				}

				if (result.Type != desiredType)
					result = Expression.Convert(result, desiredType);

			}

			return result;
		}

		public bool GetAssociationTransformation(IBuildContext buildContext, [NotNullWhen(true)] out Expression? transformation)
		{
			transformation = null;
			if (_associations == null)
				return false;

			foreach (var pair in _associations)
			{
				if (pair.Value is ContextRefExpression contextRef && contextRef.BuildContext == buildContext)
				{
					transformation = pair.Key.Expression!;
					return true;
				}
			}

			return false;
		}
	}
}

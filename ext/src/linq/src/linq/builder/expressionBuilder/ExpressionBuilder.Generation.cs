using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Data;
	using mooSQL.linq.Expressions;
	using Mapping;
	using static Data.EntityConstructorBase;
    using mooSQL.data;

    internal partial class ExpressionBuilder
	{
		EntityConstructor? _entityConstructor;

		internal class EntityConstructor : EntityConstructorBase
		{
			public ExpressionBuilder Builder { get; }

			public EntityConstructor(ExpressionBuilder builder)
			{
				Builder = builder;
			}

			public override List<LoadWithInfo>? GetTableLoadWith(Expression path)
			{
				var unwrapped = path.UnwrapConvert();
				var table     = SequenceHelper.GetTableOrCteContext(Builder, unwrapped);

				if (table == null)
					return null;

				return Builder.GetTableLoadWith(table);
			}

			public override Expression? TryConstructFullEntity(SqlGenericConstructorExpression constructorExpression, Type constructType, ProjectFlags flags, bool checkInheritance, out string? error)
			{
				var constructed = base.TryConstructFullEntity(constructorExpression, constructType, flags, checkInheritance, out error);

				if (constructed != null)
				{
					if (constructorExpression.ConstructionRoot != null)
					{
						var tableContext = SequenceHelper.GetTableContext(Builder, constructorExpression.ConstructionRoot); 
						if (tableContext != null)
							constructed = NotifyEntityCreated(constructed, tableContext.SqlTable);
					}
				}

				return constructed;
			}
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(DBInstance mappingSchema, Expression refExpression, Type entityType, ProjectFlags flags, FullEntityPurpose purpose = FullEntityPurpose.Default)
		{
			_entityConstructor ??= new EntityConstructor(this);

			var generic = _entityConstructor.BuildFullEntityExpression( mappingSchema, refExpression, entityType, flags, purpose);

			return generic;
		}

		public SqlGenericConstructorExpression BuildEntityExpression(DBInstance mappingSchema,
			Expression refExpression, Type entityType, IReadOnlyCollection<MemberInfo> members)
		{
			_entityConstructor ??= new EntityConstructor(this);

			var generic = _entityConstructor.BuildEntityExpression( mappingSchema, refExpression, entityType, members);

			return generic;
		}

		public Expression? TryConstruct(DBInstance mappingSchema, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags)
		{
			mappingSchema      =   constructorExpression.DBLive ?? mappingSchema;
			_entityConstructor ??= new EntityConstructor(this);

			var result = _entityConstructor.TryConstruct(mappingSchema,  constructorExpression, flags, out var error);

			return result;
		}

		public Expression Construct(DBInstance mappingSchema, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags)
		{
			mappingSchema =   constructorExpression.DBLive ?? mappingSchema;
			_entityConstructor ??= new EntityConstructor(this);

			var constructed = _entityConstructor.Construct(mappingSchema,  constructorExpression, flags);

			return constructed;
		}

		public SqlGenericConstructorExpression RemapToNewPath(Expression prefixPath, SqlGenericConstructorExpression constructorExpression, Expression currentPath)
		{
			//TODO: only assignments
			var newAssignments = new List<SqlGenericConstructorExpression.Assignment>();

			foreach (var assignment in constructorExpression.Assignments)
			{
				Expression newAssignmentExpression;

				var memberAccess = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);

				if (assignment.Expression is SqlGenericConstructorExpression generic)
				{
					newAssignmentExpression = RemapToNewPath(prefixPath, generic, memberAccess);
				}
				else
				{
					newAssignmentExpression = memberAccess;
				}

				newAssignments.Add(new SqlGenericConstructorExpression.Assignment(assignment.MemberInfo,
					newAssignmentExpression, assignment.IsMandatory, assignment.IsLoaded));
			}

			return new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto,
				constructorExpression.ObjectType, null, newAssignments.AsReadOnly(), constructorExpression.DBLive, currentPath);
		}
	}
}

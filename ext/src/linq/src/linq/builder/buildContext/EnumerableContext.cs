using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Data;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.data;

    //TODO: review
    sealed class EnumerableContext : BuildContextBase
	{

		readonly bool _filedsDefined;
		public EntityInfo Entity { get; private set; }
		public override Expression?    Expression    { get; }

		public ValuesTableWord Table         { get; }

		public EnumerableContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQueryClause query, Type elementType)
			: base(builder, elementType, query)
		{
			Parent            = buildInfo.Parent;

			Entity = builder.DBLive.client.EntityCash.getEntityInfo(elementType);
			Table      = BuildValuesTable(buildInfo.Expression, out _filedsDefined);
			Expression = buildInfo.Expression;

			if (!buildInfo.IsTest)
				SelectQuery.From.FindTableSrc(Table);
		}

		EnumerableContext(ExpressionBuilder builder, DBInstance mappingSchema, Expression expression, SelectQueryClause query, ValuesTableWord table, Type elementType)
			: base(builder, elementType, query)
		{
			Parent = null;
            Entity = builder.DBLive.client.EntityCash.getEntityInfo(elementType);
            Table      = table;
			Expression = expression;
		}

        ValuesTableWord BuildValuesTable(Expression expr, out bool fieldsDefined)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
			{
				fieldsDefined = true;
				return BuildValuesTableFromArray((NewArrayExpression)expr);
			}

			var param = Builder.ParametersContext.BuildParameter(this, expr, null, forceConstant : true,
				buildParameterType : ParametersContext.BuildParameterType.InPredicate);

			if (param == null)
			{
				throw new InvalidOperationException($"Expression '{expr}' not translated to parameter.");
			}

			fieldsDefined = false;
			return new ValuesTableWord(param.SqlParameter);
		}

        ValuesTableWord BuildValuesTableFromArray(NewArrayExpression arrayExpression)
		{

			//if (MappingSchema.IsScalarType(ElementType))
			//{
			//	var rows  = arrayExpression.Expressions.Select(e => new[] {Builder.ConvertToSqlEn(Parent, e)}).ToList();
			//	var contextRef = new ContextRefExpression(ElementType, this);
			//	var specialProp = SequenceHelper.CreateSpecialProperty(contextRef, ElementType, "item");
			//	var field = new FieldWord(Table, "item") { Type = new DbDataType(ElementType) };
			//	return new ValuesTableWord(new[] { field }, new[] { specialProp.Member }, rows);
			//}

			var knownMembers = new HashSet<MemberInfo>();

			foreach (var row in arrayExpression.Expressions)
			{
				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(members, row);

				knownMembers.AddRange(members.Keys);
			}


			var en = DB.client.EntityCash.getEntityInfo(ElementType);

			var builtRows = new List<IExpWord[]>(arrayExpression.Expressions.Count);

			var columnsInfo = knownMembers
				.Select(m => (Member: m, Column: en.Columns.FirstOrDefault(c => c.PropertyInfo == m)))
				.ToList();

			foreach (var row in arrayExpression.Expressions)
			{
				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(members, row);

				var rowValues = new IExpWord[columnsInfo.Count];

				var idx = 0;
				foreach (var (member, column) in columnsInfo)
				{
                    IExpWord sql;
					if (members.TryGetValue(member, out var accessExpr))
					{
						sql = Builder.ConvertToSqlEn(Parent, accessExpr, columnDescriptor: column);
					}
					else
					{
						var nullValue = Expression.Constant(DB.dialect.mapping.GetDefaultValue(ElementType), ElementType);
						sql = Builder.ConvertToSqlEn(Parent, nullValue, columnDescriptor: column);
					}

					rowValues[idx] = sql;
					++idx;
				}

				builtRows.Add(rowValues);
			}

			var fields = new FieldWord[columnsInfo.Count];

			for (var index = 0; index < columnsInfo.Count; index++)
			{
				var (member, column) = columnsInfo[index];
				var field = new FieldWord(new DbDataType(member.GetMemberType()), $"item{index + 1}", true);
				fields[index]        = field;
			}

			return new ValuesTableWord(fields, columnsInfo.Select(ci => ci.Member).ToArray(), builtRows);
		}

		static ConstructorInfo _parameterConstructor =
			MemberHelper.ConstructorOf(() => new ParameterWord(new DbDataType(typeof(object)), "", null));

		static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new ValueWord(new DbDataType(typeof(object)), null));

        FieldWord? GetField(MemberExpression path)
		{
			if (SequenceHelper.IsSpecialProperty(path, ElementType, "item"))
			{
				var newField = BuildField(null, path);
				return newField;
			}

			foreach (var column in Entity.Columns)
			{
				if (!column.PropertyInfo.EqualsTo(path.Member, ElementType))
				{
					continue;
				}

				var newField = BuildField(column, path);
				return newField;
			}

			return null;
		}

        FieldWord BuildField(EntityColumn? column, MemberExpression me)
		{
			var memberName = me.Member.Name;
			if (!Table.FieldsLookup!.TryGetValue(me.Member, out var newField))
			{
				//var getter = column?.GetDbParamLambda();
				//if (getter == null)
				//{
				//	var thisParam = Expression.Parameter(me.Type, memberName);
				//	getter = Expression.Lambda(thisParam, thisParam);
				//}

				//var dbDataType = column?.GetDbDataType(true);

				//var generator = new ExpressionGenerator();
				//if (typeof(DataParameter).IsSameOrParentOf(getter.Body.Type))
				//{
				//	var variable  = generator.AssignToVariable(getter.Body);
				//	generator.AddExpression(
				//		Expression.New(
				//			_parameterConstructor,
				//			Expression.Property(variable, Methods.LinqToDB.DataParameter.DbDataType),
				//			Expression.Constant(memberName),
				//			Expression.Property(variable, Methods.LinqToDB.DataParameter.Value)
				//		));
				//}
				//else
				//{
				//	generator.AddExpression(Expression.New(_sqlValueconstructor,
				//		Expression.Constant(dbDataType),
				//		Expression.Convert(getter.Body, typeof(object))));
				//}

				//var param = Expression.Parameter(typeof(object), "e");

				//var body = generator.Build();
				//body = body.Replace(getter.Parameters[0], Expression.Convert(param, ElementType));

				//var getterLambda = Expression.Lambda<Func<object, IExpWord>>(body, param);
				//var getterFunc   = getterLambda.Compile();

                //Table.Add(newField = new FieldWord(dbDataType, memberName, column?.CanBeNull ?? true), me.Member, getterFunc);
			}

			return newField;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot() || flags.IsTable())
					return path;

				//if (MappingSchema.IsScalarType(ElementType))
				//{
				//	if (path.Type != ElementType)
				//	{
				//		path = ((ContextRefExpression)path).WithType(ElementType);
				//	}

				//	var specialProp = SequenceHelper.CreateSpecialProperty(path, ElementType, "item");
				//	return Builder.MakeExpression(this, specialProp, flags);
				//}

				if (Table.FieldsLookup == null)
					throw new InvalidOperationException("Enumerable fields are not defined.");

				Expression result;
				if (_filedsDefined)
				{
					var membersOrdered =
						from f in Table.Fields
						join fm in Table.FieldsLookup on f equals fm.Value
						select fm.Key;

					result = Builder.BuildEntityExpression(DB, path, ElementType, membersOrdered.ToList());

				}
				else
				{
					result = Builder.BuildFullEntityExpression(DB, path, ElementType, flags);
				}

				return result;
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(this, path);

			var sql = GetField(member);
			if (sql == null)
				return ExpressionBuilder.CreateSqlError(this, path);

			var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path);

			return placeholder;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new EnumerableContext(Builder, DB, Expression!, context.CloneElement(SelectQuery),
				context.CloneElement(Table), ElementType);
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
			{
				var sqlColumn = SelectQuery.Select.Columns[0];
				if (sqlColumn.RawAlias == null)
					sqlColumn.Alias = alias;
			}
		}

		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	using Tools;
	using mooSQL.data.model;
    using mooSQL.data.Mapping;
    using mooSQL.utils;
    using mooSQL.data;

    partial class TableBuilder
	{
		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		public class TableContext : BuildContextBase, ITableContext
		{
			#region Properties

			public DBInstance DBLive {  get; set; }
			public override Expression?   Expression    { get; }
			public override Type          ElementType   => ObjectType;

			public          Type             OriginalType;


            public EntityInfo Entity { get; set; }

            public Type          ObjectType    { get; set; }
			public TableWord      SqlTable      { get; set; }
			public LoadWithInfo  LoadWithRoot  { get; set; } = new();
			public MemberInfo[]? LoadWithPath  { get; set; }

			public bool IsSubQuery { get; }

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder,  BuildInfo buildInfo, Type originalType) : base (builder, originalType, buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				SelectQuery    = buildInfo.SelectQuery;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				DB = builder.DBLive;
				OriginalType     = originalType;
				ObjectType       = GetObjectType();

				Entity = DB.client.EntityCash.getEntityInfo(ObjectType);
				SqlTable         = new TableWord(Entity);

				if (!buildInfo.IsTest || buildInfo.IsSubQuery)
					SelectQuery.From.AddOrGetTable(SqlTable,null);

                    //SelectQuery.From.FindTableSrc(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, DBInstance mappingSchema, BuildInfo buildInfo, TableWord table) : base (builder, table.ObjectType, buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				SelectQuery    = buildInfo.SelectQuery;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				DBLive = mappingSchema;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
                Entity = DB.client.EntityCash.getEntityInfo(ObjectType);

                if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.FindTableSrc(SqlTable);

				Init(true);
			}

			internal TableContext(ExpressionBuilder builder, DBInstance mappingSchema, SelectQueryClause selectQuery, TableWord table, bool isOptional) : base(builder, table.ObjectType, selectQuery)
			{
				Parent         = null;
				Expression     = null;
				IsSubQuery     = false;
				IsOptional     = isOptional;
				DBLive = mappingSchema;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
                Entity = DB.client.EntityCash.getEntityInfo(ObjectType);

                if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.FindTableSrc(SqlTable);

				Init(true);
			}



            public TableContext(ExpressionBuilder builder, DBInstance mappingSchema, BuildInfo buildInfo) : base (builder, typeof(object), buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				DBLive = mappingSchema;

				var mc   = (MethodCallExpression)buildInfo.Expression;
				//var attr = mc.Method.GetTableFunctionAttribute(mappingSchema);

				//if (attr == null)
				//	throw new LinqException($"Method '{mc.Method}' has no '{nameof(Sql.TableFunctionAttribute)}'.");

				if (!typeof(IQueryable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return IQueryable<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
                Entity = DB.client.EntityCash.getEntityInfo(ObjectType);
                SqlTable         = new TableWord(Entity);

				SelectQuery.From.FindTableSrc(SqlTable);

				//attr.SetTable(builder.DataOptions, (context: this, builder),  mappingSchema, SqlTable, mc, static (context, a, _, inline) =>
				//{
				//	if (context.builder.CanBeCompiled(a, false))
				//	{
				//		var param = context.builder.ParametersContext.BuildParameter(context.context, a, columnDescriptor : null, forceConstant : true, doNotCheckCompatibility : true);
				//		if (param != null)
				//		{
				//			if (inline == true)
				//			{
				//				param.SqlParameter.IsQueryParameter = false;
				//			}
				//			return new SqlPlaceholderExpression(null, param.SqlParameter, a);
				//		}
				//	}
				//	return context.builder.ConvertToSqlExpr(context.context, a);
				//});

				builder.RegisterExtensionAccessors(mc);

				Init(true);
			}

			protected Type GetObjectType()
			{
				for (var type = OriginalType.BaseType; type != null && type != typeof(object); type = type.BaseType)
				{
					var en = DB.client.EntityCash.getEntityInfo(type);
					if (en.Columns.Count > 0)
						return type;
				}

				return OriginalType;
			}

			public IReadOnlyList<EntiyInherit> InheritanceMapping = null!;

			protected void Init(bool applyFilters)
			{
				//InheritanceMapping = EntityDescriptor.InheritanceMapping;

				// Original table is a parent.
				//
				if (applyFilters && ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);

					if (predicate.GetType() != typeof(mooSQL.data.model.affirms.Expr))
                        SelectQuery.Where.EnsureConjunction().Add(predicate);
				}
			}

			#endregion

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsAssociationRoot() || flags.IsExtractProjection() || flags.IsAggregationRoot())
					return path;

				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsTable())
						return path;

					// Eager load case
					if (path.Type.IsEnumerableType(ElementType))
					{
						return path;
					}

					//if (MappingSchema.IsScalarType(ElementType))
					//{
					//	var tablePlaceholder =
					//		ExpressionBuilder.CreatePlaceholder(this, SqlTable, path, trackingPath : path);
					//	return tablePlaceholder;
					//}

					Expression fullEntity = Builder.BuildFullEntityExpression(DB, path, ElementType, flags);
					// Entity can contain calculated columns which should be exposed
					fullEntity = Builder.ConvertExpressionTree(fullEntity);

					if (fullEntity.Type != path.Type)
						fullEntity = Expression.Convert(fullEntity, path.Type);

					return fullEntity;
				}

				Expression member;

				if (path is MemberExpression me)
					member = me;
				else
					return path;

				var sql = GetField(member, false);

				if (sql != null)
				{
					if (flags.HasFlag(ProjectFlags.Table))
					{
						var root = Builder.GetRootContext(this, path, false);
						return root ?? path;
					}
				}

				if (sql == null)
				{
					return path;
				}

				var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path, trackingPath : path);

				return placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TableContext(Builder, DBLive, context.CloneElement(SelectQuery), context.CloneElement(SqlTable), IsOptional);
			}

			public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override bool IsOptional { get; }

			#region GetContext

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.CreateSubQuery || buildInfo.IsTest)
					return this;

				var expr = Builder.GetSequenceExpression(this);
				if (expr == null)
					return this;

				var context = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

				return context;
			}

			public override BaseSentence GetResultStatement()
			{
				return new SelectSentence(SelectQuery);
			}

			#endregion

			#region SetAlias

			public override void SetAlias(string? alias)
			{
				if (alias == null)
					return;

				if (SqlTable.Alias != null)
					return;

				if (!alias.Contains('<'))
					SqlTable.Alias = alias;
			}

			#endregion

			#region Helpers

			protected IExpWord? GetField(Expression expression, bool throwException)
			{
				if (expression.NodeType == ExpressionType.MemberAccess)
				{
					var memberExpression = (MemberExpression)expression;
					var col = Entity.GetColumn(memberExpression.Member);

					if (col != null)
					{
								
						var expr = memberExpression.Expression!;

						if (col.PropertyInfo.DeclaringType != memberExpression.Member.DeclaringType)
							expr = Expression.Convert(memberExpression.Expression!, col.PropertyInfo.DeclaringType!);

						expression = memberExpression = ExpressionHelper.PropertyOrField(expr, col.PropertyName);
					}
						
					

					var levelExpression = expression;

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						if (levelExpression != expression)
						{
							var levelMember = (MemberExpression)levelExpression;

							if (memberExpression.Member.IsNullableValueMember() && memberExpression.Expression == levelExpression)
								memberExpression = levelMember;
							else
							{
								var sameType =
									levelMember.Member.ReflectedType == SqlTable.ObjectType ||
									levelMember.Member.DeclaringType == SqlTable.ObjectType;

								if (!sameType)
								{
									var members = SqlTable.ObjectType.GetInstanceMemberEx(levelMember.Member.Name);

									foreach (var mi in members)
									{
										if (mi.DeclaringType == levelMember.Member.DeclaringType)
										{
											sameType = true;
											break;
										}
									}
								}

								if (sameType || InheritanceMapping.Count > 0)
								{
									string? pathName = null;

									foreach (var field in SqlTable.Fields)
									{
										var name = levelMember.Member.Name;
										if (field.Name.IndexOf('.') >= 0)
										{
											if (pathName == null)
											{
												var suffix = string.Empty;
												for (var ex = (MemberExpression)expression;
													ex != levelMember;
													ex = (MemberExpression)ex.Expression!)
												{
													suffix = string.IsNullOrEmpty(suffix)
														? ex.Member.Name
														: ex.Member.Name + "." + suffix;
												}

												pathName = !string.IsNullOrEmpty(suffix) ? name + "." + suffix : name;
											}

											if (field.Name == pathName)
												return field;
										}
										else if (field.Name == name)
											return field;
									}
								}
							}
						}

						if (levelExpression == memberExpression)
						{
							foreach (var field in SqlTable.Fields)
							{
								if (field.ColumnDescriptor.PropertyInfo.EqualsTo(memberExpression.Member, SqlTable.ObjectType))
								{
									if (field.ColumnDescriptor.PropertyInfo.IsComplex()
										&& !field.ColumnDescriptor.PropertyInfo.IsDynamicColumnPropertyEx())
									{
										var name = memberExpression.Member.Name;
										var me   = memberExpression;

										if (me.Expression.UnwrapConvert() is MemberExpression)
										{
											while (me.Expression.UnwrapConvert() is MemberExpression me1)
											{
												me   = me1;
												name = me.Member.Name + '.' + name;
											}

											var fld = SqlTable.FindFieldByMemberName(name);

											if (fld != null)
												return fld;
										}
									}
									else
									{
										if (SequenceHelper.IsSameContext(memberExpression.Expression.UnwrapConvert(), this))
											return field;
									}
								}

								if (InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
								{
									foreach (var mapping in InheritanceMapping)
									{
										var en = DB.client.EntityCash.getEntityInfo(mapping.Type);
										foreach (var mm in en.Columns)
										{
											if (mm.PropertyInfo.EqualsTo(memberExpression.Member))
												return field;
										}
									}
								}

							}

							if (memberExpression.Member.IsDynamicColumnPropertyEx())
							{
								var fieldName = memberExpression.Member.Name;

								// do not add association columns
								var flag = true;



								if (flag)
								{
									var newField = SqlTable.FindFieldByMemberName(fieldName);
									if (newField == null)
									{
										var fied= Entity.GetColumn(fieldName);
										newField = new FieldWord(fied);
                                        newField.IsDynamic = true;

										SqlTable.Add(newField);
									}

									return newField;
								}
							}


						}
					}
				}

				if (throwException)
				{
					throw new LinqException($"Member '{expression}' is not a table column.");
				}
				return null;
			}

			#endregion
		}
	}
}

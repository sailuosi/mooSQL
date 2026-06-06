using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{	using Common;

	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	[BuildsMethodCall("First", "FirstOrDefault", "Single", "SingleOrDefault")]
	[BuildsMethodCall("FirstAsync", "FirstOrDefaultAsync", "SingleAsync", "SingleOrDefaultAsync", 
		CanBuildName = nameof(CanBuildAsyncMethod))]
	static class FirstSingleBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable() && call.Arguments.Count <= 2;

		public static bool CanBuildAsyncMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsAsyncExtension() && call.Arguments.Count <= 3;

		internal static BuildSequenceResult Compile(ClauseSqlTranslator builder, BuildInfo buildInfo)
			=> BuildCore(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);

		public enum MethodKind
		{
			First,
			FirstOrDefault,
			Single,
			SingleOrDefault,
		}

		static MethodKind GetMethodKind(string methodName)
		{
			return methodName switch
			{
				"First"                => MethodKind.First,
				"FirstAsync"           => MethodKind.First,
				"FirstOrDefault"       => MethodKind.FirstOrDefault,
				"FirstOrDefaultAsync"  => MethodKind.FirstOrDefault,
				"Single"               => MethodKind.Single,
				"SingleAsync"          => MethodKind.Single,
				"SingleOrDefault"      => MethodKind.SingleOrDefault,
				"SingleOrDefaultAsync" => MethodKind.SingleOrDefault,
				_ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
			};
		}

		static BuildSequenceResult BuildCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var argument = methodCall.Arguments[0];
			var argumentCount = methodCall.Arguments.Count;

			if (methodCall.IsAsyncExtension())
				--argumentCount;

			var cardinality = buildInfo.SourceCardinality;

			if (buildInfo.SourceCardinality != SourceCardinality.Unknown)
			{
				cardinality &= ~SourceCardinality.Many;
			}

			cardinality |= SourceCardinality.One;
			var methodKind = GetMethodKind(methodCall.Method.Name);

			switch (methodKind)
			{
				case MethodKind.First:
				case MethodKind.Single: 
					break;

				case MethodKind.FirstOrDefault:
				case MethodKind.SingleOrDefault:
				{
					cardinality |= SourceCardinality.Zero;
					break;
				}
			}

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, argument)
			{
				SourceCardinality = cardinality
			});

			if (buildResult.BuildContext == null)
				return BuildSequenceResult.Error(methodCall);

			var sequence = buildResult.BuildContext;

			if (argumentCount > 1)
			{
				var filterLambda = methodCall.Arguments[1].UnwrapLambda();
				sequence = builder.BuildWhere(buildInfo.Parent, sequence, filterLambda, false, false, buildInfo.IsTest);

				if (sequence == null)
					return BuildSequenceResult.Error(methodCall);
			}

			sequence = new SubQueryContext(sequence);

			var take = 0;

			switch (methodKind)
			{
				case MethodKind.First          :
				{
					take        = 1;
					break;
				}
				case MethodKind.FirstOrDefault :
				{
					take        = 1;
					break;
				}
				case MethodKind.Single          :
				case MethodKind.SingleOrDefault :
				{
					// FK 关联 to-one 已由键约束保证 0/1 行，无需 TAKE 2 做 Single 校验；
					// 否则在无窗口函数方言上会阻断 SentenceOptimizerVisitor.OptimizeApply。
					if (!buildInfo.IsSubQuery && !buildInfo.IsAssociation)
					{
						if (buildInfo.SelectQuery.Select.TakeValue is null or ValueWord { Value: >= 2 })
						{
							take = 2;
						}
					}

					break;
				}
			}

			if (take != 0)
			{
				var takeExpression = new ValueWord(take);
				builder.BuildTake(sequence, takeExpression, null);
			}

			var canBeWeak = false;

			if (buildInfo.Parent != null && (cardinality & SourceCardinality.Zero) != 0)
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, null, allowNullField: true, isNullValidationDisabled: false);
				canBeWeak = true;
			}

			var firstSingleContext = new FirstSingleContext(buildInfo.Parent, sequence, methodKind, buildInfo.IsSubQuery, buildInfo.IsAssociation, canBeWeak, cardinality, buildInfo.IsTest);

			return BuildSequenceResult.FromContext(firstSingleContext);
		}

		public sealed class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext? parent, IBuildContext sequence, MethodKind methodKind,
				bool isSubQuery, bool isAssociation, bool canBeWeak, SourceCardinality cardinality, bool isTest)
				: base(parent, sequence, null)
			{
				_methodKind   = methodKind;
				IsSubQuery    = isSubQuery;
				IsAssociation = isAssociation;
				CanBeWeak     = canBeWeak;
				Cardinality   = cardinality;
				IsTest        = isTest;
			}

			readonly MethodKind _methodKind;

			public bool              IsSubQuery    { get; }
			public bool              IsAssociation { get; }
			public bool              CanBeWeak     { get; }
			public bool              IsTest        { get; }
			public SourceCardinality Cardinality   { get; set; }
			/// <summary>不支持 APPLY 时由 TryCreateAssociation 设置，仅用于 JOIN 派生表别名。</summary>
			public string?           JoinAlias      { get; set; }

			public override bool IsOptional => (Cardinality & SourceCardinality.Zero) != 0 || Cardinality == SourceCardinality.Unknown;

			bool _isJoinCreated;
			bool _asSubquery;

			void CreateJoin()
			{
				// sequence created in test mode and there can be no tables.
				//
				if (IsTest)
					return;

				if (_isJoinCreated  || _asSubquery)
					return;

				// process as subquery
				if (Parent!.SelectQuery.From.Tables.Count == 0)
				{
					_asSubquery = true;
					return;
				}

				if (!_isJoinCreated)
				{
					_isJoinCreated = true;

					var applySupported = Parent!.Builder.DBLive.dialect.Option.ProviderFlags.IsApplyJoinSupported;

					if (!applySupported)
					{
						var alias = IsAssociation ? JoinAlias : null;
						if (CanBeWeak)
							Parent.SelectQuery.From.LeftJoin(SelectQuery, alias, null);
						else
							Parent.SelectQuery.From.InnerJoin(SelectQuery, alias, null);
					}
					else if (CanBeWeak)
					{
						Parent.SelectQuery.From.OuterApply(SelectQuery, null, null);
					}
					else
					{
						Parent.SelectQuery.From.CrossApply(SelectQuery, null, null);
					}
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if ((flags.IsAssociationRoot() || flags.IsRoot()) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				if (!flags.IsTest() && IsSubQuery)
				{
					CreateJoin();
				}

				var projected = base.MakeExpression(path, flags);

				if (flags.IsTable())
					return projected;

				if (_asSubquery)
				{
					if (Parent == null)
						return path;

					projected = Builder.BuildSqlExpression(this, projected, ProjectFlags.SQL,
						buildFlags : BuildFlags.ForceAssignments);

					if (projected is SqlPlaceholderExpression placeholder)
					{
						var column = Builder.ToColumns(this, placeholder);
						if (column is SqlPlaceholderExpression)
						{
							projected = ClauseSqlTranslator.CreatePlaceholder(Parent, SelectQuery, path);
						}
						else
						{
							projected = path;
						}
					}
					else
					{
						projected = path;
					}
				}

				return projected;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new FirstSingleContext(null, context.CloneContext(Sequence),
					_methodKind, IsSubQuery, IsAssociation, CanBeWeak, Cardinality, false)
				{
					_isJoinCreated = _isJoinCreated
				};
			}
		}
	}
}

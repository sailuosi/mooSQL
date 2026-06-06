using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;

	enum FirstSingleMethodKind
	{
		First,
		FirstOrDefault,
		Single,
		SingleOrDefault,
	}

	internal sealed class FirstSingleContext : SequenceContextBase
	{
		internal FirstSingleContext(IBuildContext? parent, IBuildContext sequence, FirstSingleMethodKind methodKind,
			bool isSubQuery, bool isAssociation, bool canBeWeak, mooSQL.data.model.SourceCardinality cardinality, bool isTest)
			: base(parent, sequence, null)
		{
			_methodKind   = methodKind;
			IsSubQuery    = isSubQuery;
			IsAssociation = isAssociation;
			CanBeWeak     = canBeWeak;
			Cardinality   = cardinality;
			IsTest        = isTest;
		}

		readonly FirstSingleMethodKind _methodKind;

		public bool              IsSubQuery    { get; }
		public bool              IsAssociation { get; }
		public bool              CanBeWeak     { get; }
		public bool              IsTest        { get; }
		internal mooSQL.data.model.SourceCardinality Cardinality   { get; set; }
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

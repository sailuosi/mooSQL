using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;

	using SqlQuery;
	/// <summary>
	/// 构建环境，包含父环境、表达式、查询SQL等
	/// </summary>
	internal sealed class BuildInfo
	{
		public BuildInfo(IBuildContext? parent, Expression expression, SelectQueryClause selectQuery)
		{
			Parent            = parent;
			Expression        = expression;
			SelectQuery       = selectQuery;
			SourceCardinality = SourceCardinality.Unknown;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression)
			: this(buildInfo.Parent, expression, buildInfo.SelectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression, SelectQueryClause selectQuery)
			: this(buildInfo.Parent, expression, selectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
		}

		public BuildInfo?     SequenceInfo             { get; set; }
		public IBuildContext? Parent                   { get; set; }
		public Expression     Expression               { get; set; }
		public SelectQueryClause    SelectQuery              { get; set; }
		public bool           CopyTable                { get; set; }
		public bool           CreateSubQuery           { get; set; }
		public bool           AssociationsAsSubQueries { get; set; }
		public bool           IsAssociation            { get; set; }
		public JoinKind       JoinType                 { get; set; }
		public bool           IsSubQuery               => Parent != null;

		bool _isAssociationBuilt;
		public bool   IsAssociationBuilt
		{
			get => _isAssociationBuilt;
			set
			{
				_isAssociationBuilt = value;

				if (SequenceInfo != null)
					SequenceInfo.IsAssociationBuilt = value;
			}
		}

		SourceCardinality _sourceCardinality;
		public SourceCardinality SourceCardinality
		{
			get
			{
				if (SequenceInfo == null)
					return _sourceCardinality;
				var parent = SequenceInfo.SourceCardinality;
				if (parent == SourceCardinality.Unknown)
					return _sourceCardinality;
				return parent;
			}

			set => _sourceCardinality = value;
		}

		bool _isAggregation;

		public bool IsAggregation
		{
			get
			{
				if (_isAggregation || SequenceInfo == null)
					return _isAggregation;
				return SequenceInfo.IsAggregation;
			}

			set => _isAggregation = value;
		}

		bool _isTest;

		public bool IsTest
		{
			get
			{
				if (_isTest || SequenceInfo == null)
					return _isTest;
				return SequenceInfo.IsTest;
			}

			set => _isTest = value;
		}

		public ProjectFlags GetFlags()
		{
			return GetFlags(ProjectFlags.SQL);
		}

		public ProjectFlags GetFlags(ProjectFlags withFlag)
		{
			var flags = withFlag;

			if (IsTest)
				flags |= ProjectFlags.Test;

			return flags;
		}

	}
}

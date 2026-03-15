using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using SqlQuery;
	using Common;
	using mooSQL.linq.Expressions;
	using mooSQL.linq.Mapping;
	using mooSQL.data.model;
    using mooSQL.utils;

	internal class CteContext : BuildContextBase
	{
		public Expression CteExpression { get; set;  }

		public override Expression?   Expression    => CteExpression;


		public IBuildContext?   CteInnerQueryContext { get; private set; }
		public SubQueryContext? SubqueryContext      { get; private set; }
		public CTEClause        CteClause            { get; private set; }

		public CteContext(ExpressionBuilder builder, IBuildContext? cteInnerQueryContext, CTEClause cteClause, Expression cteExpression)
			: this(builder, cteClause.ObjectType, cteInnerQueryContext?.SelectQuery ?? new SelectQueryClause())
		{
			CteInnerQueryContext = cteInnerQueryContext;
			CteClause            = cteClause;
			CteExpression        = cteExpression;
		}

		CteContext(ExpressionBuilder builder, Type objectType, SelectQueryClause selectQuery)
			: base(builder, objectType, selectQuery)
		{
			CteClause     = default!;
			CteExpression = default!;
		}

		Dictionary<Expression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);
		Dictionary<Expression, SqlPlaceholderExpression> _recursiveMap = new (ExpressionEqualityComparer.Instance);

		bool _isRecursiveCall;

		public void InitQuery()
		{
			if (CteInnerQueryContext != null)
				return;

			if (_isRecursiveCall)
				return;

			var saveRecursiveBuild = Builder.IsRecursiveBuild;

			Builder.IsRecursiveBuild = true;

			var cteBuildInfo = new BuildInfo((IBuildContext?)null, Expression!, new SelectQueryClause());

			_isRecursiveCall         = true;

			var cteInnerQueryContext = Builder.BuildSequence(cteBuildInfo);

			CteInnerQueryContext = cteInnerQueryContext;
			CteClause.Body       = cteInnerQueryContext.SelectQuery;
			SelectQuery          = cteInnerQueryContext.SelectQuery;
			SubqueryContext      = new SubQueryContext(cteInnerQueryContext);

			_isRecursiveCall = false;

			if (_recursiveMap.Count > 0)
			{
				var subQueryExpr = new ContextRefExpression(SubqueryContext.ElementType, SubqueryContext);
				var buildFlags = BuildFlags.ForceAssignments;

				var all = Builder.BuildSqlExpression(SubqueryContext, subQueryExpr, ProjectFlags.SQL,
					buildFlags : buildFlags);

				var cteExpr = subQueryExpr.WithContext(this);

				PostProcessExpression(all, cteExpr);
			}

			Builder.IsRecursiveBuild = saveRecursiveBuild;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.ExtractProjection))
				return path;

			if (_isRecursiveCall)
			{
				if (SequenceHelper.IsSameContext(path, this) && _recursiveMap.Count > 0)
				{
					if (_recursiveMap.TryGetValue(path, out var value))
						return value;
					return path;
				}

				if (_knownMap.TryGetValue(path, out var alreadyTranslated))
					return alreadyTranslated;

				if (!_recursiveMap.TryGetValue(path, out var newPlaceholder))
				{
					// For recursive CTE we cannot calculate nullability correctly, so based on path.Type
					var field = new FieldWord(new DbDataType(path.Type), TableLikeHelpers.GenerateColumnAlias(path) ?? "field", path.Type.IsReferType());

					newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQueryClause?)null, field, path, trackingPath: path);
					_recursiveMap[path] = newPlaceholder;
				}

				return newPlaceholder;
			}

			InitQuery();

			if (SubqueryContext == null || CteInnerQueryContext == null)
				throw new InvalidOperationException();

			var subqueryPath  = SequenceHelper.CorrectExpression(path, this, SubqueryContext);
			var correctedPath = subqueryPath;

			if (!ReferenceEquals(subqueryPath, path))
			{
				_isRecursiveCall = true;

				var buildFlags = BuildFlags.ForceAssignments;
				correctedPath = Builder.BuildSqlExpression(SubqueryContext, correctedPath, flags.SqlFlag(), buildFlags: buildFlags);

				_isRecursiveCall = false;

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					var postProcessed = PostProcessExpression(correctedPath, path);

					return postProcessed;
				}
			}

			return correctedPath;
		}

		Expression PostProcessExpression(Expression correctedPath, Expression subqueryPath)
		{
			correctedPath = SequenceHelper.CorrectTrackingPath(Builder, correctedPath, subqueryPath);

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(correctedPath);

			var remapped = TableLikeHelpers.RemapToFields(SubqueryContext!, null, CteClause.Fields, _knownMap, _recursiveMap, correctedPath,
				placeholders);
			return remapped;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var newContext = new CteContext(Builder, ElementType, SelectQuery);

			context.RegisterCloned(this, newContext);

			newContext.SubqueryContext      = context.CloneContext(SubqueryContext);
			newContext.CteInnerQueryContext = context.CloneContext(CteInnerQueryContext);
			newContext.CteClause            = context.CloneElement(CteClause);
			newContext.CteExpression        = context.CloneExpression(CteExpression);

			return newContext;
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			throw new InvalidOperationException();
		}

		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}
	}
}

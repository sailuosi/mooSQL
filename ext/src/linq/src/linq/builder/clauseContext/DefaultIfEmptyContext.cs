using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	internal sealed class DefaultIfEmptyContext : SequenceContextBase
	{
		readonly IClauseContext _nullabilitySequence;
		readonly bool          _allowNullField;

		ReadOnlyCollection<Expression>? _notNullConditions;

		internal DefaultIfEmptyContext(IClauseContext? parent, IClauseContext sequence, IClauseContext nullabilitySequence, Expression? defaultValue, bool allowNullField, bool isNullValidationDisabled)
			: base(parent, sequence, null)
		{
			_nullabilitySequence     = nullabilitySequence;
			_allowNullField          = allowNullField;
			DefaultValue             = defaultValue;
			IsNullValidationDisabled = isNullValidationDisabled;
		}

		public bool        IsNullValidationDisabled { get; set; }
		public Expression? DefaultValue             { get; }

		public const string NotNullPropName = "not_null";

		public ReadOnlyCollection<Expression> GetNotNullConditions()
		{
			if (_notNullConditions == null)
				_notNullConditions = PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, true) ?? throw new InvalidOperationException();
			return _notNullConditions;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
				return path;

			var newPath = SequenceHelper.CorrectExpression(path, this, Sequence);

			if (ExpressionEqualityComparer.Instance.Equals(newPath, path))
				return path;

			if (flags.IsTraverse() || flags.IsRoot() || flags.IsTable() || flags.IsExtractProjection())
				return newPath;

			if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
			{
				var placeholder = ClauseSqlTranslator.CreatePlaceholder(this,
					new NullabilityWord(new ValueWord(1), true),
					path,
					alias : NotNullPropName);

				return placeholder;
			}

			if (!IsNullValidationDisabled && DefaultValue != null)
			{
				var notNullConditions = GetNotNullConditions();

				var sequenceRef = new ContextRefExpression(ElementType, Sequence);

				var testCondition = notNullConditions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);

				var defaultValue = DefaultValue;
				if (defaultValue.Type != sequenceRef.Type)
				{
					defaultValue = Expression.Convert(defaultValue, sequenceRef.Type);
				}

				var body = Expression.Condition(testCondition, sequenceRef, defaultValue);

				var projectedDefault = Builder.Project(Sequence, newPath, null, -1, flags, body, true);
				return projectedDefault;
			}

			var expr = Builder.BuildSqlExpression(this, newPath, flags/*.SqlFlag()*/);

			if (!flags.IsTest())
			{
				expr = Builder.UpdateNesting(this, expr);
			}

			expr = SequenceHelper.CorrectTrackingPath(Builder, expr, path);

			if (!IsNullValidationDisabled && /*!flags.IsKeys() && */expr.UnwrapConvert() is not SqlEagerLoadExpression)
			{
				if (expr is SqlPlaceholderExpression placeholder)
				{
					if (flags.IsExpression())
					{
						var nullablePlaceholder = placeholder.MakeNullable();
						if (path.Type != placeholder.Type)
						{
							return Expression.Condition(
								Expression.NotEqual(nullablePlaceholder, Expression.Default(placeholder.Type)),
								placeholder, Expression.Default(path.Type));
						}
					}

					return placeholder;
				}

				if (_notNullConditions == null)
				{
					_notNullConditions = PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, _allowNullField);
				}

				if (_notNullConditions != null)
				{
					expr = new SqlDefaultIfEmptyExpression(expr, _notNullConditions);
					return expr;
				}
			}

			return expr;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new DefaultIfEmptyContext(null,
				context.CloneContext(Sequence),
				context.CloneContext(_nullabilitySequence),
				context.CloneExpression(DefaultValue),
				_allowNullField,
				IsNullValidationDisabled);
		}

		public override IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
			return Sequence.GetContext(expression, buildInfo);
		}

		public override bool IsOptional => true;

		internal static ReadOnlyCollection<Expression>? PrepareNoNullConditions(ClauseSqlTranslator builder, IClauseContext notNullHandlerSequence, IClauseContext sequence, IClauseContext nullabilitySequence, bool allowNullField)
		{
			var sequenceRef  = new ContextRefExpression(sequence.ElementType, sequence);
			var translated   = builder.BuildSqlExpression(sequence, sequenceRef, ProjectFlags.SQL, buildFlags: BuildFlags.ForceAssignments);
			var placeholders = ClauseSqlTranslator.CollectDistinctPlaceholders(translated);

			var nullability = NullabilityContext.GetContext(nullabilitySequence.SelectQuery);
			var notNull = placeholders
				.Where(p => !p.Sql.CanBeNullable(nullability))
				.Cast<Expression>()
				.ToList();

			if (notNull.Count == 0)
			{
				/*
				if (!allowNullField)
					return null;
					*/

				if (builder.DBLive.dialect.Option.ProviderFlags.IsAccessBuggyLeftJoinConstantNullability)
				{
					if (placeholders.Count == 0)
						return null;

					notNull = placeholders.Cast<Expression>().ToList();
				}
				else
				{
					notNull.Add(SequenceHelper.CreateSpecialProperty(new ContextRefExpression(notNullHandlerSequence.ElementType, notNullHandlerSequence), typeof(int?),
						NotNullPropName));
				}

			}
			else if (notNull.Count > 0)
			{
				notNull.RemoveRange(1, notNull.Count - 1);
			}

			return notNull.AsReadOnly();
		}
	}
}

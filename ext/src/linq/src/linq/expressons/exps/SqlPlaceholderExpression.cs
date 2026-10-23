using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace mooSQL.linq.expressions
{
	using mooSQL.linq.utils;
    using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.linq.clause;
	/// <summary>
	/// SqlPlaceholderExpression type.
	/// </summary>
	/// <summary>
	/// SqlPlaceholderExpression type.
	/// </summary>
	public sealed class SqlPlaceholderExpression : Expression
	{
#if DEBUG
		static int _placeholderCounter;
		/// <summary>
		/// Id
		/// </summary>
		/// <summary>
		/// Gets or sets Id.
		/// </summary>
		public int Id { get; }
#endif
		/// <summary>
		/// SqlPlaceholderExpression type.
		/// </summary>

		public SqlPlaceholderExpression(SelectQueryClause? selectQuery, IExpWord sql, Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			#if DEBUG

			if (sql is ColumnWord column && column.Parent == selectQuery)
				throw new InvalidOperationException();
			if (path is SqlPlaceholderExpression)
				throw new InvalidOperationException();
			if (trackingPath is SqlPlaceholderExpression)
				throw new InvalidOperationException();

			if (null != sql.Find(e => e is SelectQueryClause sc && ReferenceEquals(sc, selectQuery)))
			{
				throw new InvalidOperationException($"Wrong select query.");
			}

			#endif

			SelectQuery  = selectQuery;
			Path         = path;
			ConvertType  = convertType ?? path.Type;
			Alias        = alias;
			Index        = index;
			Sql          = sql;
			TrackingPath = trackingPath;

#if DEBUG
			Id = Interlocked.Increment(ref _placeholderCounter);

			if (Id == 0)
			{

			}
#endif
		}
		/// <summary>
		/// Gets or sets SelectQuery.
		/// </summary>

		public SelectQueryClause?   SelectQuery  { get; }
		/// <summary>
		/// Path
		/// </summary>
		/// <summary>
		/// Gets or sets Path.
		/// </summary>
		public Expression     Path         { get; }
		/// <summary>
		/// TrackingPath
		/// </summary>
		/// <summary>
		/// Gets or sets TrackingPath.
		/// </summary>
		public Expression?    TrackingPath { get; }
		/// <summary>
		/// Index
		/// </summary>
		/// <summary>
		/// Gets or sets Index.
		/// </summary>
		public int?           Index        { get; }
		/// <summary>
		/// Alias
		/// </summary>
		/// <summary>
		/// Gets or sets Alias.
		/// </summary>
		public string?        Alias        { get; set; }
		/// <summary>
		/// Sql
		/// </summary>
		/// <summary>
		/// Gets or sets Sql.
		/// </summary>
		public IExpWord Sql          { get; }
		/// <summary>
		/// ConvertType
		/// </summary>
		/// <summary>
		/// Gets or sets ConvertType.
		/// </summary>
		public Type           ConvertType  { get; }
/// <summary>
/// NodeType
/// </summary>
	

		public override ExpressionType NodeType => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type     => ConvertType ?? Path.Type;

#if DEBUG
		/// <summary>
		/// History
		/// </summary>
		/// <summary>
		/// Gets or sets History.
		/// </summary>
		public List<SqlPlaceholderExpression>? History { get; private set; }

		void AppendHistory(SqlPlaceholderExpression ancestor)
		{
			History ??= new List<SqlPlaceholderExpression>();
			if (ancestor.History != null)
				History.AddRange(ancestor.History);

			History.Add(ancestor);
		}
#endif
		/// <summary>
		/// MakeNullable
		/// </summary>

		public SqlPlaceholderExpression MakeNullable()
		{
			if (!Type.IsReferType())
			{
				var type           = Type.WrapNullable();
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}
		/// <summary>
		/// MakeNotNullable
		/// </summary>

		public SqlPlaceholderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type           = Type.GetGenericArguments()[0];
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}
		/// <summary>
		/// WithPath
		/// </summary>

		public SqlPlaceholderExpression WithPath(Expression path)
		{
			if (ExpressionEqualityComparer.Instance.Equals(path, Path))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}
		/// <summary>
		/// WithSql
		/// </summary>

		public SqlPlaceholderExpression WithSql(IExpWord sqlExpression)
		{
			if (Equals(Sql, sqlExpression))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, sqlExpression, Path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}
		/// <summary>
		/// WithSelectQuery
		/// </summary>

		public SqlPlaceholderExpression WithSelectQuery(SelectQueryClause selectQuery)
		{
			if (ReferenceEquals(SelectQuery, selectQuery))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(selectQuery, Sql, Path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}
		/// <summary>
		/// WithTrackingPath
		/// </summary>

		public SqlPlaceholderExpression WithTrackingPath(Expression trackingPath)
		{
			if (ReferenceEquals(trackingPath, TrackingPath) || ExpressionEqualityComparer.Instance.Equals(trackingPath, TrackingPath))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, Alias, Index, trackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}
		/// <summary>
		/// WithAlias
		/// </summary>

		public SqlPlaceholderExpression WithAlias(string? alias)
		{
			if (Equals(Alias, alias))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			var pathStr = "#" + ExpressionEqualityComparer.Instance.GetHashCode(Path)/* + " " + Path*/;

			var startStr = "SQL";
#if DEBUG
			startStr += $"[ID:{Id}]";
#endif
			string result;
			if (SelectQuery == null)
			{
				if (Sql is ColumnWord column)
				{
					var sourceId = column.Parent!.SourceID;
					result = $"{startStr}[S:{Index}]({sourceId})";
				}
				else
					result = $"{startStr}";
			}
			else
				result = $"{startStr}(S:{SelectQuery.SourceID})";

			var sqlStr = $"{{{Sql}}}";
			if (Sql.CanBeNullable(NullabilityContext.NonQuery) && Sql is not ColumnWord)
				sqlStr += "?";
			result += $": {sqlStr} ({pathStr})";

			/*
			if (TrackingPath != null)
			{
				result += " TP: " + TrackingPath;
			}
			*/

			return result;
		}
		/// <summary>
		/// Equals
		/// </summary>

		public bool Equals(SqlPlaceholderExpression other)
		{
			return Equals(SelectQuery, other.SelectQuery)                       &&
			       ExpressionEqualityComparer.Instance.Equals(Path, other.Path) &&
			       Index       == other.Index                                   &&
			       ConvertType == other.ConvertType;
		}
/// <summary>
/// Equals
/// </summary>
	

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlPlaceholderExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = SelectQuery != null ? SelectQuery.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(Path);
				hashCode = (hashCode * 397) ^ Index.GetHashCode();
				hashCode = (hashCode * 397) ^ ConvertType.GetHashCode();
				return hashCode;
			}
		}
		/// <summary>
		/// WithType
		/// </summary>

		public SqlPlaceholderExpression WithType(Type type)
		{
			if (Type != type)
			{
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlPlaceholderExpression(this);
			return base.Accept(visitor);
		}

	}

}

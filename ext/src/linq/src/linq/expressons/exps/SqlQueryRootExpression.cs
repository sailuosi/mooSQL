using System;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	using mooSQL.linq.utils;
	using mooSQL.linq.mapping;
    using mooSQL.data;
    /// <summary>
    /// SqlQueryRootExpression type.
    /// </summary>

    public class SqlQueryRootExpression : Expression, IEquatable<SqlQueryRootExpression>
	{
		/// <summary>
		/// Gets or sets ContextType.
		/// </summary>

		public Type          ContextType   { get; }
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>

		public DBInstance DBLive {  get; }
		/// <summary>
		/// SqlQueryRootExpression type.
		/// </summary>

		public SqlQueryRootExpression(DBInstance mappingSchema, Type contextType)
		{
			DBLive = mappingSchema;
			ContextType   = contextType;
		}
		/// <summary>
		/// Creates Create.
		/// </summary>

		public static SqlQueryRootExpression Create(DBInstance dataContext)
		{
			return new SqlQueryRootExpression(dataContext, dataContext.GetType());
		}
		/// <summary>
		/// Creates Create.
		/// </summary>

		public static SqlQueryRootExpression Create(DBInstance dataContext, Type contextType)
		{
			return new SqlQueryRootExpression(dataContext, contextType);
		}
/// <summary>
/// ToString
/// </summary>
	



		public override string ToString()
		{
			return $"Context<{ContextType.Name}>(MS:)";
		}
/// <summary>
/// NodeType
/// </summary>
	

		public override ExpressionType NodeType => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type     => ContextType;
		/// <summary>
		/// Equals
		/// </summary>

		public bool Equals(SqlQueryRootExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return  ContextType == other.ContextType;
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

			return Equals((SqlQueryRootExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				return  ContextType.GetHashCode();
			}
		}
		/// <summary>
		/// operator ==
		/// </summary>

		public static bool operator ==(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return Equals(left, right);
		}
		/// <summary>
		/// operator !=
		/// </summary>

		public static bool operator !=(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return !Equals(left, right);
		}
/// <summary>
/// VisitChildren
/// </summary>
	

		protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase expressionVisitorBase)
				return expressionVisitorBase.VisitSqlQueryRootExpression(this);

			return base.Accept(visitor);
		}
	}
}

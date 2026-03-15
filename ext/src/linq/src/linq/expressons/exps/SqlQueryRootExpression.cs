using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	using Common.Internal;
	using Mapping;
    using mooSQL.data;

    public class SqlQueryRootExpression : Expression, IEquatable<SqlQueryRootExpression>
	{

		public Type          ContextType   { get; }

		public DBInstance DBLive {  get; }

		public SqlQueryRootExpression(DBInstance mappingSchema, Type contextType)
		{
			DBLive = mappingSchema;
			ContextType   = contextType;
		}

		public static SqlQueryRootExpression Create(DBInstance dataContext)
		{
			return new SqlQueryRootExpression(dataContext, dataContext.GetType());
		}

		public static SqlQueryRootExpression Create(DBInstance dataContext, Type contextType)
		{
			return new SqlQueryRootExpression(dataContext, contextType);
		}



		public override string ToString()
		{
			return $"Context<{ContextType.Name}>(MS:)";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ContextType;

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

		public override int GetHashCode()
		{
			unchecked
			{
				return  ContextType.GetHashCode();
			}
		}

		public static bool operator ==(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return !Equals(left, right);
		}

		protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase expressionVisitorBase)
				return expressionVisitorBase.VisitSqlQueryRootExpression(this);

			return base.Accept(visitor);
		}
	}
}

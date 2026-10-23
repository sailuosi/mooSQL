using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// SqlPathExpression type.
	/// </summary>
	/// <summary>
	/// SqlPathExpression type.
	/// </summary>
	public class SqlPathExpression : Expression, IEquatable<SqlPathExpression>
	{
		/// <summary>
		/// SqlPathExpression type.
		/// </summary>
		/// <summary>
		/// SqlPathExpression type.
		/// </summary>
		public SqlPathExpression(Expression[] path, Type type)
		{
			Type = type;
			Path = path;
		}
		/// <summary>
		/// Gets or sets Path.
		/// </summary>

		public Expression[] Path { get; set; }
/// <summary>
/// CanReduce
/// </summary>
	

		public override bool           CanReduce => false;
	/// <summary>
	/// NodeType
	/// </summary>
	
		public override ExpressionType NodeType  => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type      { get; }
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return "$Path$->" + string.Join("->", Path.Select(p => p.ToString()));
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
			{
				return baseVisitor.VisitSqlPathExpression(this);
			}

			return base.Accept(visitor);
		}
		/// <summary>
		/// Equals
		/// </summary>

		public bool Equals(SqlPathExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (Type != other.Type)
			{
				return false;
			}

			return Path.SequenceEqual(other.Path, ExpressionEqualityComparer.Instance);
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

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SqlPathExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}
		/// <summary>
		/// operator ==
		/// </summary>

		public static bool operator ==(SqlPathExpression? left, SqlPathExpression? right)
		{
			return Equals(left, right);
		}
		/// <summary>
		/// operator !=
		/// </summary>

		public static bool operator !=(SqlPathExpression? left, SqlPathExpression? right)
		{
			return !Equals(left, right);
		}
		/// <summary>
		/// Applies pending changes for the edit table.
		/// </summary>

		public SqlPathExpression Update(Expression[] path)
		{
			if (ReferenceEquals(Path, path) || path.SequenceEqual(Path, ExpressionEqualityComparer.Instance))
				return this;

			return new SqlPathExpression(path, Type);
		}
		/// <summary>
		/// WithType
		/// </summary>

		public SqlPathExpression WithType(Type type)
		{
			if (Type == type)
				return this;

			return new SqlPathExpression(Path, type);
		}

	}
}

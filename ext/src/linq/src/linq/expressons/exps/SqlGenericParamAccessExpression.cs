using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// SqlGenericParamAccessExpression type.
	/// </summary>
	/// <summary>
	/// SqlGenericParamAccessExpression type.
	/// </summary>
	public class SqlGenericParamAccessExpression : Expression, IEquatable<SqlGenericParamAccessExpression>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <summary>
		/// Gets or sets Constructor.
		/// </summary>
		public Expression    Constructor   { get; }
		/// <summary>
		/// ParameterInfo
		/// </summary>
		/// <summary>
		/// Gets or sets ParameterInfo.
		/// </summary>
		public ParameterInfo ParameterInfo { get; }
		/// <summary>
		/// ParamIndex
		/// </summary>
		/// <summary>
		/// ParamIndex
		/// </summary>
		public int           ParamIndex    => ParameterInfo.Position;
		/// <summary>
		/// ParamType
		/// </summary>
		/// <summary>
		/// ParamType
		/// </summary>
		public Type          ParamType     => ParameterInfo.ParameterType;
		/// <summary>
		/// SqlGenericParamAccessExpression type.
		/// </summary>

		public SqlGenericParamAccessExpression(Expression constructor, ParameterInfo parameterInfo)
		{
			Constructor        = constructor;
			ParameterInfo = parameterInfo;
		}
/// <summary>
/// NodeType
/// </summary>
	

		public override ExpressionType NodeType => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type     => ParamType;
		/// <summary>
		/// Applies pending changes for the edit table.
		/// </summary>

		public SqlGenericParamAccessExpression Update(Expression constructor)
		{
			if (ReferenceEquals(Constructor, constructor))
				return this;
			return new SqlGenericParamAccessExpression(constructor, ParameterInfo);
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return $"{Constructor}[[{ParamIndex}]]";
		}
		/// <summary>
		/// Equals
		/// </summary>

		public bool Equals(SqlGenericParamAccessExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return ExpressionEqualityComparer.Instance.Equals(Constructor, other.Constructor) && ParameterInfo.Equals(other.ParameterInfo);
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

			return Equals((SqlGenericParamAccessExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = ExpressionEqualityComparer.Instance.GetHashCode(Constructor);
				hashCode = (hashCode * 397) ^ ParameterInfo.GetHashCode();
				return hashCode;
			}
		}
		/// <summary>
		/// operator ==
		/// </summary>

		public static bool operator ==(SqlGenericParamAccessExpression? left, SqlGenericParamAccessExpression? right)
		{
			return Equals(left, right);
		}
		/// <summary>
		/// operator !=
		/// </summary>

		public static bool operator !=(SqlGenericParamAccessExpression? left, SqlGenericParamAccessExpression? right)
		{
			return !Equals(left, right);
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlGenericParamAccessExpression(this);
			return base.Accept(visitor);
		}

	}
}

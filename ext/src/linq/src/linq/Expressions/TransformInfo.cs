using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// TransformInfo type.
	/// </summary>
	/// <summary>
	/// TransformInfo struct.
	/// </summary>
	public struct TransformInfo
	{
		/// <summary>
		/// TransformInfo type.
		/// </summary>
		/// <summary>
		/// TransformInfo type.
		/// </summary>
		public TransformInfo(Expression expression, bool stop)
		{
			Expression = expression;
			Stop       = stop;
			Continue   = false;
		}
		/// <summary>
		/// TransformInfo type.
		/// </summary>

		public TransformInfo(Expression expression)
		{
			Expression = expression;
			Stop       = false;
			Continue   = false;
		}
		/// <summary>
		/// TransformInfo type.
		/// </summary>

		public TransformInfo(Expression expression, bool stop, bool @continue)
		{
			Expression = expression;
			Stop       = stop;
			Continue   = @continue;
		}
		/// <summary>
		/// Expression
		/// </summary>

		public Expression Expression;
		/// <summary>
		/// Stop
		/// </summary>
		/// <summary>
		/// Stop
		/// </summary>
		public bool       Stop;
		/// <summary>
		/// Continue
		/// </summary>
		/// <summary>
		/// Continue
		/// </summary>
		public bool       Continue;
	}
}

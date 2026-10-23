using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// ICustomMapper type.
	/// </summary>
	/// <summary>
	/// ICustomMapper interface.
	/// </summary>
	public interface ICustomMapper
	{
		/// <summary>
		/// CanMap
		/// </summary>
		/// <summary>
		/// CanMap
		/// </summary>
		bool CanMap(Expression expression);
		/// <summary>
		/// Map
		/// </summary>
		/// <summary>
		/// Map
		/// </summary>
		Expression Map(Expression expression);
	}
}

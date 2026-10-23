namespace mooSQL.linq.expressions
{
	/// <summary>
	/// IPrintableExpression type.
	/// </summary>
	/// <summary>
	/// IPrintableExpression interface.
	/// </summary>
	public interface IPrintableExpression
	{
		/// <summary>
		///     Creates a printable string representation of the given expression using <see cref="ExpressionPrinter" />.
		/// </summary>
		/// <param name="expressionPrinter">The expression printer to use.</param>
		void Print(ExpressionPrinter expressionPrinter);
	}
}

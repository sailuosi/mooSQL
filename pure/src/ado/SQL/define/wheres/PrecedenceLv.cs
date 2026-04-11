namespace mooSQL.data.model
{
	/// <summary>
	/// SQL 表达式节点优先级常量（数值越大越先结合），用于生成括号与化简。
	/// </summary>
	public sealed class PrecedenceLv
	{
		/// <summary>最高：括号、成员、调用、下标、自增自减、new、typeof 等。</summary>
		public const int Primary            = 100; // (x) x.y f(x) a[x] x++ x-- new typeof sizeof checked unchecked
		/// <summary>一元：正负、逻辑非、前缀自增自减、强制转换。</summary>
		public const int Unary              =  90; // + - ! ++x --x (T)x
		/// <summary>
		/// This precedence is only for SQLite's || concatenate operator: https://www.sqlite.org/lang_expr.html
		/// </summary>
		public const int Concatenate        =  85; // SQLite's ||
		/// <summary>乘除取模。</summary>
		public const int Multiplicative     =  80; // * / %
		/// <summary>减法（与 C# 优先级分层对应）。</summary>
		public const int Subtraction        =  70; // -
		/// <summary>加减。</summary>
		public const int Additive           =  60; // +
		/// <summary>比较与集合：IN、BETWEEN、LIKE、ANY/ALL 等。</summary>
		public const int Comparison         =  50; // ANY ALL SOME EXISTS, IS [NOT], IN, BETWEEN, LIKE, < > <= >=, == !=
		/// <summary>按位异或等。</summary>
		public const int Bitwise            =  40; // ^
		/// <summary>逻辑非 NOT。</summary>
		public const int LogicalNegation    =  30; // NOT
		/// <summary>逻辑与 AND。</summary>
		public const int LogicalConjunction =  20; // AND
		/// <summary>逻辑或 OR。</summary>
		public const int LogicalDisjunction =  10; // OR
		/// <summary>未知或未分类（最低优先级占位）。</summary>
		public const int Unknown            =   0;
	}
}

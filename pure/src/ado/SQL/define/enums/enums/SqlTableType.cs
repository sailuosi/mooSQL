namespace mooSQL.data
{
	/// <summary>
	/// SQL 表类型
	/// </summary>
	public enum SqlTableType
	{
		/// <summary>
		/// 真实表
		/// </summary>
		Table = 0,
		/// <summary>
		/// 系统表
		/// </summary>
		SystemTable,
		/// <summary>
		/// 视图
		/// </summary>
		Function,
		/// <summary>
		/// 表达式，比如：select 1 from dual
		/// </summary>
		Expression,
		/// <summary>
		/// 公用表表达式（Common Table Expression）
		/// </summary>
		Cte,
		/// <summary>
		/// 原生SQL，比如：select * from (select 1 as a) t
		/// </summary>
		RawSql,
		/// <summary>
		/// 合并源，比如：merge into t using (select 1 as a) s
		/// </summary>
		MergeSource,
		/// <summary>
		/// 值表，比如：select * from (values(1)) t
		/// </summary>
		Values
	}
	/// <summary>
	/// 数据库表类型
	/// </summary>
	public enum DBTableType
	{
		/// <summary>
		/// 未定义
		/// </summary>
		None = 0,
		/// <summary>
		/// 实体表
		/// </summary>
		Table =1,
		/// <summary>
		/// 视图
		/// </summary>
		View =2,
		/// <summary>
		/// 临时表
		/// </summary>
		Temp=3,
		/// <summary>
		/// 代表一个复合查询结果，但不代表一个真实的表
		/// </summary>
		Select=4,
		/// <summary>
		/// 虚表
		/// </summary>
		Fake=9
	}
}

using System;

namespace mooSQL.data.model
{
    using mooSQL.data;


    /// <summary>
    /// 类型 SqlGetValue。
    /// </summary>
    public class SqlGetValue
	{
		/// <summary>
		/// 初始化 SqlGetValue（构造）。
		/// </summary>
		public SqlGetValue(IExpWord sql, Type valueType, EntityColumn? columnDescriptor, Func<object, object>? getValueFunc)
		{
			Sql              = sql;
			ValueType        = valueType;
			ColumnDescriptor = columnDescriptor;
			GetValueFunc     = getValueFunc;
		}

		/// <summary>
		/// 属性 Sql（IExpWord）。
		/// </summary>
		public IExpWord        Sql              { get; }
		/// <summary>
		/// 属性 ValueType（Type）。
		/// </summary>
		public Type                  ValueType        { get; }
		/// <summary>
		/// 属性 ColumnDescriptor（EntityColumn?）。
		/// </summary>
		public EntityColumn?     ColumnDescriptor { get; }
		/// <summary>
		/// WithSql 方法（返回 SqlGetValue）。
		/// </summary>
		public Func<object, object>? GetValueFunc     { get; }

		/// <summary>
		/// WithSql 方法（返回 SqlGetValue）。
		/// </summary>
		public SqlGetValue WithSql(IExpWord sql)
		{
			if (ReferenceEquals(sql, Sql))
				return this;

			return new SqlGetValue(sql, ValueType, ColumnDescriptor, GetValueFunc);
		}
	}
}
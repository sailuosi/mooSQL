using System;

namespace mooSQL.data.model
{
    using mooSQL.data;


    public class SqlGetValue
	{
		public SqlGetValue(IExpWord sql, Type valueType, EntityColumn? columnDescriptor, Func<object, object>? getValueFunc)
		{
			Sql              = sql;
			ValueType        = valueType;
			ColumnDescriptor = columnDescriptor;
			GetValueFunc     = getValueFunc;
		}

		public IExpWord        Sql              { get; }
		public Type                  ValueType        { get; }
		public EntityColumn?     ColumnDescriptor { get; }
		public Func<object, object>? GetValueFunc     { get; }

		public SqlGetValue WithSql(IExpWord sql)
		{
			if (ReferenceEquals(sql, Sql))
				return this;

			return new SqlGetValue(sql, ValueType, ColumnDescriptor, GetValueFunc);
		}
	}
}

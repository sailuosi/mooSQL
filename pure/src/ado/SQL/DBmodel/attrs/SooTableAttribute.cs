using System;
using mooSQL.data.Mapping;
using mooSQL.data.model;



namespace mooSQL.data
{
    /// <summary>
    /// 映射数据库表到类或接口
    /// 您可以将其应用于任何类，包括非公共类、嵌套类或抽象类。
    /// 将它应用于接口将允许您对目标表执行查询，但您需要指定
    /// 如果你想从这样的映射中选择数据，那么在你的查询中显式地投影。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
	public class SooTableAttribute : MappingAttribute
	{
		/// <summary>
		/// 创建表映射
		/// </summary>
		public SooTableAttribute()
		{
			IsColumnAttributeRequired = true;
		}

        /// <summary>
        /// 创建表映射
        /// </summary>
        /// <param name="tableName">Name of mapped table or view in database.</param>
        public SooTableAttribute(string tableName) : this()
		{
			Name = tableName;
		}
		/// <summary>
		/// 别名，用于查询时指定别名
		/// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 数据库表名，空缺时使用类名
        /// </summary>
        public string? Name                     { get; set; }

		/// <summary>
		/// 可选架构
		/// </summary>
		public string? Schema                   { get; set; }

		/// <summary>
		/// 数据库名
		/// </summary>
		public string? Database                 { get; set; }

		/// <summary>
		/// 服务器
		/// </summary>
		public string? Server                   { get; set; }
		/// <summary>
		/// 中文名
		/// </summary>
		public string Caption { get; set; }
		/// <summary>
		/// 表类型
		/// </summary>
		public DBTableType Type { get; set; }
		/// <summary>
		/// 表选项，已废弃
		/// </summary>
		public TableOptions TableOptions        { get; set; }

		/// <summary>
		/// 标识列的特性是不是必须贴，是的话，只认可贴表的列。
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   IsColumnAttributeRequired { get; set; }

		/// <summary>
		/// 标识是否动态表名，用于各类分表。
		/// </summary>
		public bool LiveName { get; set; }

		public override string GetObjectID()
		{
#if NETFRAMEWORK
			return string.Format("{0}.{1}.{2}", Name, Type.ToString(), Database);
				//"."+Configuration+ "Name}.{Schema}.{Database}.{Server}.{(Type.ToString())}.{(int)TableOptions}.{(IsColumnAttributeRequired ? '1' : '0')}.{(IsView ? '1' : '0')}.");

#endif
#if NET5_0_OR_GREATER
			return FormattableString.Invariant($".{Configuration}.{Name}.{Schema}.{Database}.{Server}.{(Type.ToString())}");
#endif

		}


	}
}

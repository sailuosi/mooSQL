using mooSQL.data.model;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库表字段定义
    /// </summary>
    public class DbColumnInfo
    {
        /// <summary>
        /// 所属表
        /// </summary>
        public DbTableInfo Table { get; set; }
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 映射到 C# 类型
        /// </summary>
        public Type CsType { get; set; }
        /// <summary>
        /// 数据库枚举类型int值
        /// </summary>
        public DbDataType DbType { get; set; }
        /// <summary>
        /// 类型化的字段类型，如int,varchar,datetime等
        /// </summary>
        public DataFam FieldType { get; set; }
        /// <summary>
        /// 数据库类型，字符串，varchar
        /// </summary>
        public string DbTypeText { get; set; }
        /// <summary>
        /// 数据库类型，字符串，varchar(255)
        /// </summary>
        public string DbTypeTextFull { get; set; }
        /// <summary>
        /// 最大长度
        /// </summary>
        public int MaxLength { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public bool IsPrimary { get; set; }
        /// <summary>
        /// 自增标识
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// 是否可DBNull
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// 数据库默认值
        /// </summary>
        public string DefaultValue { get; set; }
        /// <summary>
        /// 字段位置
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// 数值精度
        /// </summary>
        public int Precision { get; set; }
        /// <summary>
        /// 小数位长度
        /// </summary>
        public int Scale
        {
            get; set;
        }
    }
}

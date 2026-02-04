using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data.builder;
/// <summary>
/// DDL构造中的列信息
/// </summary>
public class DDLField
{
    public string FieldName { get; set; }
    /// <summary>
    /// 完整的类型定义，如varchar(255)
    /// </summary>
    public string TextType { get; set; }

    public string Mode { get; set; }
    /// <summary>
    /// 可空性
    /// </summary>
    public bool Nullable { get; set; }
    /// <summary>
    /// 默认值
    /// </summary>
    public string DefaultValue { get; set; }
    /// <summary>
    /// 中文名
    /// </summary>
    public string Caption { get; set; }
    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimary { get; set; }
    /// <summary>
    /// 是否唯一
    /// </summary>
    public bool Unique {  get; set; }

    /// <summary>
    /// 字段长度
    /// </summary>
    public int Precision { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int Scale { get; set; }
    /// <summary>
    /// 字段类型,如Varchar,int不含长度信息
    /// </summary>
    public string FieldType { get; set; }
    /// <summary>
    /// 格式化的类型信息
    /// </summary>
    public DbDataType DbType { get; set; }
}





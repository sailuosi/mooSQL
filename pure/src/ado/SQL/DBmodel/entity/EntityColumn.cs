
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体字段信息
    /// </summary>
    public class EntityColumn
    {
        /// <summary>
        /// 所属表
        /// </summary>
        public EntityInfo belongTable { get; set; }
        /// <summary>
        /// 字段信息
        /// </summary>
        /// <param name="parent"></param>
        public EntityColumn(EntityInfo parent) { 
            this.belongTable = parent;
            this.DbTableName = parent.DbTableName;
            this.EntityName = parent.EntityName;

        }
        /// <summary>
        /// 属性反射
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }
        /// <summary>
        /// 属性名
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// 数据库字段名
        /// </summary>
        public string DbColumnName { get; set; }
        public string OldDbColumnName { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 中文名
        /// </summary>
        public string ColumnDescription { get; set; }
        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; }
        /// <summary>
        /// 可空
        /// </summary>
        public bool IsNullable { get; set; }
        /// <summary>
        /// 是否自增id
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// 是否主键
        /// </summary>
        public bool IsPrimarykey { get; set; }
        public bool IsTreeKey { get; set; }
        public bool IsEnableUpdateVersionValidation { get; set; }
        public object SqlParameterDbType { get; set; }
        /// <summary>
        /// 实体名
        /// </summary>
        public string EntityName { get;  set; }

        public string Edition { get; set; }

        public List<string> Editions
        {
            get {
                if (this.Editions != null) { 
                    var tar=this.Edition.Split(',');
                    return tar.ToList();
                }
                return null;
            }
        }
        /// <summary>
        /// 数据库表名
        /// </summary>
        public string DbTableName { get; set; }
        /// <summary>
        /// 是否忽略字段
        /// </summary>
        public bool IsIgnore { get;  set; }

        public DbDataType DbType { get; set; }
        public DataFam DataType { get; set; }
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
        public string OracleSequenceName { get; set; }
        public bool IsOnlyIgnoreInsert { get; set; }
        public bool IsOnlyIgnoreUpdate { get; set; }
        public bool IsTranscoding { get; set; }
        public string SerializeDateTimeFormat { get;  set; }
        public bool IsJson { get;  set; }
        public bool NoSerialize { get;  set; }
        public string[] IndexGroupNameList { get;  set; }
        public string[] UIndexGroupNameList { get;  set; }
        public bool IsArray { get;  set; }
        public Type UnderType { get;  set; }
        public EntityNavi Navigat { get; set; }
        public int CreateTableFieldSort { get; set; }
        public object SqlParameterSize { get;  set; }
        public string InsertSql { get;  set; }
        public bool InsertServerTime { get;  set; }
        public bool UpdateServerTime { get; set; }
        public string UpdateSql { get; set; }
        public object ExtendedAttribute { get;  set; }
        /// <summary>
        /// 字段种类
        /// </summary>
        public FieldKind Kind { get; set; }
        /// <summary>
        /// 字段SQL别名，即as部分名称，例如：as 别名
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 自定义字段SQL表达式，例如：自定义字段SQL表达式
        /// </summary>
        public string FreeSQL { get; set; }
        /// <summary>
        /// 来源的join中表名称，例如：来源的join中表名称.字段名 as 别名
        /// </summary>
        public string SrcTable { get; set; }
        /// <summary>
        /// 来源的join中字段名称，例如：来源的join中表名称.字段名 as 别名
        /// </summary>
        public string SrcField { get; set; }
        /// <summary>
        /// 绑定的代码表名称，为空时不使用，需要预定义代码表加载方式。
        /// </summary>
        public string Dict { get; set; }

        /// <summary>
        /// 序列名
        /// </summary>
        public string SequenceName { get; set; }
        /// <summary>
        /// 序列所属架构
        /// </summary>
        public string SequenceSchema { get; set; }
        public Dictionary<string,object> more = new Dictionary<string,object>();
        /// <summary>
        /// 是否外键
        /// </summary>
        public bool IsFK=false;
        /// <summary>
        /// 外键对象的表名称
        /// </summary>
        public string thatTable {  get; set; }
        /// <summary>
        /// 外键字段
        /// </summary>
        public string thatField { get; set; }
    }
}

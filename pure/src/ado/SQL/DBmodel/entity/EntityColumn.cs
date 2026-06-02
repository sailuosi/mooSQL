
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
        /// <summary>
        /// 属性 OldDbColumnName（string）。
        /// </summary>
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
        /// <summary>
        /// 是否为分表分片键字段。
        /// </summary>
        public bool IsShardField { get; set; }
        /// <summary>
        /// 属性 IsTreeKey（bool）。
        /// </summary>
        public bool IsTreeKey { get; set; }
        /// <summary>
        /// 属性 IsEnableUpdateVersionValidation（bool）。
        /// </summary>
        public bool IsEnableUpdateVersionValidation { get; set; }
        /// <summary>
        /// 属性 SqlParameterDbType（object）。
        /// </summary>
        public object SqlParameterDbType { get; set; }
        /// <summary>
        /// 实体名
        /// </summary>
        public string EntityName { get;  set; }

        /// <summary>
        /// 属性 Edition（string）。
        /// </summary>
        public string Edition { get; set; }

        /// <summary>
        /// 属性 Editions（List<string>）。
        /// </summary>
        public List<string> Editions
        {
            get {
                if (this.Edition != null) { 
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

        /// <summary>
        /// 属性 DbType（DbDataType）。
        /// </summary>
        public DbDataType DbType { get; set; }
        /// <summary>
        /// 属性 DataType（DataFam）。
        /// </summary>
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
        /// <summary>
        /// 属性 OracleSequenceName（string）。
        /// </summary>
        public string OracleSequenceName { get; set; }
        /// <summary>
        /// 属性 IsOnlyIgnoreInsert（bool）。
        /// </summary>
        public bool IsOnlyIgnoreInsert { get; set; }
        /// <summary>
        /// 属性 IsOnlyIgnoreUpdate（bool）。
        /// </summary>
        public bool IsOnlyIgnoreUpdate { get; set; }
        /// <summary>
        /// 属性 IsTranscoding（bool）。
        /// </summary>
        public bool IsTranscoding { get; set; }
        /// <summary>
        /// 属性 SerializeDateTimeFormat（string）。
        /// </summary>
        public string SerializeDateTimeFormat { get;  set; }
        /// <summary>
        /// 属性 IsJson（bool）。
        /// </summary>
        public bool IsJson { get;  set; }
        /// <summary>
        /// 属性 NoSerialize（bool）。
        /// </summary>
        public bool NoSerialize { get;  set; }
        /// <summary>
        /// 属性 IndexGroupNameList（string[]）。
        /// </summary>
        public string[] IndexGroupNameList { get;  set; }
        /// <summary>
        /// 属性 UIndexGroupNameList（string[]）。
        /// </summary>
        public string[] UIndexGroupNameList { get;  set; }
        /// <summary>
        /// 属性 IsArray（bool）。
        /// </summary>
        public bool IsArray { get;  set; }
        /// <summary>
        /// 属性 UnderType（Type）。
        /// </summary>
        public Type UnderType { get;  set; }
        /// <summary>
        /// 属性 Navigat（EntityNavi）。
        /// </summary>
        public EntityNavi Navigat { get; set; }
        /// <summary>
        /// 属性 CreateTableFieldSort（int）。
        /// </summary>
        public int CreateTableFieldSort { get; set; }
        /// <summary>
        /// 属性 SqlParameterSize（object）。
        /// </summary>
        public object SqlParameterSize { get;  set; }
        /// <summary>
        /// 属性 InsertSql（string）。
        /// </summary>
        public string InsertSql { get;  set; }
        /// <summary>
        /// 属性 InsertServerTime（bool）。
        /// </summary>
        public bool InsertServerTime { get;  set; }
        /// <summary>
        /// 属性 UpdateServerTime（bool）。
        /// </summary>
        public bool UpdateServerTime { get; set; }
        /// <summary>
        /// 属性 UpdateSql（string）。
        /// </summary>
        public string UpdateSql { get; set; }
        /// <summary>
        /// 属性 ExtendedAttribute（object）。
        /// </summary>
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
        /// <summary>
        /// 字段 more（Dictionary<string,object>）。
        /// </summary>
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

        /// <summary>
        /// 转换为String。
        /// </summary>
        public override string ToString()
        {
            return this.DbColumnName;
        }
    }
}
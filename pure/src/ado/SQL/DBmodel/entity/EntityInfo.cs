using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 实体信息
    /// </summary>
    public class EntityInfo
    {
        private string _DbTableName;
        /// <summary>
        /// 实体类名
        /// </summary>
        public string EntityName { get; set; }
        /// <summary>
        /// 数据库表名
        /// </summary>
        public string DbTableName { get { return _DbTableName == null ? EntityName : _DbTableName;  } set { _DbTableName = value; } }
        /// <summary>
        /// 中文名
        /// </summary>
        public string TableDescription { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// 字段信息
        /// </summary>
        public List<EntityColumn> Columns { get; set; }
        /// <summary>
        /// 是否可以删除
        /// </summary>
        public bool? Deletable { get;  set; }
        /// <summary>
        /// 是否可以更新
        /// </summary>
        public bool? Updatable { get; set; }
        /// <summary>
        /// 是否可以插入
        /// </summary>
        public bool? Insertable { get; set; }
        /// <summary>
        /// 索引信息
        /// </summary>
        public List<DbIndexInfo> Indexs { get;  set; }
        /// <summary>
        /// 是否启用创建表字段排序
        /// </summary>
        public bool IsCreateTableFiledSort { get; set; }
        /// <summary>
        /// 描述，即中文名
        /// </summary>
        public string Discrimator { get; set; }
        /// <summary>
        /// 数据库架构名，默认为dbo
        /// </summary>
        public string SchemaName { get; set; }
        /// <summary>
        /// 数据库名，默认为当前连接数据库
        /// </summary>
        public string DatabaseName { get; set; }
        /// <summary>
        /// 数据库服务器名，默认为当前连接服务器
        /// </summary>
        public string ServerName { get; set; }
        /// <summary>
        /// 数据库连接位
        /// </summary>
        public int DBPosition { get; set; }
        /// <summary>
        /// 数据库连接位名称
        /// </summary>
        public string DBName { get; set; }
        /// <summary>
        /// 是否动态分表
        /// </summary>
        public bool? LiveName { get; set; }

        /// <summary>
        /// 是否是视图
        /// </summary>
        public bool IsView {  get; set; }
        /// <summary>
        /// 视图的SQL脚本
        /// </summary>
        public string ViewSQL { get; set; }

        /// <summary>
        /// 代指的实体类型
        /// </summary>
        public DBTableType DType { get; set; }
        /// <summary>
        /// 查询表的join配置
        /// </summary>
        public List<EntityJoin> Joins { get; set; }
        /// <summary>
        /// 查询表的条件配置
        /// </summary>
        public List<EntityWhere> Conditions { get; set; }
        /// <summary>
        /// 查询表的排序配置
        /// </summary>
        public List<EntityOrder> OrderBy { get; set; }
        /// <summary>
        /// 别名，用在多表查询时做为别名使用
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 表名解析器
        /// </summary>
        public ConcurrentDictionary<string,ITableNameInterceptor> NameParses { get; set; }
        /// <summary>
        /// 更多扩展信息
        /// </summary>
        public Dictionary<string,object> more= new Dictionary<string,object>();

        private Dictionary<string, EntityColumn> _FieldMap=new Dictionary<string, EntityColumn>();
        /// <summary>
        /// 字段信息
        /// </summary>
        public Dictionary<string, EntityColumn> FieldMap
        {
            get { 
                var tar= new Dictionary<string, EntityColumn>();
                foreach (var col in Columns)
                {
                    tar[col.PropertyName]=col;
                }
                return _FieldMap;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public EntityInfo() {
            Columns = new List<EntityColumn>();
            NameParses = new ConcurrentDictionary<string, ITableNameInterceptor>();
        }

        private static readonly object _lockObj = new object();
        /// <summary>
        /// 添加字段信息
        /// </summary>
        /// <param name="column"></param>
        public void AddColumnInfo(EntityColumn column)
        {
            lock (_lockObj) { 
                if (Columns == null) {
                    Columns= new List<EntityColumn>();
                }
                if (column == null) return;
                if (_FieldMap.ContainsKey(column.PropertyName)) {
                    _FieldMap[column.PropertyName] = column;

                    for (var i = 0; i < Columns.Count; i++) {
                        if (Columns[i].PropertyName == column.PropertyName) {
                            Columns[i]=column;
                            break;
                        }
                    }
                }
                else
                {
                    _FieldMap.Add(column.PropertyName, column);
                    Columns.Add(column);
                }            
            }


        }
        /// <summary>
        /// 获取字段信息
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public EntityColumn GetColumn(string columnName) {
            if (FieldMap.ContainsKey(columnName)) { 
                return FieldMap[columnName];
            }
            return null;
        }
        /// <summary>
        /// 按照属性名进行匹配查找
        /// </summary>
        /// <param name="memb"></param>
        /// <returns></returns>
        public EntityColumn GetColumn(MemberInfo memb)
        {
            foreach (var col in Columns) {
                if (col.PropertyName == memb.Name) { 
                    return col;
                }
            }
            return null;
        }

        private List<EntityColumn> pks;
        /// <summary>
        /// 获取主键
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public List<EntityColumn> GetPK(bool refresh=false) {
            if (refresh==false && pks != null && pks.Count > 0) { 
                return pks;
            }
            pks = new List<EntityColumn>();
            foreach (var col in Columns) {
                if (col.IsPrimarykey) { 
                    pks.Add(col);
                }
            }
            return pks;
        }
        /// <summary>
        /// 注册一个表名解析器，用于动态分表。name为解析器的名称。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nameInterceptor"></param>
        public void UseNameParser(string name,ITableNameInterceptor nameInterceptor) {
            if (nameInterceptor == null) {
                return;
            }
            if (NameParses.ContainsKey(name)) { 
                return;
            }
            NameParses.TryAdd(name,nameInterceptor);
        }
    }
}

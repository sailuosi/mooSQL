using mooSQL.data.model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体信息
    /// </summary>
    public class EntityInfo
    {

        public EntityInfo() {
            this._OrderBys = new ConcurrentDictionary<int, EntityOrder>();
            this.Joins = new ConcurrentDictionary<string, EntityJoin>();
            this._FieldMap = new ConcurrentDictionary<string, EntityColumn>();
            NameParses = new ConcurrentDictionary<string, ITableNameInterceptor>();
        }
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
        public List<EntityColumn> Columns {
            get {
                return _FieldMap.Values.ToList();
            }
        }
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
        public ConcurrentDictionary<string,EntityJoin> Joins { get; set; }
        /// <summary>
        /// 查询表的条件配置
        /// </summary>
        public ConcurrentBag<EntityWhere> Conditions { get; set; }


        private ConcurrentDictionary<int, EntityOrder> _OrderBys;

        /// <summary>
        /// 查询表的排序配置
        /// </summary>
        public List<EntityOrder> OrderBy {
            get {
                // 1. 直接获取 Key 数组（比遍历 KVP 负载更轻）
                int[] keys = _OrderBys.Keys.ToArray();

                // 2. 使用快速排序（原地排序，无额外分配）
                Array.Sort(keys);

                // 3. 预分配结果数组，按排好的 Key 回查 Value
                int count = keys.Length;
                var result = new List<EntityOrder>();
                for (int i = 0; i < count; i++)
                {
                    // 尝试获取，处理排序期间可能被删除的情况
                    if (_OrderBys.TryGetValue(keys[i], out var value)) {
                        result.Add(value);    
                    }
                }

                return result;
            }
        }
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
        public ConcurrentDictionary<string,object> more= new ConcurrentDictionary<string,object>();

        private ConcurrentDictionary<string, EntityColumn> _FieldMap=new ConcurrentDictionary<string, EntityColumn>();
        /// <summary>
        /// 字段信息
        /// </summary>
        public ConcurrentDictionary<string, EntityColumn> FieldMap
        {
            get { 
                //var tar= new Dictionary<string, EntityColumn>();
                //foreach (var col in Columns)
                //{
                //    tar[col.PropertyName]=col;
                //}
                return _FieldMap;
            }
        }


        private static readonly object _lockObj = new object();
        /// <summary>
        /// 添加字段信息
        /// </summary>
        /// <param name="column"></param>
        public void AddColumnInfo(EntityColumn column)
        {
            lock (_lockObj) { 
                if (_FieldMap == null) {
                    _FieldMap = new ConcurrentDictionary<string, EntityColumn>();
                }
                if (column == null) return;
                if (_FieldMap.ContainsKey(column.PropertyName)) {
                    _FieldMap[column.PropertyName] = column;
                    //列的list版本只读，不再需要修改
                    //for (var i = 0; i < Columns.Count; i++) {
                    //    if (Columns[i].PropertyName == column.PropertyName) {
                    //        Columns[i]=column;
                    //        break;
                    //    }
                    //}
                }
                else
                {
                    _FieldMap.TryAdd(column.PropertyName, column);
                    //Columns.Add(column);
                }            
            }


        }
        /// <summary>
        /// 添加排序
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool AddOrderBy(EntityOrder o) {
            var index = o.Idx;
            if (index == null) {
                if (this._OrderBys.Count == 0)
                {
                    index = 0;
                }
                else {
                    index = (this._OrderBys.Keys.Max() + 1);
                }
                
            }
            return _OrderBys.TryAdd(index.Value, o);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 查询参数封装类，用于构建SQL语句时使用。
    /// </summary>
    public class QueryPara
    {

        /// <summary>
        /// 页大小
        /// </summary>
        public int? pageSize { get; set; }
        /// <summary>
        /// 页码
        /// </summary>
        public int? pageNum { get; set; }
        /// <summary>
        /// 查询条件列表，用于构建SQL语句时使用。
        /// </summary>
        public List<QueryCondition> conditions { get; set; }
        /// <summary>
        /// 排序条件列表，用于构建SQL语句时使用。
        /// </summary>
        public List<QueryOrderBy> orderBy { get; set; }
        /// <summary>
        /// 需要求和等聚合的字段列表，用于构建SQL语句时使用。
        /// </summary>
        public List<SummaryField> sumFields { get; set; }


        [NonSerialized]
        private List<EntityWhere> _suckWheres;
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用，只允许调用方法赋值，不允许自动通过接口接收web参数赋值。
        /// </summary>

        public List<EntityWhere> suckWheres
        {
            get { return _suckWheres; }
        }


        private event Action<SQLBuilder> _OnBuildSQL;
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用，只允许调用方法赋值，不允许自动通过接口接收web参数赋值。
        /// </summary>
        
        public Action<SQLBuilder> onBuildingSQL { get => _OnBuildSQL; }
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用，只允许调用方法赋值，不允许自动通过接口接收web参数赋值。
        /// </summary>
        /// <param name="entityWheres"></param>
        /// <returns></returns>
        public QueryPara SuckWhere(List<EntityWhere> entityWheres) { 
             this._suckWheres = entityWheres;
            return this;
        }

        public void fireBuildSQL(SQLBuilder kit) {
            if (this._OnBuildSQL != null) {
                this._OnBuildSQL(kit);
            }
        }
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用。
        /// </summary>
        /// <param name="onBuild"></param>
        /// <returns></returns>
        public QueryPara OnBuildSQL(Action<SQLBuilder> onBuild) { 
            this._OnBuildSQL += onBuild;
            return this;
        }
    }
    /// <summary>
    /// 单个查询条件，用于构建SQL语句时使用。
    /// </summary>
    public class QueryCondition
    {
        /// <summary>
        /// 分组开闭模式
        /// </summary>
        public int? sink { get; set; }
        /// <summary>
        /// 是否关闭当前条件组，只在分组模式下有效。
        /// </summary>
        public bool? rise { get; set; }
        /// <summary>
        /// 字段名
        /// </summary>
        public string field { get; set; }
        /// <summary>
        /// 操作符，例如：=, ,LIKE等
        /// </summary>
        public string op { get; set; }
        /// <summary>
        /// 条件值，例如：1, 'abc', '2023-04-01'等。
        /// </summary>
        public object value { get; set; }
        /// <summary>
        /// 值的类型，默认不处理，如果设置，则按指定类型进行转换处理。
        /// </summary>
        public string vtype { get; set; } 
    }
    /// <summary>
    /// 排序条件，用于构建SQL语句时使用。
    /// </summary>
    public class QueryOrderBy
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int? idx { get; set; }
        /// <summary>
        /// 字段名
        /// </summary>
        public string field { get; set; }
        /// <summary>
        /// 排序说明如ASC
        /// </summary>
        public string order { get; set; }
        /// <summary>
        /// 排序定义
        /// </summary>
        /// <param name="field"></param>
        /// <param name="order"></param>
        public QueryOrderBy(string field, string order)
        {
            this.field = field;
            this.order = order;
        }
    }
    /// <summary>
    /// 需要聚合的字段，用于构建SQL语句时使用。
    /// </summary>
    public class SummaryField {
        /// <summary>
        /// 排序
        /// </summary>
        public int? idx { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string field { get; set; }
        /// <summary>
        /// 聚合的排序
        /// </summary>
        public string order { get; set; }
        /// <summary>
        /// 别名
        /// </summary>
        public string asName { get; set; }
        /// <summary>
        /// 模式，例如：sum, avg, max, min等。
        /// </summary>
        public string mode { get; set; }
    }
}

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
        
        [NonSerialized]
        private List<EntityWhere> _suckWheres;
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用，只允许调用方法赋值，不允许自动通过接口接收web参数赋值。
        /// </summary>

        public List<EntityWhere> suckWheres
        {
            get { return _suckWheres; }
        }

        [NonSerialized]
        private Action<SQLBuilder> _OnBuildSQL;
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
        /// <summary>
        /// 附加条件，用于构建SQL语句时使用。
        /// </summary>
        /// <param name="onBuild"></param>
        /// <returns></returns>
        public QueryPara OnBuildSQL(Action<SQLBuilder> onBuild) { 
            this._OnBuildSQL = onBuild;
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
        /// 操作符，例如：=, >,<LIKE等
        /// </summary>
        public string op { get; set; }
        /// <summary>
        /// 条件值，例如：1, 'abc', '2023-04-01'等。
        /// </summary>
        public object value { get; set; }
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
        public string field { get; set; }
        public string order { get; set; }

        public QueryOrderBy(string field, string order)
        {
            this.field = field;
            this.order = order;
        }
    }
}

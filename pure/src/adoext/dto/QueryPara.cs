using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class QueryPara
    {

    
        public int? pageSize { get; set; }
        public int? pageNum { get; set; }

        public List<QueryCondition> conditions { get; set; }
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
        
        public Action<SQLBuilder> onBuildSQL { get => _OnBuildSQL; }
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

    public class QueryCondition
    {
        public int? sink { get; set; }
        public bool? rise { get; set; }
        public string field { get; set; }
        public string op { get; set; }
        public object value { get; set; }
    }

    public class QueryOrderBy
    {
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

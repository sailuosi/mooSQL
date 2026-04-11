using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 快速 LINQ 编译阶段的共享状态（层次 SQL、导航列、结果类型等）。
    /// </summary>
    public class FastCompileContext
    {
        /// <summary>
        /// 初始化导航列字典。
        /// </summary>
        public FastCompileContext() {
            this.NavColumns = new Dictionary<Type, List<EntityColumn>>();
        }



        /// <summary>当前正在编织的层。</summary>
        public LayerContext CurrentLayer {  get;  set; }

        /// <summary>最顶层 SQL 层。</summary>
        public LayerContext TopLayer { get; set; }

        /// <summary>编译完成后执行查询的委托。</summary>
        public Func<QueryContext, object> onRunQuery;

        /// <summary>数据库实例。</summary>
        public DBInstance DB;

        /// <summary>最终投影或标量结果的 CLR 类型。</summary>
        public Type ResultType { get; set; }

        /// <summary>主实体类型（若有）。</summary>
        public Type EntityType { get; set; }

        /// <summary>当前编译语句类型（SELECT/UPDATE/DELETE）。</summary>
        public LayerRunType RunType { get; set; }
        /// <summary>
        /// 导航列
        /// </summary>
        public Dictionary<Type,List<EntityColumn>> NavColumns { get; set; }

        /// <summary>
        /// 由已有 <see cref="SQLBuilder"/> 初始化层上下文与数据库引用。
        /// </summary>
        public void initByBuilder(SQLBuilder builder) {

            var lay = new LayerContext();
            lay.Root = builder;
            lay.Current = builder;

            this.CurrentLayer = lay;
            this.TopLayer = lay;

            this.DB= builder.DBLive;

        }
        /// <summary>
        /// 添加导航属性
        /// </summary>
        /// <param name="boss"></param>
        /// <param name="slave"></param>
        public void AddNavTarget(Type boss, EntityColumn slave) {
            if (this.NavColumns == null) { 
                this.NavColumns= new Dictionary<Type, List<EntityColumn>>();
            }
            if (!this.NavColumns.ContainsKey(boss)) { 
                this.NavColumns.Add(boss, new List<EntityColumn>());
            }
            this.NavColumns[boss].AddNotRepeat(slave);
        }
    }

    internal class FastCompileContext<T> : FastCompileContext {


        public Func<QueryContext, T> onExecute;
    }
}

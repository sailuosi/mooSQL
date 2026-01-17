using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class FastCompileContext
    {
        public FastCompileContext() {
            this.NavColumns = new Dictionary<Type, List<EntityColumn>>();
        }



        public LayerContext CurrentLayer {  get;  set; }

        public LayerContext TopLayer { get; set; }

        public Func<QueryContext, object> onRunQuery;

        public DBInstance DB;

        public Type ResultType { get; set; }

        public Type EntityType { get; set; }

        public LayerRunType RunType { get; set; }
        /// <summary>
        /// 导航列
        /// </summary>
        public Dictionary<Type,List<EntityColumn>> NavColumns { get; set; }

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

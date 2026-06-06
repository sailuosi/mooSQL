
using mooSQL.data;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Mapping;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// SQL模型包，代表一个LINQ查询的构建结果。
    /// </summary>
    internal class SentenceBag
    {

        public DBInstance DBLive;
        /// <summary>
        /// 待执行语句集
        /// </summary>
        public List<SentenceItem> Sentences;

        public Expression srcExp;

        public Type EntityType;


        public Expression ErrorExpression;

        /// <summary>导航属性列，执行阶段二次查询加载。</summary>
        public Dictionary<Type, List<EntityColumn>> NavColumns { get; } = new();

        public void AddNavColumn(Type entityType, EntityColumn column)
        {
            if (!NavColumns.TryGetValue(entityType, out var list))
            {
                list = new List<EntityColumn>();
                NavColumns[entityType] = list;
            }
            list.AddNotRepeat(column);
        }

        public bool IsFinalized=false;

        /// <summary>无 Includes、单语句时可缓存编译产物。</summary>
        public bool IsCacheable => NavColumns.Count == 0 && (Sentences?.Count ?? 0) <= 1;

        /// <summary>
        /// 中间环节的编辑器
        /// </summary>
        public IClauseContext buildContext;

        /** 结果执行器 **/


        private ISentenceRunner runner;

        public virtual ISentenceRunner Runner { 
            get {
                if (runner == null) {
                    runner = new BasicSentenceRunner();
                }
                return runner; 
            }
        }


        public void add(SentenceItem sentence) {
            if (Sentences is null) {
                Sentences= new List<SentenceItem>();
            }
            this.Sentences.Add(sentence);
        }



        List<Expression>? _parameterized;



        internal void SetParameterized(List<Expression>? parameterized)
        {
            _parameterized = parameterized;
        }


        internal void ClearDynamicQueryableInfo()
        {
            
        }
    }

    /// <summary>
    /// 含有类型参数的结果集
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SentenceBag<T>:SentenceBag
    {

        private ISentenceRunner<T> runner;

        public ISentenceRunner<T> Runner
        {
            get {
                if (runner == null)
                {
                    runner = new BasicSentenceRunner<T>();
                }
                return runner; 
            }
        }

    }
}
